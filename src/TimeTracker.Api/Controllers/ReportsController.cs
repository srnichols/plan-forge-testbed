using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ITimeEntryReportService _reportService;

    public ReportsController(ITimeEntryReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("hours-summary")]
    public Task<ActionResult<HoursSummaryResponse>> GetHoursSummary(
        [FromQuery, Required] DateOnly start,
        [FromQuery, Required] DateOnly end,
        [FromQuery] int? projectId,
        [FromQuery] int? clientId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _reportService.GetHoursSummaryAsync(start, end, projectId, clientId, cancellationToken));

    [HttpGet("project-breakdown")]
    public Task<ActionResult<ProjectBreakdownResponse>> GetProjectBreakdown(
        [FromQuery, Required] DateOnly start,
        [FromQuery, Required] DateOnly end,
        [FromQuery] int? clientId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _reportService.GetProjectBreakdownAsync(start, end, clientId, cancellationToken));

    [HttpGet("daily-timeline")]
    public Task<ActionResult<DailyTimelineResponse>> GetDailyTimeline(
        [FromQuery, Required] DateOnly start,
        [FromQuery, Required] DateOnly end,
        [FromQuery] int? projectId,
        [FromQuery] int? clientId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _reportService.GetDailyTimelineAsync(start, end, projectId, clientId, cancellationToken));

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await action();
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
