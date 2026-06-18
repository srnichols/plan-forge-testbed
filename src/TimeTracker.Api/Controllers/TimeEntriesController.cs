using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController : ControllerBase
{
    private readonly ITimeEntryService _timeEntryService;

    public TimeEntriesController(ITimeEntryService timeEntryService)
    {
        _timeEntryService = timeEntryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimeEntry>>> GetAll(
        [FromQuery] DateTime? date,
        [FromQuery] int? projectId,
        CancellationToken cancellationToken)
    {
        var entries = await _timeEntryService.GetAllAsync(date, projectId, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TimeEntry>> GetById(int id, CancellationToken cancellationToken)
    {
        var entry = await _timeEntryService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult<TimeEntry>> Create(
        [FromBody] CreateTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entry = await _timeEntryService.CreateAsync(
                request.ProjectId,
                request.Date,
                request.Hours,
                request.Description,
                request.IsBillable,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _timeEntryService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    public sealed record CreateTimeEntryRequest(
        [Range(1, int.MaxValue, ErrorMessage = "Project is required")] int ProjectId,
        [Required] DateTime Date,
        [Range(0.01, 24.0, ErrorMessage = "Hours must be between 0.01 and 24")] decimal Hours,
        [MaxLength(1000)] string? Description,
        bool IsBillable);
}
