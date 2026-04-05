using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;

namespace TimeTracker.Tests;

public class ClientServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new ClientService(_db);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_Succeeds()
    {
        var request = new CreateClientRequest("Acme Corp", "acme@example.com", 150m);

        var client = await _service.CreateAsync(request);

        Assert.Equal("Acme Corp", client.Name);
        Assert.Equal("acme@example.com", client.Email);
        Assert.Equal(150m, client.HourlyRate);
        Assert.True(client.IsActive);
        Assert.True(client.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_Throws()
    {
        var request = new CreateClientRequest("", null, 100m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithWhitespaceName_Throws()
    {
        var request = new CreateClientRequest("   ", null, 100m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithZeroHourlyRate_Throws()
    {
        var request = new CreateClientRequest("Acme", null, 0m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeHourlyRate_Throws()
    {
        var request = new CreateClientRequest("Acme", null, -10m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEmail_Throws()
    {
        var request = new CreateClientRequest("Acme", "not-an-email", 100m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse()
    {
        var client = await _service.CreateAsync(new CreateClientRequest("Test Co", null, 100m));

        await _service.DeactivateAsync(client.Id);

        var result = await _db.Clients.FindAsync(client.Id);
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveClients()
    {
        await _service.CreateAsync(new CreateClientRequest("Active Client", null, 100m));
        var inactive = await _service.CreateAsync(new CreateClientRequest("Inactive Client", null, 100m));
        await _service.DeactivateAsync(inactive.Id);

        var results = await _service.GetAllAsync();

        Assert.Single(results);
        Assert.Equal("Active Client", results[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesFields()
    {
        var client = await _service.CreateAsync(new CreateClientRequest("Old Name", null, 100m));

        var updated = await _service.UpdateAsync(client.Id, new UpdateClientRequest("New Name", "new@example.com", 200m));

        Assert.Equal("New Name", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal(200m, updated.HourlyRate);
    }

    [Fact]
    public async Task UpdateAsync_ForInactiveClient_Throws()
    {
        var client = await _service.CreateAsync(new CreateClientRequest("To Deactivate", null, 100m));
        await _service.DeactivateAsync(client.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(client.Id, new UpdateClientRequest("New Name", null, 100m)));
    }

    [Fact]
    public async Task DeactivateAsync_ForNonExistentClient_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeactivateAsync(99999));
    }

    public void Dispose() => _db.Dispose();
}
