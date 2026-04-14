using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await dashboardService.GetSummaryAsync(ct);
        return Ok(summary);
    }
}
