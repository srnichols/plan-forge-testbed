using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class ClientSummaryServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly ClientSummaryService _service;

    public ClientSummaryServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new ClientSummaryService(_db);
    }

    [Fact]
    public async Task GetByClientIdAsync_ExistingClient_ReturnsHappyPath()
    {
        // Arrange
        var client = new Client { Name = "Contoso Ltd", Email = "c@test.com", HourlyRate = 150m, IsActive = true };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var activeProject = new Project { Name = "Web App", ClientId = client.Id, IsActive = true };
        var inactiveProject = new Project { Name = "Old Site", ClientId = client.Id, IsActive = false };
        _db.Projects.AddRange(activeProject, inactiveProject);
        await _db.SaveChangesAsync();

        _db.TimeEntries.AddRange(
            new TimeEntry { ProjectId = activeProject.Id, Date = DateTime.UtcNow, Hours = 8m, IsBillable = true },
            new TimeEntry { ProjectId = activeProject.Id, Date = DateTime.UtcNow, Hours = 2m, IsBillable = false },
            new TimeEntry { ProjectId = inactiveProject.Id, Date = DateTime.UtcNow, Hours = 3m, IsBillable = true }
        );
        await _db.SaveChangesAsync();

        _db.Invoices.AddRange(
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-001", Status = InvoiceStatus.Issued, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 1200m },
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-002", Status = InvoiceStatus.Paid, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 500m }
        );
        await _db.SaveChangesAsync();

        // Act
        ClientActivitySummary? result = await _service.GetByClientIdAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.ClientId);
        Assert.Equal("Contoso Ltd", result.ClientName);
        Assert.Equal(1, result.ProjectCount); // only active projects
        Assert.Equal(13m, result.TotalHours); // 8 + 2 + 3
        Assert.Equal(11m, result.BillableHours); // 8 + 3
        Assert.Equal(2m, result.NonBillableHours);
        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(1200m, result.OutstandingTotal); // only Issued, not Paid
    }

    [Fact]
    public async Task GetByClientIdAsync_ClientNotFound_ReturnsNull()
    {
        // Act
        ClientActivitySummary? result = await _service.GetByClientIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByClientIdAsync_ClientWithNoActivity_ReturnsZeros()
    {
        // Arrange
        var client = new Client { Name = "Empty Corp", Email = "e@test.com", HourlyRate = 100m, IsActive = true };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        // Act
        ClientActivitySummary? result = await _service.GetByClientIdAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.ClientId);
        Assert.Equal("Empty Corp", result.ClientName);
        Assert.Equal(0, result.ProjectCount);
        Assert.Equal(0m, result.TotalHours);
        Assert.Equal(0m, result.BillableHours);
        Assert.Equal(0m, result.NonBillableHours);
        Assert.Equal(0, result.InvoiceCount);
        Assert.Equal(0m, result.OutstandingTotal);
    }

    [Fact]
    public async Task GetByClientIdAsync_MixedBillableNonBillable_CalculatesCorrectly()
    {
        // Arrange
        var client = new Client { Name = "Mixed Inc", Email = "m@test.com", HourlyRate = 75m, IsActive = true };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var project = new Project { Name = "Project A", ClientId = client.Id, IsActive = true };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, Date = DateTime.UtcNow, Hours = 4m, IsBillable = true },
            new TimeEntry { ProjectId = project.Id, Date = DateTime.UtcNow, Hours = 6m, IsBillable = false },
            new TimeEntry { ProjectId = project.Id, Date = DateTime.UtcNow, Hours = 2.5m, IsBillable = true }
        );
        await _db.SaveChangesAsync();

        _db.Invoices.AddRange(
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-A", Status = InvoiceStatus.Draft, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 300m },
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-B", Status = InvoiceStatus.Issued, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 450m },
            new Invoice { ClientId = client.Id, InvoiceNumber = "INV-C", Status = InvoiceStatus.Void, PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow, Total = 100m }
        );
        await _db.SaveChangesAsync();

        // Act
        ClientActivitySummary? result = await _service.GetByClientIdAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12.5m, result.TotalHours); // 4 + 6 + 2.5
        Assert.Equal(6.5m, result.BillableHours); // 4 + 2.5
        Assert.Equal(6m, result.NonBillableHours);
        Assert.Equal(3, result.InvoiceCount);
        Assert.Equal(750m, result.OutstandingTotal); // Draft (300) + Issued (450), not Void
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
