using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface ITimeEntryService
{
    Task<IReadOnlyList<TimeEntry>> GetAllAsync(DateTime? date = null, int? projectId = null, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TimeEntry> CreateAsync(int projectId, DateTime date, decimal hours, string? description, bool isBillable, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
