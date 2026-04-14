using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}
