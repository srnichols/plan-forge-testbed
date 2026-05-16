using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Clients;

public partial class ClientsList : ComponentBase, IDisposable
{
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ClientsList> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private IQueryable<ClientDto> _clients = Enumerable.Empty<ClientDto>().AsQueryable();
    private bool _loading = true;
    private string? _loadError;
    private string? _deleteError;

    protected override async Task OnInitializedAsync()
    {
        await LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            List<ClientDto> items = await ClientsApi.GetAllAsync(_cts.Token);
            _clients = items.AsQueryable();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted load — no action needed
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load clients");
            _loadError = "We couldn't load clients. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private void NavigateToCreate() => Nav.NavigateTo("/clients/new");

    private void NavigateToEdit(int id) => Nav.NavigateTo($"/clients/{id}/edit");

    private async Task DeleteAsync(int id, string name)
    {
        _deleteError = null;

        try
        {
            await ClientsApi.DeleteAsync(id, _cts.Token);
            await LoadClientsAsync();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete client {ClientId} ({ClientName})", id, name);
            _deleteError = $"Could not delete \"{name}\". Try again.";
        }
    }

    public void Dispose() => _cts.Cancel();
}
