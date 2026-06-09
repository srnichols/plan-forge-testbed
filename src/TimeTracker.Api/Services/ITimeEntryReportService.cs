using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface ITimeEntryReportService
{
    Task<HoursSummaryResponse> GetHoursSummaryAsync(
        DateOnly start,
        DateOnly end,
        int? projectId = null,
        int? clientId = null,
        CancellationToken cancellationToken = default);

    Task<ProjectBreakdownResponse> GetProjectBreakdownAsync(
        DateOnly start,
        DateOnly end,
        int? clientId = null,
        CancellationToken cancellationToken = default);

    Task<DailyTimelineResponse> GetDailyTimelineAsync(
        DateOnly start,
        DateOnly end,
        int? projectId = null,
        int? clientId = null,
        CancellationToken cancellationToken = default);
}
