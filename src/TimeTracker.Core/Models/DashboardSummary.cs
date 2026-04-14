namespace TimeTracker.Core.Models;

public record DashboardSummary(
    int TotalClients,
    int TotalProjects,
    int TotalTimeEntries,
    decimal TotalHoursLogged,
    decimal BillableHours,
    decimal NonBillableHours,
    int TotalInvoices,
    decimal OutstandingInvoiceTotal);
