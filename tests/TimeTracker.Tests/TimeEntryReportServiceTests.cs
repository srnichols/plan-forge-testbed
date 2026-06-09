using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

[Trait("Category", "Unit")]
public sealed class TimeEntryReportServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _dbContext;
    private readonly TimeEntryReportService _sut;

    private static readonly DateOnly PeriodStart = new(2026, 4, 6);
    private static readonly DateOnly PeriodEnd = new(2026, 4, 12);

    public TimeEntryReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TimeEntryReportServiceTests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new TimeTrackerDbContext(options);
        _sut = new TimeEntryReportService(_dbContext, NullLogger<TimeEntryReportService>.Instance);
    }

    // ---------- GetHoursSummaryAsync ----------

    [Fact]
    public async Task GetHoursSummaryAsync_AggregatesBillableAndNonBillableHours()
    {
        var (clientId, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 8m, isBillable: true);
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 8), 4m, isBillable: true);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 9), 3m, isBillable: false);

        var result = await _sut.GetHoursSummaryAsync(PeriodStart, PeriodEnd);

        Assert.Equal(15m, result.TotalHours);
        Assert.Equal(12m, result.BillableHours);
        Assert.Equal(3m, result.NonBillableHours);
        Assert.Equal(3, result.EntryCount);
        Assert.Equal(PeriodStart, result.PeriodStart);
        Assert.Equal(PeriodEnd, result.PeriodEnd);
        Assert.Equal(clientId, clientId); // sanity to retain seeded reference
    }

    [Fact]
    public async Task GetHoursSummaryAsync_WithNoEntriesInRange_ReturnsZeros()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 1, 1), 5m, isBillable: true);

        var result = await _sut.GetHoursSummaryAsync(PeriodStart, PeriodEnd);

        Assert.Equal(0m, result.TotalHours);
        Assert.Equal(0m, result.BillableHours);
        Assert.Equal(0m, result.NonBillableHours);
        Assert.Equal(0, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummaryAsync_WithProjectFilter_OnlyIncludesMatchingProject()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 6m, isBillable: true);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 8), 4m, isBillable: true);

        var result = await _sut.GetHoursSummaryAsync(PeriodStart, PeriodEnd, projectId: projectIds.alpha);

        Assert.Equal(6m, result.TotalHours);
        Assert.Equal(1, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummaryAsync_WithClientFilter_OnlyIncludesMatchingClient()
    {
        var (clientId, projectIds) = await SeedAsync();
        var otherClient = await AddClientAsync("Other Client");
        var otherProject = await AddProjectAsync(otherClient, "Gamma");
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 2m, isBillable: true);
        await AddEntryAsync(otherProject, new DateTime(2026, 4, 8), 9m, isBillable: true);

        var result = await _sut.GetHoursSummaryAsync(PeriodStart, PeriodEnd, clientId: clientId);

        Assert.Equal(2m, result.TotalHours);
        Assert.Equal(1, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummaryAsync_WhenStartAfterEnd_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.GetHoursSummaryAsync(PeriodEnd, PeriodStart));
    }

    [Fact]
    public async Task GetHoursSummaryAsync_ExcludesEntriesOutsideRange()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 5), 5m, isBillable: true);  // before
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 6), 1m, isBillable: true);  // inclusive start
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 12), 2m, isBillable: true); // inclusive end
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 13), 9m, isBillable: true); // after

        var result = await _sut.GetHoursSummaryAsync(PeriodStart, PeriodEnd);

        Assert.Equal(3m, result.TotalHours);
        Assert.Equal(2, result.EntryCount);
    }

    // ---------- GetProjectBreakdownAsync ----------

    [Fact]
    public async Task GetProjectBreakdownAsync_ComputesPercentagesAndOrdersByHoursDescending()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 6m, isBillable: true);
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 8), 2m, isBillable: false);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 9), 2m, isBillable: true);

        var result = await _sut.GetProjectBreakdownAsync(PeriodStart, PeriodEnd);

        Assert.Equal(10m, result.TotalHours);
        Assert.Equal(2, result.Projects.Count);

        var alpha = result.Projects[0];
        Assert.Equal(projectIds.alpha, alpha.ProjectId);
        Assert.Equal("Alpha", alpha.ProjectName);
        Assert.Equal(8m, alpha.TotalHours);
        Assert.Equal(6m, alpha.BillableHours);
        Assert.Equal(2m, alpha.NonBillableHours);
        Assert.Equal(80m, alpha.PercentageOfTotal);

        var beta = result.Projects[1];
        Assert.Equal(projectIds.beta, beta.ProjectId);
        Assert.Equal(2m, beta.TotalHours);
        Assert.Equal(20m, beta.PercentageOfTotal);
    }

    [Fact]
    public async Task GetProjectBreakdownAsync_WithNoEntries_ReturnsEmptyProjectsAndZeroTotal()
    {
        await SeedAsync();

        var result = await _sut.GetProjectBreakdownAsync(PeriodStart, PeriodEnd);

        Assert.Equal(0m, result.TotalHours);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public async Task GetProjectBreakdownAsync_TiedHours_OrdersByProjectNameAscending()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 4m, isBillable: true);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 8), 4m, isBillable: true);

        var result = await _sut.GetProjectBreakdownAsync(PeriodStart, PeriodEnd);

        Assert.Equal(2, result.Projects.Count);
        Assert.Equal("Alpha", result.Projects[0].ProjectName);
        Assert.Equal("Beta", result.Projects[1].ProjectName);
    }

    [Fact]
    public async Task GetProjectBreakdownAsync_WithClientFilter_ExcludesOtherClientsProjects()
    {
        var (clientId, projectIds) = await SeedAsync();
        var otherClient = await AddClientAsync("Other Client");
        var otherProject = await AddProjectAsync(otherClient, "Gamma");
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 3m, isBillable: true);
        await AddEntryAsync(otherProject, new DateTime(2026, 4, 8), 7m, isBillable: true);

        var result = await _sut.GetProjectBreakdownAsync(PeriodStart, PeriodEnd, clientId: clientId);

        Assert.Single(result.Projects);
        Assert.Equal(projectIds.alpha, result.Projects[0].ProjectId);
        Assert.Equal(3m, result.TotalHours);
        Assert.Equal(100m, result.Projects[0].PercentageOfTotal);
    }

    [Fact]
    public async Task GetProjectBreakdownAsync_WhenStartAfterEnd_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.GetProjectBreakdownAsync(PeriodEnd, PeriodStart));
    }

    // ---------- GetDailyTimelineAsync ----------

    [Fact]
    public async Task GetDailyTimelineAsync_GroupsByDateAndOrdersAscending()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 9), 4m, isBillable: true);
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 3m, isBillable: true);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 7), 2m, isBillable: false);

        var result = await _sut.GetDailyTimelineAsync(PeriodStart, PeriodEnd);

        Assert.Equal(9m, result.TotalHours);
        Assert.Equal(2, result.Days.Count);

        var day1 = result.Days[0];
        Assert.Equal(new DateOnly(2026, 4, 7), day1.Date);
        Assert.Equal(5m, day1.TotalHours);
        Assert.Equal(3m, day1.BillableHours);
        Assert.Equal(2m, day1.NonBillableHours);
        Assert.Equal(2, day1.EntryCount);

        var day2 = result.Days[1];
        Assert.Equal(new DateOnly(2026, 4, 9), day2.Date);
        Assert.Equal(4m, day2.TotalHours);
        Assert.Equal(1, day2.EntryCount);
    }

    [Fact]
    public async Task GetDailyTimelineAsync_WithNoEntries_ReturnsEmptyDaysAndZeroTotal()
    {
        await SeedAsync();

        var result = await _sut.GetDailyTimelineAsync(PeriodStart, PeriodEnd);

        Assert.Equal(0m, result.TotalHours);
        Assert.Empty(result.Days);
        Assert.Equal(PeriodStart, result.PeriodStart);
        Assert.Equal(PeriodEnd, result.PeriodEnd);
    }

    [Fact]
    public async Task GetDailyTimelineAsync_WithProjectFilter_OnlyIncludesMatchingProject()
    {
        var (_, projectIds) = await SeedAsync();
        await AddEntryAsync(projectIds.alpha, new DateTime(2026, 4, 7), 3m, isBillable: true);
        await AddEntryAsync(projectIds.beta, new DateTime(2026, 4, 7), 5m, isBillable: true);

        var result = await _sut.GetDailyTimelineAsync(PeriodStart, PeriodEnd, projectId: projectIds.beta);

        Assert.Single(result.Days);
        Assert.Equal(5m, result.TotalHours);
        Assert.Equal(5m, result.Days[0].TotalHours);
    }

    [Fact]
    public async Task GetDailyTimelineAsync_WhenStartAfterEnd_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.GetDailyTimelineAsync(PeriodEnd, PeriodStart));
    }

    // ---------- Seed helpers ----------

    private async Task<(int clientId, (int alpha, int beta) projectIds)> SeedAsync()
    {
        var clientId = await AddClientAsync("Acme Corp");
        var alpha = await AddProjectAsync(clientId, "Alpha");
        var beta = await AddProjectAsync(clientId, "Beta");
        return (clientId, (alpha, beta));
    }

    private async Task<int> AddClientAsync(string name)
    {
        var client = new Client
        {
            Name = name,
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.test",
            HourlyRate = 100m,
            IsActive = true,
        };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();
        return client.Id;
    }

    private async Task<int> AddProjectAsync(int clientId, string name)
    {
        var project = new Project
        {
            ClientId = clientId,
            Name = name,
            IsActive = true,
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
