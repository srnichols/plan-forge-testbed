namespace TimeTracker.Core.Models;

public record ProjectBreakdownItem(
    int ProjectId,
    string ProjectName,
    decimal TotalHours,
    decimal BillableHours,
    decimal NonBillableHours,
    decimal PercentageOfTotal);

public record ProjectBreakdownResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalHours,
    IReadOnlyList<ProjectBreakdownItem> Projects);
