using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Clients;

public partial class ClientEdit : ComponentBase, IDisposable
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ClientEdit> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private ClientFormModel _form = new();
    private bool _isNew;
    private bool _loading = true;
    private bool _submitting;
    private string? _loadError;
    private string? _submitError;

    protected override async Task OnInitializedAsync()
    {
        _isNew = Id is null;

        if (!_isNew)
        {
            await LoadClientAsync(Id!.Value);
        }
        else
        {
            _loading = false;
        }
    }

    private async Task LoadClientAsync(int id)
    {
        _loading = true;
        _loadError = null;

        try
        {
            ClientDto? client = await ClientsApi.GetByIdAsync(id, _cts.Token);

            if (client is null)
            {
                _loadError = "Client not found.";
                return;
            }

            _form = new ClientFormModel
            {
                Name = client.Name,
                Email = client.Email,
                HourlyRate = client.HourlyRate
            };
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted load
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load client {ClientId}", id);
            _loadError = "We couldn't load this client. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubmitAsync()
    {
        _submitting = true;
        _submitError = null;

        try
        {
            if (_isNew)
            {
                await ClientsApi.CreateAsync(_form, _cts.Token);
            }
            else
            {
                await ClientsApi.UpdateAsync(Id!.Value, _form, _cts.Token);
            }

            Nav.NavigateTo("/clients");
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save client");
            _submitError = "We couldn't save the client. Please try again.";
        }
        finally
        {
            _submitting = false;
        }
    }

    private void Cancel() => Nav.NavigateTo("/clients");

    public void Dispose() => _cts.Cancel();
}
