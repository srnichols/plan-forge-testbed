using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] int? clientId, CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(clientId, cancellationToken);
        return Ok(projects.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(id, cancellationToken);
        if (project is null) return NotFound();
        return Ok(ToResponse(project));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                ClientId = request.ClientId
            };

            var created = await projectService.CreateAsync(project, cancellationToken);
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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                ClientId = request.ClientId
            };

            var updated = await projectService.UpdateAsync(id, project, cancellationToken);
            return Ok(ToResponse(updated));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
            await projectService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static object ToResponse(Project project) => new
    {
        project.Id,
        project.Name,
        project.Description,
        project.ClientId,
        project.IsActive,
        project.CreatedAt
    };
}

public record ProjectCreateRequest(string Name, string? Description, int ClientId);
