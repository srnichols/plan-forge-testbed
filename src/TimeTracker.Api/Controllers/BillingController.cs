using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billing;

    public BillingController(IBillingService billing) => _billing = billing;

    [HttpGet("summary")]
    public async Task<ActionResult<BillingSummary>> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? clientId)
    {
        if (endDate < startDate)
            return BadRequest("endDate must be after startDate");

        var summary = await _billing.GetBillingSummaryAsync(startDate, endDate, clientId);
        return Ok(summary);
    }
}
