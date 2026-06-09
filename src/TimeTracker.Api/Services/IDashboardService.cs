namespace TimeTracker.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardSummary(
    int TotalClients,
    int TotalProjects,
    int TotalTimeEntries,
    decimal TotalHoursLogged,
    decimal BillableHours,
    decimal NonBillableHours,
    int TotalInvoices,
    decimal OutstandingInvoiceTotal);
