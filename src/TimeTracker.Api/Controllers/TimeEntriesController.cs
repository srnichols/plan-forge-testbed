using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController(ITimeEntryService timeEntryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] DateTime? date, [FromQuery] int? projectId, CancellationToken cancellationToken)
    {
        var entries = await timeEntryService.GetAllAsync(date, projectId, cancellationToken);
        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entry = await timeEntryService.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound();
        return Ok(ToResponse(entry));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TimeEntryCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = new TimeEntry
            {
                ProjectId = request.ProjectId,
                Date = request.Date,
                Hours = request.Hours,
                Description = request.Description ?? string.Empty,
                IsBillable = request.IsBillable
            };

            var created = await timeEntryService.CreateAsync(entry, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await timeEntryService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static object ToResponse(TimeEntry entry) => new
    {
        entry.Id,
        entry.ProjectId,
        entry.Date,
        entry.Hours,
        entry.Description,
        entry.IsBillable,
        entry.CreatedAt
    };
}

public record TimeEntryCreateRequest(int ProjectId, DateTime Date, decimal Hours, string? Description, bool IsBillable = true);
