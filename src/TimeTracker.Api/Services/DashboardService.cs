using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class DashboardService(TimeTrackerDbContext dbContext, ILogger logger) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalClients = await dbContext.Clients
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var totalProjects = await dbContext.Projects
            .AsNoTracking()
            .CountAsync(p => p.IsActive && p.Client!.IsActive, cancellationToken);

        var entryTotals = await dbContext.TimeEntries
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalHours = g.Sum(t => (decimal?)t.Hours) ?? 0m,
                BillableHours = g.Sum(t => t.IsBillable ? (decimal?)t.Hours : 0m) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalTimeEntries = entryTotals?.Count ?? 0;
        var totalHoursLogged = entryTotals?.TotalHours ?? 0m;
        var billableHours = entryTotals?.BillableHours ?? 0m;
        var nonBillableHours = totalHoursLogged - billableHours;

        var invoiceTotals = await dbContext.Invoices
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Outstanding = g.Sum(i =>
                    (i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Issued)
                        ? (decimal?)i.Total
                        : 0m) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalInvoices = invoiceTotals?.Count ?? 0;
        var outstandingInvoiceTotal = invoiceTotals?.Outstanding ?? 0m;

        LogSummaryComputed(logger, totalClients, totalProjects, totalTimeEntries, totalInvoices);

        return new DashboardSummary(
            TotalClients: totalClients,
            TotalProjects: totalProjects,
            TotalTimeEntries: totalTimeEntries,
            TotalHoursLogged: totalHoursLogged,
            BillableHours: billableHours,
            NonBillableHours: nonBillableHours,
            TotalInvoices: totalInvoices,
            OutstandingInvoiceTotal: outstandingInvoiceTotal);
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information,
        Message = "Dashboard summary computed: Clients={Clients}, Projects={Projects}, Entries={Entries}, Invoices={Invoices}")]
    private static partial void LogSummaryComputed(ILogger logger, int clients, int projects, int entries, int invoices);
}
