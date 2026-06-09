using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(string name, string? description, int clientId, CancellationToken cancellationToken = default);
    Task<Project?> UpdateAsync(int id, string name, string? description, int clientId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
