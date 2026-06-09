using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetAll(
        [FromQuery] int? clientId,
        CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetAllAsync(cancellationToken);
        if (clientId.HasValue)
        {
            projects = projects.Where(p => p.ClientId == clientId.Value).ToList();
        }

        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Project>> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<Project>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var project = await _projectService.CreateAsync(
                request.Name,
                request.Description,
                request.ClientId,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Project>> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var project = await _projectService.UpdateAsync(
                id,
                request.Name,
                request.Description,
                request.ClientId,
                cancellationToken);

            if (project is null)
            {
                return NotFound();
            }

            return Ok(project);
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
        var deactivated = await _projectService.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return NotFound();
        }

        return NoContent();
    }

    public sealed record CreateProjectRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [StringLength(2000)] string? Description,
        [Range(1, int.MaxValue, ErrorMessage = "ClientId is required.")] int ClientId);

    public sealed record UpdateProjectRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [StringLength(2000)] string? Description,
        [Range(1, int.MaxValue, ErrorMessage = "ClientId is required.")] int ClientId);
}
