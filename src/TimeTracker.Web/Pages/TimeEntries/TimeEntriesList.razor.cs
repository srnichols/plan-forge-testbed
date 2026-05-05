using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.TimeEntries;

public partial class TimeEntriesList : ComponentBase, IDisposable
{
    [Inject] private ITimeEntriesApi TimeEntriesApi { get; set; } = default!;
    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<TimeEntriesList> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private IQueryable<TimeEntryDto> _entries = Enumerable.Empty<TimeEntryDto>().AsQueryable();
    private List<ProjectFilterOption> _projectOptions = [];
    private int? _filterProjectId;
    private string _filterProjectIdStr = string.Empty;
    private bool _loading = true;
    private string? _loadError;
    private string? _deleteError;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadProjectsAsync(), LoadEntriesAsync());
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            List<ProjectDto> projects = await ProjectsApi.GetAllAsync(ct: _cts.Token);
            _projectOptions = projects.Select(p => new ProjectFilterOption(p.Id, p.Name)).ToList();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load projects for filter");
        }
    }

    private async Task LoadEntriesAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            List<TimeEntryDto> items = await TimeEntriesApi.GetAllAsync(projectId: _filterProjectId, ct: _cts.Token);
            _entries = items.AsQueryable();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load time entries");
            _loadError = "We couldn't load time entries. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnProjectFilterChangedAsync(string val)
    {
        _filterProjectIdStr = val;
        _filterProjectId = int.TryParse(val, out int id) ? id : null;
        await LoadEntriesAsync();
    }

    private async Task ClearFilterAsync()
    {
        _filterProjectIdStr = string.Empty;
        _filterProjectId = null;
        await LoadEntriesAsync();
    }

    private void NavigateToCreate() => Nav.NavigateTo("/time-entries/new");

    private async Task DeleteAsync(int id)
    {
        _deleteError = null;

        try
        {
            await TimeEntriesApi.DeleteAsync(id, _cts.Token);
            await LoadEntriesAsync();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete time entry {EntryId}", id);
            _deleteError = "Could not delete entry. Try again.";
        }
    }

    public void Dispose() => _cts.Cancel();
}

public record ProjectFilterOption(int Id, string Name);
