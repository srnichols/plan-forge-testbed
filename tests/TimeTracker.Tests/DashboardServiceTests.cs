using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new DashboardService(_db);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyDatabase_ReturnsAllZeros()
    {
        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalClients);
        Assert.Equal(0, summary.TotalProjects);
        Assert.Equal(0, summary.TotalTimeEntries);
        Assert.Equal(0m, summary.TotalHoursLogged);
        Assert.Equal(0m, summary.BillableHours);
        Assert.Equal(0m, summary.NonBillableHours);
        Assert.Equal(0, summary.TotalInvoices);
        Assert.Equal(0m, summary.OutstandingInvoiceTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_WithData_ReturnsTotals()
    {
        // Arrange
        var client1 = new Client { Name = "Active Corp", Email = "a@test.com", HourlyRate = 100m, IsActive = true };
        var client2 = new Client { Name = "Inactive Inc", Email = "b@test.com", HourlyRate = 50m, IsActive = false };
        _db.Clients.AddRange(client1, client2);
        await _db.SaveChangesAsync();

        var project = new Project { Name = "Web App", ClientId = client1.Id, IsActive = true };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, Date = DateTime.UtcNow, Hours = 8m, IsBillable = true },
            new TimeEntry { ProjectId = project.Id, Date = DateTime.UtcNow, Hours = 2m, IsBillable = false }
        );
        await _db.SaveChangesAsync();

        _db.Invoices.Add(new Invoice
        {
            ClientId = client1.Id,
            InvoiceNumber = "INV-001",
            Status = InvoiceStatus.Issued,
            PeriodStart = DateTime.UtcNow.AddDays(-30),
            PeriodEnd = DateTime.UtcNow,
            Total = 800m
        });
        await _db.SaveChangesAsync();

        // Act
        var summary = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(1, summary.TotalClients);       // only active
        Assert.Equal(1, summary.TotalProjects);       // only active
        Assert.Equal(2, summary.TotalTimeEntries);
        Assert.Equal(10m, summary.TotalHoursLogged);  // 8 + 2
        Assert.Equal(8m, summary.BillableHours);
        Assert.Equal(2m, summary.NonBillableHours);
        Assert.Equal(1, summary.TotalInvoices);
        Assert.Equal(800m, summary.OutstandingInvoiceTotal); // Issued = outstanding
    }

    [Fact]
    public async Task GetSummaryAsync_PaidInvoices_NotCountedAsOutstanding()
    {
        // Arrange
        var client = new Client { Name = "Test", Email = "t@t.com", HourlyRate = 100m };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        _db.Invoices.AddRange(
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-001", Status = InvoiceStatus.Draft, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 500m },
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-002", Status = InvoiceStatus.Paid, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 1000m },
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-003", Status = InvoiceStatus.Void, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 200m }
        );
        await _db.SaveChangesAsync();

        // Act
        var summary = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(3, summary.TotalInvoices);
        Assert.Equal(500m, summary.OutstandingInvoiceTotal); // Only Draft counts
    }

    [Fact]
    public async Task GetSummaryAsync_MixedActiveInactive_CountsCorrectly()
    {
        // Arrange
        var active1 = new Client { Name = "A1", Email = "a@t.com", HourlyRate = 100m, IsActive = true };
        var active2 = new Client { Name = "A2", Email = "b@t.com", HourlyRate = 100m, IsActive = true };
        var inactive = new Client { Name = "I1", Email = "c@t.com", HourlyRate = 100m, IsActive = false };
        _db.Clients.AddRange(active1, active2, inactive);
        await _db.SaveChangesAsync();

        var activeProject = new Project { Name = "Active", ClientId = active1.Id, IsActive = true };
        var inactiveProject = new Project { Name = "Inactive", ClientId = active1.Id, IsActive = false };
        _db.Projects.AddRange(activeProject, inactiveProject);
        await _db.SaveChangesAsync();

        // Act
        var summary = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(2, summary.TotalClients);   // 2 active
        Assert.Equal(1, summary.TotalProjects);   // 1 active
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
