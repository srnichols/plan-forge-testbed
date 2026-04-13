using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class TimeEntryReportServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly TimeEntryReportService _service;

    public TimeEntryReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new TimeEntryReportService(_db);
    }

    private async Task<Project> SeedProjectAsync(string name = "Test Project")
    {
        var client = new Client { Name = "Test Client", Email = "test@example.com", HourlyRate = 100m };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var project = new Project { Name = name, ClientId = client.Id };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    private async Task SeedEntryAsync(int projectId, DateTime date, decimal hours, bool isBillable = true)
    {
        _db.TimeEntries.Add(new TimeEntry
        {
            ProjectId = projectId,
            Date = date,
            Hours = hours,
            IsBillable = isBillable,
            Description = "Test entry"
        });
        await _db.SaveChangesAsync();
    }

    // ── Hours Summary ──

    [Fact]
    public async Task GetHoursSummary_EmptyRange_ReturnsZeros()
    {
        var result = await _service.GetHoursSummaryAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(0m, result.TotalHours);
        Assert.Equal(0m, result.BillableHours);
        Assert.Equal(0m, result.NonBillableHours);
        Assert.Equal(0, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummary_WithEntries_CalculatesCorrectly()
    {
        var project = await SeedProjectAsync();
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 7), 8m, isBillable: true);
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 8), 6m, isBillable: true);
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 9), 2m, isBillable: false);

        var result = await _service.GetHoursSummaryAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(16m, result.TotalHours);
        Assert.Equal(14m, result.BillableHours);
        Assert.Equal(2m, result.NonBillableHours);
        Assert.Equal(3, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummary_WithProjectFilter_FiltersCorrectly()
    {
        var project1 = await SeedProjectAsync("Project A");
        var project2 = await SeedProjectAsync("Project B");
        await SeedEntryAsync(project1.Id, new DateTime(2026, 4, 7), 8m);
        await SeedEntryAsync(project2.Id, new DateTime(2026, 4, 7), 4m);

        var result = await _service.GetHoursSummaryAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), projectId: project1.Id);

        Assert.Equal(8m, result.TotalHours);
        Assert.Equal(1, result.EntryCount);
    }

    [Fact]
    public async Task GetHoursSummary_ExcludesEntriesOutsideDateRange()
    {
        var project = await SeedProjectAsync();
        await SeedEntryAsync(project.Id, new DateTime(2026, 3, 31), 5m); // before range
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 1), 8m);  // in range
        await SeedEntryAsync(project.Id, new DateTime(2026, 5, 1), 3m);  // after range

        var result = await _service.GetHoursSummaryAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(8m, result.TotalHours);
        Assert.Equal(1, result.EntryCount);
    }

    // ── Project Breakdown ──

    [Fact]
    public async Task GetProjectBreakdown_EmptyRange_ReturnsEmptyList()
    {
        var result = await _service.GetProjectBreakdownAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(0m, result.TotalHours);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public async Task GetProjectBreakdown_MultipleProjects_CalculatesPercentages()
    {
        var project1 = await SeedProjectAsync("Project Alpha");
        var project2 = await SeedProjectAsync("Project Beta");
        await SeedEntryAsync(project1.Id, new DateTime(2026, 4, 7), 6m);
        await SeedEntryAsync(project1.Id, new DateTime(2026, 4, 8), 6m);
        await SeedEntryAsync(project2.Id, new DateTime(2026, 4, 7), 4m);

        var result = await _service.GetProjectBreakdownAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(16m, result.TotalHours);
        Assert.Equal(2, result.Projects.Count);

        var alpha = result.Projects.First(p => p.ProjectName == "Project Alpha");
        Assert.Equal(12m, alpha.TotalHours);
        Assert.Equal(75m, alpha.PercentageOfTotal);

        var beta = result.Projects.First(p => p.ProjectName == "Project Beta");
        Assert.Equal(4m, beta.TotalHours);
        Assert.Equal(25m, beta.PercentageOfTotal);
    }

    [Fact]
    public async Task GetProjectBreakdown_OrderedByHoursDescending()
    {
        var project1 = await SeedProjectAsync("Small Project");
        var project2 = await SeedProjectAsync("Big Project");
        await SeedEntryAsync(project1.Id, new DateTime(2026, 4, 7), 2m);
        await SeedEntryAsync(project2.Id, new DateTime(2026, 4, 7), 10m);

        var result = await _service.GetProjectBreakdownAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal("Big Project", result.Projects[0].ProjectName);
        Assert.Equal("Small Project", result.Projects[1].ProjectName);
    }

    // ── Daily Timeline ──

    [Fact]
    public async Task GetDailyTimeline_EmptyRange_ReturnsEmptyDays()
    {
        var result = await _service.GetDailyTimelineAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(0m, result.TotalHours);
        Assert.Empty(result.Days);
    }

    [Fact]
    public async Task GetDailyTimeline_AggregatesByDay()
    {
        var project = await SeedProjectAsync();
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 7), 4m);
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 7), 3m); // same day
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 8), 6m);

        var result = await _service.GetDailyTimelineAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(13m, result.TotalHours);
        Assert.Equal(2, result.Days.Count);

        var day1 = result.Days.First(d => d.Date == new DateOnly(2026, 4, 7));
        Assert.Equal(7m, day1.TotalHours);
        Assert.Equal(2, day1.EntryCount);

        var day2 = result.Days.First(d => d.Date == new DateOnly(2026, 4, 8));
        Assert.Equal(6m, day2.TotalHours);
    }

    [Fact]
    public async Task GetDailyTimeline_SplitsBillableAndNonBillable()
    {
        var project = await SeedProjectAsync();
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 7), 6m, isBillable: true);
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 7), 2m, isBillable: false);

        var result = await _service.GetDailyTimelineAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        var day = result.Days.Single();
        Assert.Equal(8m, day.TotalHours);
        Assert.Equal(6m, day.BillableHours);
        Assert.Equal(2m, day.NonBillableHours);
    }

    [Fact]
    public async Task GetDailyTimeline_OrderedByDateAscending()
    {
        var project = await SeedProjectAsync();
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 10), 3m);
        await SeedEntryAsync(project.Id, new DateTime(2026, 4, 5), 5m);

        var result = await _service.GetDailyTimelineAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(new DateOnly(2026, 4, 5), result.Days[0].Date);
        Assert.Equal(new DateOnly(2026, 4, 10), result.Days[1].Date);
    }

    [Fact]
    public async Task GetDailyTimeline_WithProjectFilter_FiltersCorrectly()
    {
        var project1 = await SeedProjectAsync("Project A");
        var project2 = await SeedProjectAsync("Project B");
        await SeedEntryAsync(project1.Id, new DateTime(2026, 4, 7), 8m);
        await SeedEntryAsync(project2.Id, new DateTime(2026, 4, 7), 4m);

        var result = await _service.GetDailyTimelineAsync(
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), projectId: project1.Id);

        Assert.Equal(8m, result.TotalHours);
        var day = result.Days.Single();
        Assert.Equal(8m, day.TotalHours);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
