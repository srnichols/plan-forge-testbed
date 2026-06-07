using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class ClientServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _dbContext;
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TimeTrackerDbContext(options);
        _sut = new ClientService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCreatedClient()
    {
        var client = new Client { Name = "Acme Corp", Email = "info@acme.com", HourlyRate = 150m };

        var result = await _sut.CreateAsync(client);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Acme Corp", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
    {
        var client = new Client { Name = "", HourlyRate = 100m };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(client));
    }

    [Fact]
    public async Task CreateAsync_WithZeroHourlyRate_ThrowsArgumentException()
    {
        var client = new Client { Name = "Test Client", HourlyRate = 0m };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(client));
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveToFalse()
    {
        var client = new Client { Name = "Test Client", HourlyRate = 100m };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();

        await _sut.DeactivateAsync(client.Id);

        var deactivated = await _dbContext.Clients.FindAsync(client.Id);
        Assert.NotNull(deactivated);
        Assert.False(deactivated.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveClients()
    {
        _dbContext.Clients.AddRange(
            new Client { Name = "Active Client", HourlyRate = 100m, IsActive = true },
            new Client { Name = "Inactive Client", HourlyRate = 100m, IsActive = false }
        );
        await _dbContext.SaveChangesAsync();

        var results = (await _sut.GetAllAsync()).ToList();

        Assert.Single(results);
        Assert.Equal("Active Client", results[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsClient()
    {
        var client = new Client { Name = "Test Client", HourlyRate = 100m };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(client.Id);

        Assert.NotNull(result);
        Assert.Equal("Test Client", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesClient()
    {
        var client = new Client { Name = "Original", Email = "old@test.com", HourlyRate = 100m };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();

        var updated = await _sut.UpdateAsync(client.Id, new Client
        {
            Name = "Updated",
            Email = "new@test.com",
            HourlyRate = 150m
        });

        Assert.Equal("Updated", updated.Name);
        Assert.Equal("new@test.com", updated.Email);
        Assert.Equal(150m, updated.HourlyRate);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        var client = new Client { Name = "Test", HourlyRate = 100m };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.UpdateAsync(999, client));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
