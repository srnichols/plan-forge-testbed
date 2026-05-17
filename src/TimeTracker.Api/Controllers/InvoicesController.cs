using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using TimeTracker.Api.Services;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request, CancellationToken ct)
    {
        if (request.ClientId <= 0)
            return BadRequest(new { error = "ClientId must be a positive integer." });

        try
        {
            var invoice = await invoiceService.GenerateInvoiceAsync(
                request.ClientId, request.PeriodStart, request.PeriodEnd, ct);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var invoice = await invoiceService.GetInvoiceAsync(id, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet]
    public async Task<IActionResult> GetByClient([FromQuery, BindRequired] int clientId, CancellationToken ct)
    {
        if (clientId <= 0)
            return BadRequest(new { error = "clientId must be a positive integer." });

        var invoices = await invoiceService.GetClientInvoicesAsync(clientId, ct);
        return Ok(invoices);
    }

    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.IssueInvoiceAsync(id, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> Pay(int id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.MarkPaidAsync(id, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> VoidInvoice(int id, [FromBody] VoidInvoiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "Reason is required to void an invoice." });

        try
        {
            var invoice = await invoiceService.VoidInvoiceAsync(id, request.Reason, ct);
            return Ok(invoice);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}

public sealed record GenerateInvoiceRequest(int ClientId, DateTime PeriodStart, DateTime PeriodEnd);
public sealed record VoidInvoiceRequest(string Reason);
