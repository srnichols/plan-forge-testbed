using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class TimeEntryServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly TimeEntryService _service;

    public TimeEntryServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new TimeEntryService(_db);
    }

    private async Task<Project> SeedProjectAsync()
    {
        var client = new Client { Name = "Acme", HourlyRate = 100m, IsActive = true };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var project = new Project { Name = "Website", ClientId = client.Id, IsActive = true };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    [Fact]
    public async Task CreateAsync_WithValidData_Succeeds()
    {
        var project = await SeedProjectAsync();

        var result = await _service.CreateAsync(project.Id, new DateTime(2026, 6, 6), 4.5m, "Design work", true);

        Assert.True(result.Id > 0);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(4.5m, result.Hours);
        Assert.True(result.IsBillable);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public async Task CreateAsync_WithOutOfRangeHours_ThrowsValidation(decimal hours)
    {
        var project = await SeedProjectAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(project.Id, DateTime.Today, hours, null, true));
    }

    [Fact]
    public async Task CreateAsync_WithOversizedDescription_ThrowsValidation()
    {
        var project = await SeedProjectAsync();
        var description = new string('x', 1001);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(project.Id, DateTime.Today, 1m, description, true));
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentProject_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(99999, DateTime.Today, 1m, null, true));
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDateAndProject()
    {
        var project = await SeedProjectAsync();
        await _service.CreateAsync(project.Id, new DateTime(2026, 6, 1), 2m, "Day 1", true);
        await _service.CreateAsync(project.Id, new DateTime(2026, 6, 2), 3m, "Day 2", true);

        var byDate = await _service.GetAllAsync(date: new DateTime(2026, 6, 1));
        Assert.Single(byDate);
        Assert.Equal(2m, byDate[0].Hours);

        var byProject = await _service.GetAllAsync(projectId: project.Id);
        Assert.Equal(2, byProject.Count);

        var byMissingProject = await _service.GetAllAsync(projectId: 99999);
        Assert.Empty(byMissingProject);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        var project = await SeedProjectAsync();
        await _service.CreateAsync(project.Id, new DateTime(2026, 6, 1), 2m, "Older", true);
        await _service.CreateAsync(project.Id, new DateTime(2026, 6, 5), 3m, "Newer", true);

        var all = await _service.GetAllAsync();

        Assert.Equal(new DateTime(2026, 6, 5), all[0].Date.Date);
        Assert.Equal(new DateTime(2026, 6, 1), all[1].Date.Date);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingEntry()
    {
        var project = await SeedProjectAsync();
        var entry = await _service.CreateAsync(project.Id, DateTime.Today, 1m, null, true);

        var deleted = await _service.DeleteAsync(entry.Id);

        Assert.True(deleted);
        Assert.Null(await _db.TimeEntries.FindAsync(entry.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithMissingId_ReturnsFalse()
    {
        var deleted = await _service.DeleteAsync(99999);

        Assert.False(deleted);
    }

    public void Dispose() => _db.Dispose();
}
