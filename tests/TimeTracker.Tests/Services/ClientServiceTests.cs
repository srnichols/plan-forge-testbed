using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;

namespace TimeTracker.Tests.Services;

[Trait("Category", "Unit")]
public sealed class ClientServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _dbContext;
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ClientServiceTests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new TimeTrackerDbContext(options);
        _sut = new ClientService(_dbContext, NullLogger<ClientService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_PersistsClientAndReturnsIt()
    {
        var client = await _sut.CreateAsync("Acme Corp", "billing@acme.test", 125.00m);

        Assert.True(client.Id > 0);
        Assert.Equal("Acme Corp", client.Name);
        Assert.Equal("billing@acme.test", client.Email);
        Assert.Equal(125.00m, client.HourlyRate);
        Assert.True(client.IsActive);

        var persisted = await _dbContext.Clients.FindAsync(client.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Acme Corp", persisted!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_WithEmptyName_ThrowsValidationException(string? name)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(name!, "ok@example.com", 50.00m));

        Assert.Equal(0, await _dbContext.Clients.CountAsync());
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveToFalse()
    {
        var client = await _sut.CreateAsync("Globex", "ap@globex.test", 200m);

        var result = await _sut.DeactivateAsync(client.Id);

        Assert.True(result);
        var reloaded = await _dbContext.Clients.AsNoTracking()
            .FirstAsync(c => c.Id == client.Id);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveClients_OrderedByName()
    {
        await _sut.CreateAsync("Beta Inc", "b@beta.test", 100m);
        await _sut.CreateAsync("Alpha LLC", "a@alpha.test", 150m);
        var toDeactivate = await _sut.CreateAsync("Zeta Co", "z@zeta.test", 90m);
        await _sut.DeactivateAsync(toDeactivate.Id);

        var results = await _sut.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha LLC", results[0].Name);
        Assert.Equal("Beta Inc", results[1].Name);
        Assert.DoesNotContain(results, c => c.Id == toDeactivate.Id);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
