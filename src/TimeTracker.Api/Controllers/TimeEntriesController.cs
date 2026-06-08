using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController(TimeTrackerDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] DateTime? date, [FromQuery] int? projectId, CancellationToken cancellationToken)
    {
        var query = dbContext.TimeEntries.AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(t => t.Date.Date == date.Value.Date);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        var entries = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.Date,
                t.Hours,
                t.Description,
                t.IsBillable,
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.TimeEntries
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.Date,
                t.Hours,
                t.Description,
                t.IsBillable,
                t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null) return NotFound();
        return Ok(entry);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TimeEntryCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.Hours <= 0 || request.Hours > 24)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Hours must be between 0.01 and 24" });

        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            Date = request.Date,
            Hours = request.Hours,
            Description = request.Description ?? string.Empty,
            IsBillable = request.IsBillable,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TimeEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, new
        {
            entry.Id,
            entry.ProjectId,
            entry.Date,
            entry.Hours,
            entry.Description,
            entry.IsBillable,
            entry.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.TimeEntries.FindAsync([id], cancellationToken);
        if (entry is null) return NotFound();

        dbContext.TimeEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record TimeEntryCreateRequest(int ProjectId, DateTime Date, decimal Hours, string? Description, bool IsBillable = true);
