using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IInvoiceService
{
    Task<Invoice> GenerateInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
    Task<Invoice?> GetInvoiceAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetClientInvoicesAsync(int clientId, CancellationToken ct = default);
    Task<Invoice> IssueInvoiceAsync(int id, CancellationToken ct = default);
    Task<Invoice> MarkPaidAsync(int id, CancellationToken ct = default);
    Task<Invoice> VoidInvoiceAsync(int id, string reason, CancellationToken ct = default);
}

public class InvoiceService(TimeTrackerDbContext db) : IInvoiceService
{
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal WeekendMultiplier = 1.5m;
    private const decimal StandardDailyHours = 8m;

    public async Task<Invoice> GenerateInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        var client = await db.Clients
            .Where(c => c.Id == clientId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Client {clientId} not found");

        if (!client.IsActive)
            throw new ValidationException("Client is not active");

        if (periodStart >= periodEnd)
            throw new ValidationException("Period start must be before period end");

        if (periodEnd > DateTime.UtcNow.Date.AddDays(1))
            throw new ValidationException("Period end cannot be in the future");

        var hasOverlap = await db.Invoices
            .Where(i => i.ClientId == clientId && i.Status != InvoiceStatus.Void)
            .Where(i => i.PeriodStart < periodEnd && i.PeriodEnd > periodStart)
            .AnyAsync(ct);

        if (hasOverlap)
            throw new ValidationException("An invoice already exists for this client with an overlapping period");

        var entries = await db.TimeEntries
            .Include(t => t.Project)
            .Where(t => t.Project.ClientId == clientId
                && t.IsBillable
                && t.Date >= periodStart
                && t.Date < periodEnd)
            .ToListAsync(ct);

        if (entries.Count == 0)
            throw new ValidationException("No billable time entries found for the specified period");

        var lines = BuildInvoiceLines(entries, client.HourlyRate);
        decimal subtotal = lines.Sum(l => l.LineTotal);
        decimal totalHours = lines.Sum(l => l.Hours);

        decimal discountPercent = totalHours switch
        {
            > 160m => 0.15m,
            > 80m => 0.10m,
            > 40m => 0.05m,
            _ => 0m,
        };

        decimal discountAmount = Math.Round(subtotal * discountPercent, 2, MidpointRounding.ToEven);
        decimal taxAmount = Math.Round((subtotal - discountAmount) * client.TaxRate, 2, MidpointRounding.ToEven);
        decimal total = subtotal - discountAmount + taxAmount;

        string invoiceNumber = await GenerateInvoiceNumberAsync(clientId, periodStart, ct);

        var invoice = new Invoice
        {
            ClientId = clientId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Subtotal = subtotal,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            TaxRate = client.TaxRate,
            TaxAmount = taxAmount,
            Total = total,
            InvoiceLines = lines,
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceAsync(int id, CancellationToken ct = default)
    {
        return await db.Invoices
            .Include(i => i.InvoiceLines)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<IReadOnlyList<Invoice>> GetClientInvoicesAsync(int clientId, CancellationToken ct = default)
    {
        return await db.Invoices
            .Include(i => i.InvoiceLines)
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Invoice> IssueInvoiceAsync(int id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.InvoiceLines)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot issue invoice in {invoice.Status} status. Only Draft invoices can be issued.");

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> MarkPaidAsync(int id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.InvoiceLines)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException($"Cannot mark invoice as paid in {invoice.Status} status. Only Issued invoices can be paid.");

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> VoidInvoiceAsync(int id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Void reason is required");

        var invoice = await db.Invoices
            .Include(i => i.InvoiceLines)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Void)
            throw new InvalidOperationException($"Cannot void invoice in {invoice.Status} status.");

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = reason;
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    private static List<InvoiceLine> BuildInvoiceLines(List<TimeEntry> entries, decimal clientHourlyRate)
    {
        var lines = new List<InvoiceLine>();

        var byProject = entries.GroupBy(e => e.ProjectId);

        foreach (var projectGroup in byProject)
        {
            string projectName = projectGroup.First().Project.Name;
            int projectId = projectGroup.Key;

            decimal standardHours = 0m;
            decimal overtimeHours = 0m;
            decimal weekendHours = 0m;

            var byDate = projectGroup.GroupBy(e => e.Date.Date);

            foreach (var dateGroup in byDate)
            {
                decimal dailyHours = dateGroup.Sum(e => e.Hours);
                bool isWeekend = dateGroup.Key.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                if (isWeekend)
                {
                    weekendHours += dailyHours;
                }
                else
                {
                    if (dailyHours <= StandardDailyHours)
                    {
                        standardHours += dailyHours;
                    }
                    else
                    {
                        standardHours += StandardDailyHours;
                        overtimeHours += dailyHours - StandardDailyHours;
                    }
                }
            }

            if (standardHours > 0)
            {
                decimal rate = clientHourlyRate;
                lines.Add(new InvoiceLine
                {
                    ProjectId = projectId,
                    Description = $"{projectName} — Standard",
                    Hours = standardHours,
                    HourlyRate = rate,
                    RateType = RateType.Standard,
                    LineTotal = Math.Round(standardHours * rate, 2, MidpointRounding.ToEven),
                });
            }

            if (overtimeHours > 0)
            {
                decimal rate = Math.Round(clientHourlyRate * OvertimeMultiplier, 2, MidpointRounding.ToEven);
                lines.Add(new InvoiceLine
                {
                    ProjectId = projectId,
                    Description = $"{projectName} — Overtime",
                    Hours = overtimeHours,
                    HourlyRate = rate,
                    RateType = RateType.Overtime,
                    LineTotal = Math.Round(overtimeHours * rate, 2, MidpointRounding.ToEven),
                });
            }

            if (weekendHours > 0)
            {
                decimal rate = Math.Round(clientHourlyRate * WeekendMultiplier, 2, MidpointRounding.ToEven);
                lines.Add(new InvoiceLine
                {
                    ProjectId = projectId,
                    Description = $"{projectName} — Weekend",
                    Hours = weekendHours,
                    HourlyRate = rate,
                    RateType = RateType.Weekend,
                    LineTotal = Math.Round(weekendHours * rate, 2, MidpointRounding.ToEven),
                });
            }
        }

        return lines;
    }

    private async Task<string> GenerateInvoiceNumberAsync(int clientId, DateTime periodStart, CancellationToken ct)
    {
        string prefix = $"INV-{clientId:D4}-{periodStart:yyyyMM}";

        int existingCount = await db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .CountAsync(ct);

        return $"{prefix}-{(existingCount + 1):D3}";
    }
}
