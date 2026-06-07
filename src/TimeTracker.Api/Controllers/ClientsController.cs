using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IClientService clientService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetAll(CancellationToken cancellationToken)
    {
        var clients = await clientService.GetAllAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetById(int id, CancellationToken cancellationToken)
    {
        var client = await clientService.GetByIdAsync(id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create([FromBody] Client client, CancellationToken cancellationToken)
    {
        try
        {
            var created = await clientService.CreateAsync(client, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
    public async Task<ActionResult<Client>> Update(int id, [FromBody] Client client, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await clientService.UpdateAsync(id, client, cancellationToken);
            return Ok(updated);
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
            await clientService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
