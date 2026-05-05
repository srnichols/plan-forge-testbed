using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public interface ITimeEntriesApi
{
    Task<List<TimeEntryDto>> GetAllAsync(DateTime? date = null, int? projectId = null, CancellationToken ct = default);
    Task<TimeEntryDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TimeEntryDto> CreateAsync(TimeEntryFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
