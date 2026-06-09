using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly TimeTrackerDbContext _db;

    public ClientsController(TimeTrackerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetAll(CancellationToken cancellationToken)
    {
        var clients = await _db.Clients
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetById(int id, CancellationToken cancellationToken)
    {
        var client = await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var client = new Client
        {
            Name = request.Name.Trim(),
            Email = (request.Email ?? string.Empty).Trim(),
            HourlyRate = request.HourlyRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    public sealed record CreateClientRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [EmailAddress, StringLength(200)] string? Email,
        [Range(0.01, 100000.0, ErrorMessage = "HourlyRate must be greater than 0.")] decimal HourlyRate);
}
