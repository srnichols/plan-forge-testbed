using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class ProjectServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new ProjectService(_db);
    }

    private async Task<Client> SeedClientAsync(bool active = true)
    {
        var client = new Client { Name = "Acme", HourlyRate = 100m, IsActive = active };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    [Fact]
    public async Task CreateAsync_WithValidData_Succeeds()
    {
        var client = await SeedClientAsync();

        var result = await _service.CreateAsync(new CreateProjectRequest("Website", "A web project", client.Id));

        Assert.True(result.Id > 0);
        Assert.Equal("Website", result.Name);
        Assert.Equal(client.Id, result.ClientId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsValidation()
    {
        var client = await SeedClientAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(new CreateProjectRequest("", "desc", client.Id)));
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentClient_Throws()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(new CreateProjectRequest("Website", "desc", 99999)));
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse()
    {
        var client = await SeedClientAsync();
        var project = await _service.CreateAsync(new CreateProjectRequest("Website", null, client.Id));

        await _service.DeactivateAsync(project.Id);

        var result = await _db.Projects.FindAsync(project.Id);
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByClientId_WhenProvided()
    {
        var clientA = await SeedClientAsync();
        var clientB = new Client { Name = "Beta Corp", HourlyRate = 120m, IsActive = true };
        _db.Clients.Add(clientB);
        await _db.SaveChangesAsync();

        await _service.CreateAsync(new CreateProjectRequest("App-A", null, clientA.Id));
        await _service.CreateAsync(new CreateProjectRequest("App-B", null, clientB.Id));

        var filtered = await _service.GetAllAsync(clientA.Id);

        Assert.Single(filtered);
        Assert.Equal(clientA.Id, filtered[0].ClientId);

        var all = await _service.GetAllAsync(null);
        Assert.Equal(2, all.Count);
    }

    public void Dispose() => _db.Dispose();
}
