using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IClientSummaryService
{
    Task<ClientActivitySummary?> GetByClientIdAsync(int clientId, CancellationToken ct = default);
}
