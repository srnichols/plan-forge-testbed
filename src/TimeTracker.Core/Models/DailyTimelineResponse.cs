namespace TimeTracker.Core.Models;

public record DailyTimelineEntry(
    DateOnly Date,
    decimal TotalHours,
    decimal BillableHours,
    decimal NonBillableHours,
    int EntryCount);

public record DailyTimelineResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalHours,
    IReadOnlyList<DailyTimelineEntry> Days);
