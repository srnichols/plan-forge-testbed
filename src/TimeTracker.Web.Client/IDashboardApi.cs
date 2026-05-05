using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public interface IDashboardApi
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
