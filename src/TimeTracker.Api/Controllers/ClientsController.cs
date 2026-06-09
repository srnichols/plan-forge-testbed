using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetAll(CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetAllAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetById(int id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetByIdAsync(id, cancellationToken);
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

        try
        {
            var client = await _clientService.CreateAsync(
                request.Name,
                request.Email,
                request.HourlyRate,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Client>> Update(
        int id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var client = await _clientService.UpdateAsync(
                id,
                request.Name,
                request.Email,
                request.HourlyRate,
                cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            return Ok(client);
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
        var deactivated = await _clientService.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return NotFound();
        }

        return NoContent();
    }

    public sealed record CreateClientRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [EmailAddress, StringLength(200)] string? Email,
        [Range(0.01, 100000.0, ErrorMessage = "HourlyRate must be greater than 0.")] decimal HourlyRate);

    public sealed record UpdateClientRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [EmailAddress, StringLength(200)] string? Email,
        [Range(0.01, 100000.0, ErrorMessage = "HourlyRate must be greater than 0.")] decimal HourlyRate);
}
