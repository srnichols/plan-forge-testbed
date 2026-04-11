using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
    private const decimal StandardMultiplier = 1.0m;
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal WeekendMultiplier = 1.5m;
    private const decimal StandardDailyHoursThreshold = 8m;

    public async Task<Invoice> GenerateInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        if (periodStart >= periodEnd)
            throw new ValidationException("periodStart must be before periodEnd");

        if (periodEnd.Date > DateTime.UtcNow.Date)
            throw new ValidationException("Period end cannot be in the future");

        var client = await db.Clients
            .Where(c => c.Id == clientId && c.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Client {clientId} not found");

        bool hasDuplicate = await db.Invoices
            .Where(i => i.ClientId == clientId
                     && i.Status != InvoiceStatus.Void
                     && i.PeriodStart <= periodEnd
                     && i.PeriodEnd >= periodStart)
            .AnyAsync(ct);

        if (hasDuplicate)
            throw new ValidationException("An invoice already exists for this client with an overlapping period");

        var entries = await db.TimeEntries
            .Include(t => t.Project)
            .Where(t => t.Project.ClientId == clientId
                     && t.Date >= periodStart
                     && t.Date <= periodEnd
                     && t.IsBillable)
            .ToListAsync(ct);

        if (entries.Count == 0)
            throw new ValidationException("No billable time entries found for this period");

        List<InvoiceLine> invoiceLines = BuildInvoiceLines(entries, client.HourlyRate);

        decimal subtotal = invoiceLines.Sum(l => l.LineTotal);
        decimal totalHours = invoiceLines.Sum(l => l.Hours);
        decimal discountPercent = CalculateVolumeDiscount(totalHours);
        decimal discountAmount = Math.Round(subtotal * discountPercent / 100m, 2, MidpointRounding.ToEven);
        decimal afterDiscount = subtotal - discountAmount;

        // TaxRate not on Client model (out of scope for this slice) — default to 0%
        decimal taxRate = 0m;
        decimal taxAmount = Math.Round(afterDiscount * taxRate / 100m, 2, MidpointRounding.ToEven);
        decimal total = afterDiscount + taxAmount;

        string invoiceNumber = await GenerateInvoiceNumberAsync(clientId, ct);

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
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total,
            CreatedAt = DateTime.UtcNow,
            InvoiceLines = invoiceLines,
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceAsync(int id, CancellationToken ct = default)
    {
        return await db.Invoices
            .Include(i => i.InvoiceLines)
            .ThenInclude(l => l.Project)
            .Where(i => i.Id == id)
            .FirstOrDefaultAsync(ct);
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
        var invoice = await GetRequiredInvoiceAsync(id, ct);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot issue invoice {invoice.InvoiceNumber}: status is {invoice.Status}, must be Draft");

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> MarkPaidAsync(int id, CancellationToken ct = default)
    {
        var invoice = await GetRequiredInvoiceAsync(id, ct);

        if (invoice.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException(
                $"Cannot mark invoice {invoice.InvoiceNumber} as paid: status is {invoice.Status}, must be Issued");

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> VoidInvoiceAsync(int id, string reason, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var invoice = await GetRequiredInvoiceAsync(id, ct);

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException(
                $"Cannot void invoice {invoice.InvoiceNumber}: paid invoices cannot be voided");

        if (invoice.Status == InvoiceStatus.Void)
            throw new InvalidOperationException(
                $"Cannot void invoice {invoice.InvoiceNumber}: invoice is already voided");

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = reason;

        await db.SaveChangesAsync(ct);
        return invoice;
    }

    private static List<InvoiceLine> BuildInvoiceLines(List<TimeEntry> entries, decimal baseHourlyRate)
    {
        // Group entries by project and date to calculate daily hours per project
        var dailyProjectHours = entries
            .GroupBy(e => (e.ProjectId, Date: e.Date.Date))
            .Select(g => new
            {
                ProjectId = g.Key.ProjectId,
                Date = g.Key.Date,
                ProjectName = g.First().Project.Name,
                TotalHours = g.Sum(x => x.Hours),
            });

        // Classify hours into rate tiers: weekend (all 1.5x), weekday ≤8h standard, weekday >8h split
        var classified = new List<(int ProjectId, string ProjectName, RateType RateType, decimal Hours)>();

        foreach (var day in dailyProjectHours)
        {
            if (IsWeekend(day.Date))
            {
                classified.Add((day.ProjectId, day.ProjectName, RateType.Weekend, day.TotalHours));
            }
            else if (day.TotalHours <= StandardDailyHoursThreshold)
            {
                classified.Add((day.ProjectId, day.ProjectName, RateType.Standard, day.TotalHours));
            }
            else
            {
                classified.Add((day.ProjectId, day.ProjectName, RateType.Standard, StandardDailyHoursThreshold));
                classified.Add((day.ProjectId, day.ProjectName, RateType.Overtime, day.TotalHours - StandardDailyHoursThreshold));
            }
        }

        // Aggregate by (ProjectId, RateType) into invoice lines
        return classified
            .GroupBy(c => (c.ProjectId, c.ProjectName, c.RateType))
            .Select(g =>
            {
                decimal hours = g.Sum(x => x.Hours);
                decimal multiplier = g.Key.RateType switch
                {
                    RateType.Weekend => WeekendMultiplier,
                    RateType.Overtime => OvertimeMultiplier,
                    _ => StandardMultiplier,
                };
                decimal hourlyRate = Math.Round(baseHourlyRate * multiplier, 2, MidpointRounding.ToEven);
                decimal lineTotal = Math.Round(hours * hourlyRate, 2, MidpointRounding.ToEven);

                return new InvoiceLine
                {
                    ProjectId = g.Key.ProjectId,
                    Description = $"{g.Key.ProjectName} - {g.Key.RateType}",
                    Hours = hours,
                    HourlyRate = hourlyRate,
                    RateType = g.Key.RateType,
                    LineTotal = lineTotal,
                };
            })
            .ToList();
    }

    private async Task<Invoice> GetRequiredInvoiceAsync(int id, CancellationToken ct)
    {
        return await db.Invoices
            .Include(i => i.InvoiceLines)
            .Where(i => i.Id == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");
    }

    private async Task<string> GenerateInvoiceNumberAsync(int clientId, CancellationToken ct)
    {
        string prefix = $"INV-{clientId:D4}-{DateTime.UtcNow:yyyyMM}-";

        string? maxNumber = await db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);

        int nextSequence = 1;
        if (maxNumber is not null)
        {
            string sequencePart = maxNumber[prefix.Length..];
            if (int.TryParse(sequencePart, out int current))
                nextSequence = current + 1;
        }

        return $"{prefix}{nextSequence:D3}";
    }

    private static bool IsWeekend(DateTime date)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static decimal CalculateVolumeDiscount(decimal totalHours)
    {
        return totalHours switch
        {
            > 160m => 15m,
            > 80m => 10m,
            > 40m => 5m,
            _ => 0m,
        };
    }
}
