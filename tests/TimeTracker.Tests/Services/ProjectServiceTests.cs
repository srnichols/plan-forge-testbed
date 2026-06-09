using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests.Services;

[Trait("Category", "Unit")]
public sealed class ProjectServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _dbContext;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectServiceTests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new TimeTrackerDbContext(options);
        _sut = new ProjectService(_dbContext, NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_PersistsProjectAndReturnsIt()
    {
        var client = await SeedClientAsync("Acme Corp");

        var project = await _sut.CreateAsync("Website Redesign", "Marketing site refresh", client.Id);

        Assert.True(project.Id > 0);
        Assert.Equal("Website Redesign", project.Name);
        Assert.Equal("Marketing site refresh", project.Description);
        Assert.Equal(client.Id, project.ClientId);
        Assert.True(project.IsActive);

        var persisted = await _dbContext.Projects.FindAsync(project.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Website Redesign", persisted!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_WithEmptyName_ThrowsValidationException(string? name)
    {
        var client = await SeedClientAsync("Globex");

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(name!, "desc", client.Id));

        Assert.Equal(0, await _dbContext.Projects.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentClient_ThrowsValidationException()
    {
        const int missingClientId = 9999;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync("Orphan Project", null, missingClientId));

        Assert.Contains(missingClientId.ToString(), ex.Message);
        Assert.Equal(0, await _dbContext.Projects.CountAsync());
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveToFalse()
    {
        var client = await SeedClientAsync("Initech");
        var project = await _sut.CreateAsync("Mobile App", null, client.Id);

        var result = await _sut.DeactivateAsync(project.Id);

        Assert.True(result);
        var reloaded = await _dbContext.Projects.AsNoTracking()
            .FirstAsync(p => p.Id == project.Id);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveProjects_AcrossClients_OrderedByName()
    {
        // Seed projects across two clients to verify the query returns all
        // active projects (the current GetAllAsync signature has no per-client
        // filter — Slice 2 documents this behaviour; a future slice can add
        // a clientId overload).
        var clientA = await SeedClientAsync("Client A");
        var clientB = await SeedClientAsync("Client B");

        await _sut.CreateAsync("Beta Site", null, clientA.Id);
        await _sut.CreateAsync("Alpha API", null, clientB.Id);
        var toDeactivate = await _sut.CreateAsync("Zeta Worker", null, clientA.Id);
        await _sut.DeactivateAsync(toDeactivate.Id);

        var results = await _sut.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha API", results[0].Name);
        Assert.Equal("Beta Site", results[1].Name);
        Assert.DoesNotContain(results, p => p.Id == toDeactivate.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenFilteringByClient_CallerCanFilterReturnedProjects()
    {
        // Until IProjectService exposes a clientId overload, callers filter
        // the returned collection. This test pins that contract so a future
        // slice that adds a server-side filter does not silently regress it.
        var clientA = await SeedClientAsync("Client A");
        var clientB = await SeedClientAsync("Client B");

        await _sut.CreateAsync("A-One", null, clientA.Id);
        await _sut.CreateAsync("A-Two", null, clientA.Id);
        await _sut.CreateAsync("B-One", null, clientB.Id);

        var all = await _sut.GetAllAsync();
        var forClientA = all.Where(p => p.ClientId == clientA.Id).ToList();

        Assert.Equal(2, forClientA.Count);
        Assert.All(forClientA, p => Assert.Equal(clientA.Id, p.ClientId));
    }

    private async Task<Client> SeedClientAsync(string name)
    {
        var client = new Client
        {
            Name = name,
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.test",
            HourlyRate = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();
        return client;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
