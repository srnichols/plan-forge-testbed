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
    [Inject] private IClientsApi ClientsApi { get; set; } = default!;
    [Inject] private IProjectsApi ProjectsApi { get; set; } = default!;
    [Inject] private ILogger<InvoiceDetail> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private InvoiceDto? _invoice;
    private IQueryable<InvoiceLineDto> _lines = Enumerable.Empty<InvoiceLineDto>().AsQueryable();
    private Dictionary<int, string> _projectNames = [];
    private string _clientName = string.Empty;
    private bool _loading = true;
    private bool _working;
    private string? _loadError;
    private string? _actionError;
    private bool _showVoidForm;
    private VoidInvoiceFormModel _voidForm = new();
    private int _loadedId;

    private List<(string Label, Func<CancellationToken, Task> Action, string ErrorMessage, Appearance Appearance)> _actions = [];

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == Id && _invoice is not null)
        {
            return;
        }

        _loadedId = Id;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _loadError = null;
        _actionError = null;
        _showVoidForm = false;
        _voidForm = new VoidInvoiceFormModel();

        try
        {
            InvoiceDto? invoice = await InvoicesApi.GetByIdAsync(Id, _cts.Token);

            if (invoice is null)
            {
                _invoice = null;
                return;
            }

            _invoice = invoice;
            _lines = invoice.InvoiceLines.AsQueryable();

            await Task.WhenAll(LoadClientNameAsync(invoice.ClientId), LoadProjectNamesAsync(invoice.InvoiceLines));
            RebuildActions();
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

    private async Task LoadClientNameAsync(int clientId)
    {
        try
        {
            ClientDto? client = await ClientsApi.GetByIdAsync(clientId, _cts.Token);
            _clientName = client?.Name ?? $"Client #{clientId}";
        }
        catch (OperationCanceledException)
        {
            _clientName = $"Client #{clientId}";
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to resolve client name for {ClientId}", clientId);
            _clientName = $"Client #{clientId}";
        }
    }

    private async Task LoadProjectNamesAsync(IEnumerable<InvoiceLineDto> lines)
    {
        HashSet<int> ids = lines.Select(l => l.ProjectId).ToHashSet();
        Dictionary<int, string> map = new();

        try
        {
            foreach (int projectId in ids)
            {
                ProjectDto? project = await ProjectsApi.GetByIdAsync(projectId, _cts.Token);
                map[projectId] = project?.Name ?? $"Project #{projectId}";
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to resolve some project names for invoice {InvoiceId}", Id);
        }

        _projectNames = map;
    }

    private string ProjectName(int projectId)
        => _projectNames.TryGetValue(projectId, out string? name) ? name : $"Project #{projectId}";

    private void RebuildActions()
    {
        _actions = [];

        if (_invoice is null)
        {
            return;
        }

        switch (_invoice.Status)
        {
            case InvoiceStatus.Draft:
                _actions.Add(("Issue", ct => InvoicesApi.IssueAsync(Id, ct), "We couldn't issue the invoice.", Appearance.Accent));
                _actions.Add(("Void", _ => { ShowVoidForm(); return Task.CompletedTask; }, string.Empty, Appearance.Outline));
                break;
            case InvoiceStatus.Issued:
                _actions.Add(("Mark Paid", ct => InvoicesApi.MarkPaidAsync(Id, ct), "We couldn't mark the invoice as paid.", Appearance.Accent));
                _actions.Add(("Void", _ => { ShowVoidForm(); return Task.CompletedTask; }, string.Empty, Appearance.Outline));
                break;
        }
    }

    private void ShowVoidForm()
    {
        _showVoidForm = true;
        _voidForm = new VoidInvoiceFormModel();
        _actionError = null;
    }

    private void CancelVoid()
    {
        _showVoidForm = false;
        _voidForm = new VoidInvoiceFormModel();
    }

    private async Task ConfirmVoidAsync()
    {
        await RunActionAsync(ct => InvoicesApi.VoidAsync(Id, _voidForm, ct), "We couldn't void the invoice.");
    }

    private async Task RunActionAsync(Func<CancellationToken, Task> action, string errorMessage)
    {
        if (_working)
        {
            return;
        }

        // Void shows a form first; running the form-opener should not set _working/reload.
        if (string.IsNullOrEmpty(errorMessage))
        {
            await action(_cts.Token);
            return;
        }

        _working = true;
        _actionError = null;

        try
        {
            await action(_cts.Token);
            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Invoice action failed for invoice {InvoiceId}", Id);
            _actionError = errorMessage;
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
}
