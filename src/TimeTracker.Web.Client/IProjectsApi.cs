using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public interface IProjectsApi
{
    Task<List<ProjectDto>> GetAllAsync(int? clientId = null, CancellationToken ct = default);
    Task<ProjectDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProjectDto> CreateAsync(ProjectFormModel model, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(int id, ProjectFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
