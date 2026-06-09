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
    private readonly GenerateInvoiceFormModel _form = new()
    {
        PeriodStart = DateTime.Today.AddDays(-30),
        PeriodEnd = DateTime.Today
    };
    private List<ClientDto> _clients = [];
    private string _clientIdStr = string.Empty;
    private bool _loading = true;
    private bool _submitting;
    private string? _loadError;
    private string? _submitError;
    private string? _validationError;

    private DateTime? _periodStartProxy
    {
        get => _form.PeriodStart == default ? null : _form.PeriodStart;
        set => _form.PeriodStart = value ?? default;
    }

    private DateTime? _periodEndProxy
    {
        get => _form.PeriodEnd == default ? null : _form.PeriodEnd;
        set => _form.PeriodEnd = value ?? default;
    }

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

    private void OnClientChanged(string value)
    {
        _clientIdStr = value;
        _form.ClientId = int.TryParse(value, out int id) ? id : 0;
    }

    private async Task SubmitAsync()
    {
        _validationError = null;
        _submitError = null;

        if (_form.PeriodEnd < _form.PeriodStart)
        {
            _validationError = "Period end must be on or after period start.";
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
            _submitError = "We couldn't generate the invoice. Please try again.";
        }
        finally
        {
            _submitting = false;
        }
    }

    private void Cancel() => Nav.NavigateTo("/invoices");

    public void Dispose() => _cts.Cancel();
}
