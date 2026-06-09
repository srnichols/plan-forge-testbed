using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;

namespace TimeTracker.Api.Services;

public class DashboardService(TimeTrackerDbContext dbContext) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalClients = await dbContext.Clients.CountAsync(c => c.IsActive, cancellationToken);
        var totalProjects = await dbContext.Projects.CountAsync(p => p.IsActive, cancellationToken);
        var totalTimeEntries = await dbContext.TimeEntries.CountAsync(cancellationToken);
        var totalHours = await dbContext.TimeEntries.SumAsync(t => t.Hours, cancellationToken);
        var billableHours = await dbContext.TimeEntries.Where(t => t.IsBillable).SumAsync(t => t.Hours, cancellationToken);
        var totalInvoices = await dbContext.Invoices.CountAsync(cancellationToken);

        return new DashboardSummary(
            TotalClients: totalClients,
            TotalProjects: totalProjects,
            TotalTimeEntries: totalTimeEntries,
            TotalHoursLogged: totalHours,
            BillableHours: billableHours,
            NonBillableHours: totalHours - billableHours,
            TotalInvoices: totalInvoices,
            OutstandingInvoiceTotal: 0m);
    }
}
