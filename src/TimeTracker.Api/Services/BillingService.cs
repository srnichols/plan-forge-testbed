using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;

namespace TimeTracker.Api.Services;

public interface IBillingService
{
    Task<BillingSummary> GetBillingSummaryAsync(DateTime startDate, DateTime endDate, int? clientId = null);
}

public class BillingService : IBillingService
{
    private readonly TimeTrackerDbContext _db;

    public BillingService(TimeTrackerDbContext db) => _db = db;

    public async Task<BillingSummary> GetBillingSummaryAsync(DateTime startDate, DateTime endDate, int? clientId = null)
    {
        var query = _db.TimeEntries
            .Include(t => t.Project)
            .ThenInclude(p => p.Client)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (clientId.HasValue)
            query = query.Where(t => t.Project.ClientId == clientId.Value);

        var entries = await query.ToListAsync();

        var byClient = entries
            .GroupBy(e => e.Project.Client)
            .Select(g => new ClientBilling
            {
                ClientName = g.Key.Name,
                HourlyRate = g.Key.HourlyRate,
                BillableHours = g.Where(e => e.IsBillable).Sum(e => e.Hours),
                NonBillableHours = g.Where(e => !e.IsBillable).Sum(e => e.Hours),
                TotalAmount = g.Where(e => e.IsBillable).Sum(e => e.Hours) * g.Key.HourlyRate,
            })
            .ToList();

        return new BillingSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalBillableHours = byClient.Sum(c => c.BillableHours),
            TotalNonBillableHours = byClient.Sum(c => c.NonBillableHours),
            TotalAmount = byClient.Sum(c => c.TotalAmount),
            ByClient = byClient,
        };
    }
}

public record BillingSummary
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalBillableHours { get; init; }
    public decimal TotalNonBillableHours { get; init; }
    public decimal TotalAmount { get; init; }
    public List<ClientBilling> ByClient { get; init; } = new();
}

public record ClientBilling
{
    public string ClientName { get; init; } = string.Empty;
    public decimal HourlyRate { get; init; }
    public decimal BillableHours { get; init; }
    public decimal NonBillableHours { get; init; }
    public decimal TotalAmount { get; init; }
}
