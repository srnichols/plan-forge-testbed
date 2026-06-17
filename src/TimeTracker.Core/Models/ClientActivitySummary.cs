namespace TimeTracker.Core.Models;

public record ClientActivitySummary(
    int ClientId,
    string ClientName,
    int ProjectCount,
    decimal TotalHours,
    decimal BillableHours,
    decimal NonBillableHours,
    int InvoiceCount,
    decimal OutstandingTotal);
