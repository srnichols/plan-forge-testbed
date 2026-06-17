using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class ClientSummaryService(TimeTrackerDbContext db) : IClientSummaryService
{
    public Task<ClientActivitySummary?> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
