using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController : ControllerBase
{
    private readonly TimeTrackerDbContext _db;

    public TimeEntriesController(TimeTrackerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TimeEntry>>> GetAll([FromQuery] DateTime? date, [FromQuery] int? projectId)
    {
        var query = _db.TimeEntries.Include(t => t.Project).AsQueryable();
        if (date.HasValue) query = query.Where(t => t.Date.Date == date.Value.Date);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TimeEntry>> GetById(int id)
    {
        var entry = await _db.TimeEntries.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult<TimeEntry>> Create(CreateTimeEntryRequest request)
    {
        var project = await _db.Projects.FindAsync(request.ProjectId);
        if (project is null) return BadRequest("Project not found");

        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            Date = request.Date,
            Hours = request.Hours,
            Description = request.Description,
            IsBillable = request.IsBillable,
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry is null) return NotFound();
        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateTimeEntryRequest(int ProjectId, DateTime Date, decimal Hours, string? Description, bool IsBillable = true);
