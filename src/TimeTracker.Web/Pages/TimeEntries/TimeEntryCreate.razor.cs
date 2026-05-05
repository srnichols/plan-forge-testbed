using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.TimeEntries;

public partial class TimeEntryCreate : ComponentBase, IDisposable
{
    [Inject] private ITimeEntriesApi TimeEntriesApi { get; set; } = default!;
    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<TimeEntryCreate> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private readonly TimeEntryFormModel _model = new();
    private List<ProjectOption> _projectOptions = [];
    private bool _loading = true;
    private bool _saving;
    private string? _loadError;
    private string? _saveError;

    private string _projectIdStr
    {
        get => _model.ProjectId > 0 ? _model.ProjectId.ToString() : string.Empty;
        set
        {
            if (int.TryParse(value, out int id))
                _model.ProjectId = id;
        }
    }

    private DateTime? _dateProxy
    {
        get => _model.Date;
        set => _model.Date = value ?? DateTime.Today;
    }

    private double _hoursProxy
    {
        get => (double)_model.Hours;
        set => _model.Hours = (decimal)value;
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            List<ProjectDto> projects = await ProjectsApi.GetAllAsync(ct: _cts.Token);
            _projectOptions = projects.Select(p => new ProjectOption(p.Id, p.Name)).ToList();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
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

    private async Task HandleSubmitAsync()
    {
        _saving = true;
        _saveError = null;

        try
        {
            await TimeEntriesApi.CreateAsync(_model, _cts.Token);
            Nav.NavigateTo("/time-entries");
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create time entry");
            _saveError = "We couldn't save the time entry. Please try again.";
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => Nav.NavigateTo("/time-entries");

    public void Dispose() => _cts.Cancel();
}

public record ProjectOption(int Id, string Name);
