using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface ITimeEntryService
{
    Task<IEnumerable<TimeEntry>> GetAllAsync(DateTime? date, int? projectId, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TimeEntry> CreateAsync(TimeEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
