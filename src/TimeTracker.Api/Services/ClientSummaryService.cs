using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class ClientSummaryService(TimeTrackerDbContext db) : IClientSummaryService
{
    public async Task<ClientActivitySummary?> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        Client? client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientId, ct);
        if (client is null)
            return null;

        int projectCount = await db.Projects.CountAsync(p => p.ClientId == clientId && p.IsActive, ct);

        List<int> projectIds = await db.Projects
            .Where(p => p.ClientId == clientId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        decimal totalHours = await db.TimeEntries
            .Where(t => projectIds.Contains(t.ProjectId))
            .SumAsync(t => (decimal?)t.Hours, ct) ?? 0m;

        decimal billableHours = await db.TimeEntries
            .Where(t => projectIds.Contains(t.ProjectId) && t.IsBillable)
            .SumAsync(t => (decimal?)t.Hours, ct) ?? 0m;

        decimal nonBillableHours = totalHours - billableHours;

        int invoiceCount = await db.Invoices.CountAsync(i => i.ClientId == clientId, ct);

        decimal outstandingTotal = await db.Invoices
            .Where(i => i.ClientId == clientId && (i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Issued))
            .SumAsync(i => (decimal?)i.Total, ct) ?? 0m;

        return new ClientActivitySummary(
            clientId,
            client.Name,
            projectCount,
            totalHours,
            billableHours,
            nonBillableHours,
            invoiceCount,
            outstandingTotal);
    }
}
