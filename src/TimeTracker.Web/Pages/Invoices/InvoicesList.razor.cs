using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using TimeTracker.Core.Models;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Invoices;

public partial class InvoicesList : ComponentBase, IDisposable
{
    [Inject] private IInvoicesApi InvoicesApi { get; set; } = default!;
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private ILogger<InvoicesList> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private IQueryable<InvoiceDto> _invoices = Enumerable.Empty<InvoiceDto>().AsQueryable();
    private List<ClientInvoiceOption> _clientOptions = [];
    private int? _selectedClientId;
    private string _selectedClientIdStr = string.Empty;
    private bool _loading = true;
    private string? _loadError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            List<ClientDto> clients = await ClientsApi.GetAllAsync(_cts.Token);
            _clientOptions = clients.Select(c => new ClientInvoiceOption(c.Id, c.Name)).ToList();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
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

    private async Task OnClientChangedAsync(string val)
    {
        _selectedClientIdStr = val;
        _selectedClientId = int.TryParse(val, out int id) ? id : null;

        if (_selectedClientId is null)
        {
            _invoices = Enumerable.Empty<InvoiceDto>().AsQueryable();
            return;
        }

        _loading = true;
        _loadError = null;

        try
        {
            List<InvoiceDto> items = await InvoicesApi.GetByClientAsync(_selectedClientId.Value, _cts.Token);
            _invoices = items.AsQueryable();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load invoices for client {ClientId}", _selectedClientId);
            _loadError = "We couldn't load invoices. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private static Appearance GetStatusAppearance(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => Appearance.Neutral,
        InvoiceStatus.Issued => Appearance.Accent,
        InvoiceStatus.Paid => Appearance.Accent,
        InvoiceStatus.Void => Appearance.Outline,
        _ => Appearance.Neutral
    };

    public void Dispose() => _cts.Cancel();
}

public record ClientInvoiceOption(int Id, string Name);
