using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class InvoiceService(TimeTrackerDbContext dbContext, ILogger<InvoiceService> logger) : IInvoiceService
{
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal WeekendMultiplier = 1.5m;
    private const decimal StandardDailyHoursCap = 8m;

    public async Task<Invoice> GenerateInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(periodStart, periodEnd);

        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken)
            ?? throw new ValidationException($"Client with id {clientId} does not exist.");
        if (!client.IsActive)
        {
            throw new ValidationException($"Client {clientId} is not active.");
        }

        await EnsureNoOverlappingInvoiceAsync(clientId, periodStart, periodEnd, cancellationToken);

        var entries = await dbContext.TimeEntries
            .AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.Project.ClientId == clientId
                        && t.IsBillable
                        && t.Date >= periodStart
                        && t.Date <= periodEnd)
            .ToListAsync(cancellationToken);

        var lines = BuildInvoiceLines(entries, client.HourlyRate);
        var subtotal = lines.Sum(l => l.LineTotal);
        var totalBillableHours = lines.Sum(l => l.Hours);

        var discountPercent = CalculateVolumeDiscount(totalBillableHours);
        var discountAmount = decimal.Round(subtotal * discountPercent, 2, MidpointRounding.ToEven);
        var taxAmount = decimal.Round((subtotal - discountAmount) * client.TaxRate, 2, MidpointRounding.ToEven);
        var total = subtotal - discountAmount + taxAmount;

        var invoiceNumber = await GenerateInvoiceNumberAsync(clientId, cancellationToken);

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
            CreatedAt = DateTime.UtcNow,
            InvoiceLines = lines,
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogInvoiceGenerated(logger, invoice.Id, invoice.InvoiceNumber, clientId, invoice.Total);
        return invoice;
    }

    public Task<Invoice?> GetInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceLines)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetClientInvoicesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceLines)
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ValidationException($"Invoice {id} cannot be issued from status {invoice.Status}; it must be Draft.");
        }

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        LogInvoiceIssued(logger, invoice.Id);
        return invoice;
    }

    public async Task<Invoice?> MarkPaidAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new ValidationException($"Invoice {id} cannot be marked paid from status {invoice.Status}; it must be Issued.");
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        LogInvoicePaid(logger, invoice.Id);
        return invoice;
    }

    public async Task<Invoice?> VoidInvoiceAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("Void reason is required.");
        }

        if (reason.Trim().Length > 500)
        {
            throw new ValidationException("Void reason must be 500 characters or fewer.");
        }

        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new ValidationException($"Invoice {id} cannot be voided once paid.");
        }

        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new ValidationException($"Invoice {id} is already void.");
        }

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        LogInvoiceVoided(logger, invoice.Id);
        return invoice;
    }

    private static void ValidateDateRange(DateTime periodStart, DateTime periodEnd)
    {
        if (periodStart >= periodEnd)
        {
            throw new ValidationException("Period start must be before period end.");
        }

        if (periodStart > DateTime.UtcNow || periodEnd > DateTime.UtcNow)
        {
            throw new ValidationException("Period dates cannot be in the future.");
        }
    }

    private async Task EnsureNoOverlappingInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var overlapping = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.ClientId == clientId
                           && i.Status != InvoiceStatus.Void
                           && i.PeriodStart <= periodEnd
                           && i.PeriodEnd >= periodStart, cancellationToken);

        if (overlapping)
        {
            throw new ValidationException($"An existing invoice for client {clientId} overlaps the requested period.");
        }
    }

    private static List<InvoiceLine> BuildInvoiceLines(IEnumerable<TimeEntry> entries, decimal clientHourlyRate)
    {
        var lines = new List<InvoiceLine>();

        var byProject = entries.GroupBy(e => new { e.ProjectId, ProjectName = e.Project.Name });

        foreach (var projectGroup in byProject.OrderBy(g => g.Key.ProjectName))
        {
            var standardHours = 0m;
            var overtimeHours = 0m;
            var weekendHours = 0m;

            var byDate = projectGroup.GroupBy(e => e.Date.Date);
            foreach (var dayGroup in byDate)
            {
                var dailyHours = dayGroup.Sum(e => e.Hours);
                if (dailyHours <= 0)
                {
                    continue;
                }

                var dayOfWeek = dayGroup.Key.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                {
                    weekendHours += dailyHours;
                }
                else
                {
                    var standardPortion = Math.Min(dailyHours, StandardDailyHoursCap);
                    standardHours += standardPortion;
                    if (dailyHours > StandardDailyHoursCap)
                    {
                        overtimeHours += dailyHours - StandardDailyHoursCap;
                    }
                }
            }

            AddLineIfPositive(lines, projectGroup.Key.ProjectId, projectGroup.Key.ProjectName, standardHours, clientHourlyRate, RateType.Standard);
            AddLineIfPositive(lines, projectGroup.Key.ProjectId, projectGroup.Key.ProjectName, overtimeHours, clientHourlyRate * OvertimeMultiplier, RateType.Overtime);
            AddLineIfPositive(lines, projectGroup.Key.ProjectId, projectGroup.Key.ProjectName, weekendHours, clientHourlyRate * WeekendMultiplier, RateType.Weekend);
        }

        return lines;
    }

    private static void AddLineIfPositive(List<InvoiceLine> lines, int projectId, string projectName, decimal hours, decimal hourlyRate, RateType rateType)
    {
        if (hours <= 0)
        {
            return;
        }

        var roundedRate = decimal.Round(hourlyRate, 2, MidpointRounding.ToEven);
        var lineTotal = decimal.Round(hours * roundedRate, 2, MidpointRounding.ToEven);

        lines.Add(new InvoiceLine
        {
            ProjectId = projectId,
            Description = $"{projectName} — {rateType}",
            Hours = hours,
            HourlyRate = roundedRate,
            RateType = rateType,
            LineTotal = lineTotal,
        });
    }

    private static decimal CalculateVolumeDiscount(decimal totalBillableHours)
    {
        if (totalBillableHours > 160m) return 0.15m;
        if (totalBillableHours > 80m) return 0.10m;
        if (totalBillableHours > 40m) return 0.05m;
        return 0m;
    }

    private async Task<string> GenerateInvoiceNumberAsync(int clientId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"INV-{clientId:D4}-{now:yyyyMM}-";

        var existingCount = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(i => i.ClientId == clientId
                             && i.InvoiceNumber.StartsWith(prefix), cancellationToken);

        var sequence = existingCount + 1;
        return $"{prefix}{sequence:D3}";
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Invoice generated: {InvoiceId} ({InvoiceNumber}) for client {ClientId}, total {Total}")]
    private static partial void LogInvoiceGenerated(ILogger logger, int invoiceId, string invoiceNumber, int clientId, decimal total);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Invoice issued: {InvoiceId}")]
    private static partial void LogInvoiceIssued(ILogger logger, int invoiceId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Invoice paid: {InvoiceId}")]
    private static partial void LogInvoicePaid(ILogger logger, int invoiceId);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Invoice voided: {InvoiceId}")]
    private static partial void LogInvoiceVoided(ILogger logger, int invoiceId);
}
