using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class DashboardService(TimeTrackerDbContext db) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        int totalClients = await db.Clients.CountAsync(c => c.IsActive, ct);
        int totalProjects = await db.Projects.CountAsync(p => p.IsActive, ct);
        int totalTimeEntries = await db.TimeEntries.CountAsync(ct);

        decimal totalHoursLogged = await db.TimeEntries.SumAsync(t => t.Hours, ct);
        decimal billableHours = await db.TimeEntries
            .Where(t => t.IsBillable)
            .SumAsync(t => t.Hours, ct);
        decimal nonBillableHours = totalHoursLogged - billableHours;

        int totalInvoices = await db.Invoices.CountAsync(ct);
        decimal outstandingInvoiceTotal = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Issued)
            .SumAsync(i => i.Total, ct);

        return new DashboardSummary(
            totalClients,
            totalProjects,
            totalTimeEntries,
            totalHoursLogged,
            billableHours,
            nonBillableHours,
            totalInvoices,
            outstandingInvoiceTotal);
    }
}
