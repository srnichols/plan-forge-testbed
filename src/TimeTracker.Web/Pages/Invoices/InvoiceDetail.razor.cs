using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using TimeTracker.Core.Models;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.Invoices;

public partial class InvoiceDetail : ComponentBase, IDisposable
{
    [Parameter] public int Id { get; set; }

    [Inject] private IInvoicesApi InvoicesApi { get; set; } = default!;
    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private ILogger<InvoiceDetail> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private InvoiceDto? _invoice;
    private string? _clientName;
    private Dictionary<int, string> _projectNames = [];
    private bool _loading = true;
    private bool _working;
    private bool _showVoidForm;
    private string _voidReason = string.Empty;
    private string? _loadError;
    private string? _actionError;

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _loadError = null;

        try
        {
            _invoice = await InvoicesApi.GetByIdAsync(Id, _cts.Token);

            if (_invoice is not null)
            {
                ClientDto? client = await ClientsApi.GetByIdAsync(_invoice.ClientId, _cts.Token);
                _clientName = client?.Name;

                List<ProjectDto> projects = await ProjectsApi.GetAllAsync(_invoice.ClientId, _cts.Token);
                _projectNames = projects.ToDictionary(p => p.Id, p => p.Name);
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load invoice {InvoiceId}", Id);
            _loadError = "We couldn't load this invoice. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    private string ProjectName(int projectId)
        => _projectNames.TryGetValue(projectId, out string? name) ? name : $"Project #{projectId}";

    private IEnumerable<InvoiceAction> AvailableActions()
    {
        if (_invoice is null || _showVoidForm)
        {
            yield break;
        }

        switch (_invoice.Status)
        {
            case InvoiceStatus.Draft:
                yield return new InvoiceAction("Issue", Appearance.Accent, IssueAsync);
                yield return new InvoiceAction("Void", Appearance.Outline, () => { ShowVoid(); return Task.CompletedTask; });
                break;
            case InvoiceStatus.Issued:
                yield return new InvoiceAction("Mark Paid", Appearance.Accent, MarkPaidAsync);
                yield return new InvoiceAction("Void", Appearance.Outline, () => { ShowVoid(); return Task.CompletedTask; });
                break;
            case InvoiceStatus.Paid:
            case InvoiceStatus.Void:
            default:
                break;
        }
    }

    private void ShowVoid()
    {
        _voidReason = string.Empty;
        _actionError = null;
        _showVoidForm = true;
    }

    private void CancelVoid()
    {
        _showVoidForm = false;
        _voidReason = string.Empty;
    }

    private async Task IssueAsync()
    {
        await RunActionAsync(
            async ct => await InvoicesApi.IssueAsync(Id, ct),
            "Could not issue this invoice. Try again.");
    }

    private async Task MarkPaidAsync()
    {
        await RunActionAsync(
            async ct => await InvoicesApi.MarkPaidAsync(Id, ct),
            "Could not mark this invoice as paid. Try again.");
    }

    private async Task ConfirmVoidAsync()
    {
        if (string.IsNullOrWhiteSpace(_voidReason))
        {
            return;
        }

        VoidInvoiceFormModel model = new() { Reason = _voidReason.Trim() };
        bool succeeded = await RunActionAsync(
            async ct => await InvoicesApi.VoidAsync(Id, model, ct),
            "Could not void this invoice. Try again.");

        if (succeeded)
        {
            _showVoidForm = false;
            _voidReason = string.Empty;
        }
    }

    private async Task<bool> RunActionAsync(Func<CancellationToken, Task> action, string errorMessage)
    {
        _working = true;
        _actionError = null;

        try
        {
            await action(_cts.Token);
            await LoadAsync();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Invoice action failed for invoice {InvoiceId}", Id);
            _actionError = errorMessage;
            return false;
        }
        finally
        {
            _working = false;
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

    private sealed record InvoiceAction(string Label, Appearance Appearance, Func<Task> Invoke);
}
