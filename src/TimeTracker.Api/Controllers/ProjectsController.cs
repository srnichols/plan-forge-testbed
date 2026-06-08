using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(TimeTrackerDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] int? clientId, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.Where(p => p.IsActive);

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        var projects = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.ClientId,
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.ClientId,
                p.IsActive,
                p.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Name is required" });

        var project = new Project
        {
            Name = request.Name,
            ClientId = request.ClientId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, new
        {
            project.Id,
            project.Name,
            project.Description,
            project.ClientId,
            project.IsActive,
            project.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (project is null) return NotFound();

        project.Name = request.Name;
        project.ClientId = request.ClientId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { project.Id, project.Name, project.Description, project.ClientId, project.IsActive, project.CreatedAt });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (project is null) return NotFound();

        project.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record ProjectCreateRequest(string Name, string? Description, int ClientId);
