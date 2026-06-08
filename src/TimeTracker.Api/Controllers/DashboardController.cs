using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;

namespace TimeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(TimeTrackerDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var totalClients = await dbContext.Clients.CountAsync(c => c.IsActive, cancellationToken);
        var totalProjects = await dbContext.Projects.CountAsync(p => p.IsActive, cancellationToken);
        var totalTimeEntries = await dbContext.TimeEntries.CountAsync(cancellationToken);
        var totalHours = await dbContext.TimeEntries.SumAsync(t => t.Hours, cancellationToken);
        var billableHours = await dbContext.TimeEntries.Where(t => t.IsBillable).SumAsync(t => t.Hours, cancellationToken);
        var totalInvoices = await dbContext.Invoices.CountAsync(cancellationToken);

        return Ok(new
        {
            TotalClients = totalClients,
            TotalProjects = totalProjects,
            TotalTimeEntries = totalTimeEntries,
            TotalHoursLogged = totalHours,
            BillableHours = billableHours,
            NonBillableHours = totalHours - billableHours,
            TotalInvoices = totalInvoices,
            OutstandingInvoiceTotal = 0m
        });
    }
}
