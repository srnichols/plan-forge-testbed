using System.Net.Http.Json;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public class ProjectsApi(HttpClient httpClient) : IProjectsApi
{
    public async Task<List<ProjectDto>> GetAllAsync(int? clientId = null, CancellationToken ct = default)
    {
        var url = clientId.HasValue ? $"api/projects?clientId={clientId}" : "api/projects";
        return await httpClient.GetFromJsonAsync<List<ProjectDto>>(url, ct) ?? [];
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/projects/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(ct);
    }

    public async Task<ProjectDto> CreateAsync(ProjectFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/projects", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectDto>(ct))!;
    }

    public async Task<ProjectDto> UpdateAsync(int id, ProjectFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/projects/{id}", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectDto>(ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/projects/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
