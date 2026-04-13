using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController(ITimeEntryReportService reportService) : ControllerBase
{
    [HttpGet("hours-summary")]
    public async Task<IActionResult> GetHoursSummary(
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        [FromQuery] int? projectId,
        CancellationToken ct)
    {
        if (start > end)
            return Problem(
                title: "Invalid date range",
                detail: "Start date must be on or before end date.",
                statusCode: 400);

        var result = await reportService.GetHoursSummaryAsync(start, end, projectId, ct);
        return Ok(result);
    }

    [HttpGet("project-breakdown")]
    public async Task<IActionResult> GetProjectBreakdown(
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        CancellationToken ct)
    {
        if (start > end)
            return Problem(
                title: "Invalid date range",
                detail: "Start date must be on or before end date.",
                statusCode: 400);

        var result = await reportService.GetProjectBreakdownAsync(start, end, ct);
        return Ok(result);
    }

    [HttpGet("daily-timeline")]
    public async Task<IActionResult> GetDailyTimeline(
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        [FromQuery] int? projectId,
        CancellationToken ct)
    {
        if (start > end)
            return Problem(
                title: "Invalid date range",
                detail: "Start date must be on or before end date.",
                statusCode: 400);

        var result = await reportService.GetDailyTimelineAsync(start, end, projectId, ct);
        return Ok(result);
    }
}
