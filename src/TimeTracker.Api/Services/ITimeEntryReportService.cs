using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface ITimeEntryReportService
{
    Task<HoursSummaryResponse> GetHoursSummaryAsync(
        DateOnly start, DateOnly end, int? projectId = null, CancellationToken ct = default);

    Task<ProjectBreakdownResponse> GetProjectBreakdownAsync(
        DateOnly start, DateOnly end, CancellationToken ct = default);

    Task<DailyTimelineResponse> GetDailyTimelineAsync(
        DateOnly start, DateOnly end, int? projectId = null, CancellationToken ct = default);
}
