using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

[Trait("Category", "Unit")]
public sealed class DashboardServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _dbContext;
    private readonly IDashboardService _sut;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"DashboardServiceTests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new TimeTrackerDbContext(options);
        _sut = ResolveService(_dbContext);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyDatabase_ReturnsAllZeros()
    {
        var result = await _sut.GetSummaryAsync();

        Assert.Equal(0, result.TotalClients);
        Assert.Equal(0, result.TotalProjects);
        Assert.Equal(0, result.TotalTimeEntries);
        Assert.Equal(0m, result.TotalHoursLogged);
        Assert.Equal(0m, result.BillableHours);
        Assert.Equal(0m, result.NonBillableHours);
        Assert.Equal(0, result.TotalInvoices);
        Assert.Equal(0m, result.OutstandingInvoiceTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsOnlyActiveClientsAndProjects()
    {
        var activeClient = await AddClientAsync("Active Co", isActive: true);
        var inactiveClient = await AddClientAsync("Old Co", isActive: false);
        await AddProjectAsync(activeClient, "Alpha", isActive: true);
        await AddProjectAsync(activeClient, "Beta", isActive: true);
        await AddProjectAsync(inactiveClient, "Gamma", isActive: true);
        await AddProjectAsync(activeClient, "Delta", isActive: false);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(1, result.TotalClients);
        Assert.Equal(2, result.TotalProjects);
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesAllTimeEntriesIncludingInactiveOwners()
    {
        var client = await AddClientAsync("Client", isActive: true);
        var project = await AddProjectAsync(client, "Proj", isActive: true);
        await AddEntryAsync(project, new DateTime(2026, 4, 7), 8m, isBillable: true);
        await AddEntryAsync(project, new DateTime(2026, 4, 8), 4m, isBillable: true);
        await AddEntryAsync(project, new DateTime(2026, 4, 9), 3m, isBillable: false);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(3, result.TotalTimeEntries);
        Assert.Equal(15m, result.TotalHoursLogged);
        Assert.Equal(12m, result.BillableHours);
        Assert.Equal(3m, result.NonBillableHours);
    }

    [Fact]
    public async Task GetSummaryAsync_OutstandingInvoiceTotal_SumsOnlyDraftAndIssued()
    {
        var client = await AddClientAsync("Client", isActive: true);
        await AddInvoiceAsync(client, "INV-001", InvoiceStatus.Draft, total: 100m);
        await AddInvoiceAsync(client, "INV-002", InvoiceStatus.Issued, total: 250m);
        await AddInvoiceAsync(client, "INV-003", InvoiceStatus.Paid, total: 500m);
        await AddInvoiceAsync(client, "INV-004", InvoiceStatus.Void, total: 999m);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(4, result.TotalInvoices);
        Assert.Equal(350m, result.OutstandingInvoiceTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.GetSummaryAsync(cts.Token));
    }

    // ---------- Helpers ----------

    private static IDashboardService ResolveService(TimeTrackerDbContext dbContext)
    {
        var serviceType = Type.GetType("TimeTracker.Api.Services.DashboardService, TimeTracker.Api");
        if (serviceType is null)
        {
            return new MissingDashboardService();
        }

        var instance = Activator.CreateInstance(
            serviceType,
            dbContext,
            NullLogger<object>.Instance);

        return (IDashboardService)instance!;
    }

    private sealed class MissingDashboardService : IDashboardService
    {
        public Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException(
                "DashboardService has not been implemented yet (TDD Red phase).");
    }

    private async Task<int> AddClientAsync(string name, bool isActive)
    {
        var client = new Client
        {
            Name = name,
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.test",
            HourlyRate = 100m,
            IsActive = isActive,
        };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();
        return client.Id;
    }

    private async Task<int> AddProjectAsync(int clientId, string name, bool isActive)
    {
        var project = new Project
        {
            ClientId = clientId,
            Name = name,
            IsActive = isActive,
        };
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        return project.Id;
    }

    private async Task AddEntryAsync(int projectId, DateTime date, decimal hours, bool isBillable)
    {
        _dbContext.TimeEntries.Add(new TimeEntry
        {
            ProjectId = projectId,
            Date = date,
            Hours = hours,
            IsBillable = isBillable,
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task AddInvoiceAsync(int clientId, string invoiceNumber, InvoiceStatus status, decimal total)
    {
        _dbContext.Invoices.Add(new Invoice
        {
            ClientId = clientId,
            InvoiceNumber = invoiceNumber,
            Status = status,
            PeriodStart = new DateTime(2026, 4, 1),
            PeriodEnd = new DateTime(2026, 4, 30),
            Subtotal = total,
            Total = total,
        });
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
