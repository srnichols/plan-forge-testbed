using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController(TimeTrackerDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetByClient([FromQuery] int? clientId, CancellationToken cancellationToken)
    {
        var query = dbContext.Invoices.Include(i => i.InvoiceLines).AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(i => i.ClientId == clientId.Value);
        }

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.ClientId,
                i.InvoiceNumber,
                Status = i.Status,
                i.PeriodStart,
                i.PeriodEnd,
                i.Subtotal,
                i.DiscountPercent,
                i.DiscountAmount,
                i.TaxRate,
                i.TaxAmount,
                i.Total,
                i.CreatedAt,
                i.IssuedAt,
                i.PaidAt,
                i.VoidedAt,
                i.VoidReason,
                InvoiceLines = i.InvoiceLines.Select(l => new
                {
                    l.Id,
                    l.ProjectId,
                    l.Description,
                    l.Hours,
                    l.HourlyRate,
                    l.RateType,
                    l.LineTotal
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.InvoiceLines)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null) return NotFound();

        return Ok(new
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
            InvoiceLines = invoice.InvoiceLines.Select(l => new
            {
                l.Id,
                l.ProjectId,
                l.Description,
                l.Hours,
                l.HourlyRate,
                l.RateType,
                l.LineTotal
            }).ToList()
        });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = new Invoice
        {
            ClientId = request.ClientId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            Status = InvoiceStatus.Draft,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Subtotal = 0m,
            Total = 0m,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, new
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
            InvoiceLines = new List<object>()
        });
    }

    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FindAsync([id], cancellationToken);
        if (invoice is null) return NotFound();

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { invoice.Id, invoice.Status, invoice.IssuedAt });
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FindAsync([id], cancellationToken);
        if (invoice is null) return NotFound();

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { invoice.Id, invoice.Status, invoice.PaidAt });
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> Void(int id, [FromBody] VoidInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FindAsync([id], cancellationToken);
        if (invoice is null) return NotFound();

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = request.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { invoice.Id, invoice.Status, invoice.VoidedAt, invoice.VoidReason });
    }
}

public record GenerateInvoiceRequest(int ClientId, DateTime PeriodStart, DateTime PeriodEnd);
public record VoidInvoiceRequest(string Reason);
