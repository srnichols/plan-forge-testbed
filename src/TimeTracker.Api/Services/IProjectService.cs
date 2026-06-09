using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllAsync(int? clientId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task<Project> UpdateAsync(int id, Project project, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
