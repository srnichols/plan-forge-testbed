using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<Invoice>> Generate(
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var invoice = await _invoiceService.GenerateInvoiceAsync(
                request.ClientId,
                request.PeriodStart,
                request.PeriodEnd,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Invoice>> GetById(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Invoice>>> GetByClient(
        [FromQuery, Required] int clientId,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var invoices = await _invoiceService.GetClientInvoicesAsync(clientId, cancellationToken);
        return Ok(invoices);
    }

    [HttpPost("{id:int}/issue")]
    public Task<ActionResult<Invoice>> Issue(int id, CancellationToken cancellationToken) =>
        ExecuteTransitionAsync(() => _invoiceService.IssueInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/pay")]
    public Task<ActionResult<Invoice>> Pay(int id, CancellationToken cancellationToken) =>
        ExecuteTransitionAsync(() => _invoiceService.MarkPaidAsync(id, cancellationToken));

    [HttpPost("{id:int}/void")]
    public async Task<ActionResult<Invoice>> Void(
        int id,
        [FromBody] VoidInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return await ExecuteTransitionAsync(() => _invoiceService.VoidInvoiceAsync(id, request.Reason, cancellationToken));
    }

    private async Task<ActionResult<Invoice>> ExecuteTransitionAsync(Func<Task<Invoice?>> action)
    {
        try
        {
            var invoice = await action();
            if (invoice is null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    public sealed record GenerateInvoiceRequest(
        [Range(1, int.MaxValue, ErrorMessage = "ClientId must be greater than 0.")] int ClientId,
        [Required] DateTime PeriodStart,
        [Required] DateTime PeriodEnd);

    public sealed record VoidInvoiceRequest(
        [Required, StringLength(500, MinimumLength = 1)] string Reason);
}
