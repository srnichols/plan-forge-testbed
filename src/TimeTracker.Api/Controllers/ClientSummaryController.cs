using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/clients/{id:int}/summary")]
[Produces("application/json")]
public class ClientSummaryController(IClientSummaryService clientSummaryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(int id, CancellationToken ct)
    {
        var summary = await clientSummaryService.GetByClientIdAsync(id, ct);

        if (summary is null)
        {
            return Problem(
                title: "Client not found",
                detail: $"No client exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(summary);
    }
}
