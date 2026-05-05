using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Projects;

public partial class ProjectEdit : ComponentBase, IDisposable
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ProjectEdit> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private readonly ProjectFormModel _model = new();
    private List<ClientOption> _clientOptions = [];
    private bool _loading = true;
    private bool _saving;
    private string? _loadError;
    private string? _saveError;

    private string _clientIdStr
    {
        get => _model.ClientId > 0 ? _model.ClientId.ToString() : string.Empty;
        set
        {
            if (int.TryParse(value, out int id))
                _model.ClientId = id;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            List<ClientDto> clients = await ClientsApi.GetAllAsync(_cts.Token);
            _clientOptions = clients.Select(c => new ClientOption(c.Id, c.Name)).ToList();

            if (Id.HasValue)
            {
                ProjectDto? project = await ProjectsApi.GetByIdAsync(Id.Value, _cts.Token);
                if (project is null)
                {
                    _loadError = "Project not found.";
                    return;
                }
                _model.Name = project.Name;
                _model.Description = project.Description;
                _model.ClientId = project.ClientId;
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load project {ProjectId}", Id);
            _loadError = "We couldn't load the project. Try refreshing.";
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
            if (Id.HasValue)
                await ProjectsApi.UpdateAsync(Id.Value, _model, _cts.Token);
            else
                await ProjectsApi.CreateAsync(_model, _cts.Token);

            Nav.NavigateTo("/projects");
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save project {ProjectId}", Id);
            _saveError = "We couldn't save the project. Please try again.";
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => Nav.NavigateTo("/projects");

    public void Dispose() => _cts.Cancel();
}

public record ClientOption(int Id, string Name);
