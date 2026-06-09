using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetByClient([FromQuery] int? clientId, CancellationToken cancellationToken)
    {
        var invoices = await invoiceService.GetByClientAsync(clientId, cancellationToken);
        return Ok(invoices.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null) return NotFound();
        return Ok(ToResponse(invoice));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GenerateAsync(request.ClientId, request.PeriodStart, request.PeriodEnd, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, ToResponse(invoice));
    }

    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await invoiceService.IssueAsync(id, cancellationToken);
            return Ok(new { invoice.Id, invoice.Status, invoice.IssuedAt });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await invoiceService.MarkPaidAsync(id, cancellationToken);
            return Ok(new { invoice.Id, invoice.Status, invoice.PaidAt });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> Void(int id, [FromBody] VoidInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await invoiceService.VoidAsync(id, request.Reason, cancellationToken);
            return Ok(new { invoice.Id, invoice.Status, invoice.VoidedAt, invoice.VoidReason });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static object ToResponse(Invoice invoice) => new
    {
        invoice.Id,
        invoice.ClientId,
        invoice.InvoiceNumber,
        invoice.Status,
        invoice.PeriodStart,
        invoice.PeriodEnd,
        invoice.Subtotal,
        invoice.DiscountPercent,
        invoice.DiscountAmount,
        invoice.TaxRate,
        invoice.TaxAmount,
        invoice.Total,
        invoice.CreatedAt,
        invoice.IssuedAt,
        invoice.PaidAt,
        invoice.VoidedAt,
        invoice.VoidReason,
        InvoiceLines = (invoice.InvoiceLines ?? []).Select(l => new
        {
            l.Id,
            l.ProjectId,
            l.Description,
            l.Hours,
            l.HourlyRate,
            l.RateType,
            l.LineTotal
        }).ToList()
    };
}

public record GenerateInvoiceRequest(int ClientId, DateTime PeriodStart, DateTime PeriodEnd);
public record VoidInvoiceRequest(string Reason);
