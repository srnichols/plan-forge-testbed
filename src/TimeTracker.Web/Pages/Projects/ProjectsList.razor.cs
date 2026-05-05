using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Projects;

public partial class ProjectsList : ComponentBase, IDisposable
{
    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ProjectsList> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private IQueryable<ProjectDto> _projects = Enumerable.Empty<ProjectDto>().AsQueryable();
    private bool _loading = true;
    private string? _loadError;
    private string? _deleteError;

    protected override async Task OnInitializedAsync()
    {
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            List<ProjectDto> items = await ProjectsApi.GetAllAsync(ct: _cts.Token);
            _projects = items.AsQueryable();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted load — no action needed
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load projects");
            _loadError = "We couldn't load projects. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void NavigateToCreate() => Nav.NavigateTo("/projects/new");

    private void NavigateToEdit(int id) => Nav.NavigateTo($"/projects/{id}/edit");

    private async Task DeleteAsync(int id, string name)
    {
        _deleteError = null;

        try
        {
            await ProjectsApi.DeleteAsync(id, _cts.Token);
            await LoadProjectsAsync();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete project {ProjectId} ({ProjectName})", id, name);
            _deleteError = $"Could not delete \"{name}\". Try again.";
        }
    }

    public void Dispose() => _cts.Cancel();
}
