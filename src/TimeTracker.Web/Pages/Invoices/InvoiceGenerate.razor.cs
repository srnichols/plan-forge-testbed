using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Invoices;

public partial class InvoiceGenerate : ComponentBase, IDisposable
{
    [Inject] private IInvoicesApi InvoicesApi { get; set; } = default!;
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<InvoiceGenerate> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private readonly GenerateInvoiceFormModel _form = new();
    private List<ClientDto> _clients = [];
    private string _selectedClientIdStr = string.Empty;
    private DateTime? _periodStart;
    private DateTime? _periodEnd;
    private bool _loading = true;
    private bool _submitting;
    private string? _loadError;
    private string? _submitError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _clients = await ClientsApi.GetAllAsync(_cts.Token);
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

    private void OnClientChanged(string val)
    {
        _selectedClientIdStr = val;
        _form.ClientId = int.TryParse(val, out int id) ? id : 0;
    }

    private async Task SubmitAsync()
    {
        _form.PeriodStart = _periodStart ?? default;
        _form.PeriodEnd = _periodEnd ?? default;

        _submitError = null;

        if (_form.PeriodStart == default || _form.PeriodEnd == default)
        {
            _submitError = "Select both a start and end date for the billing period.";
            return;
        }

        if (_form.PeriodEnd < _form.PeriodStart)
        {
            _submitError = "Period end must be on or after the period start.";
            return;
        }

        _submitting = true;

        try
        {
            InvoiceDto created = await InvoicesApi.GenerateAsync(_form, _cts.Token);
            Nav.NavigateTo($"/invoices/{created.Id}");
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate invoice for client {ClientId}", _form.ClientId);
            _submitError = "Could not generate the invoice. Try again.";
            _submitting = false;
        }
    }

    private void Cancel() => Nav.NavigateTo("/invoices");

    public void Dispose() => _cts.Cancel();
}
