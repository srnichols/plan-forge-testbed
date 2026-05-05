using System.Net.Http.Json;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public class DashboardApi(HttpClient httpClient) : IDashboardApi
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
        => (await httpClient.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard", ct))!;
}
