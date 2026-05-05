namespace TimeTracker.Web.Client.Models;

public record DashboardSummaryDto(
    int TotalClients,
    int TotalProjects,
    int TotalTimeEntries,
    decimal TotalHoursLogged,
    decimal BillableHours,
    decimal NonBillableHours,
    int TotalInvoices,
    decimal OutstandingInvoiceTotal);
