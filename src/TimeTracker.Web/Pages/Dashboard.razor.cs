using Microsoft.AspNetCore.Components;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages;

public partial class Dashboard : ComponentBase, IDisposable
{
    [Inject] private IDashboardApi DashboardApi { get; set; } = default!;
    [Inject] private ILogger<Dashboard> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private DashboardSummaryDto? _summary;
    private bool _loading = true;
    private string? _loadError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _summary = await DashboardApi.GetSummaryAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Navigation aborted load — no action needed
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load dashboard summary");
            _loadError = "We couldn't load the dashboard. Try refreshing.";
        }
        finally
        {
            _loading = false;
        }
    }

    public void Dispose() => _cts.Cancel();
}
