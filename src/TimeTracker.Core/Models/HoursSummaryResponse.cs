namespace TimeTracker.Core.Models;

public record HoursSummaryResponse(
    decimal TotalHours,
    decimal BillableHours,
    decimal NonBillableHours,
    int EntryCount,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);
