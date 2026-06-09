using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ClientsApiIntegrationTests : IClassFixture<ClientsApiIntegrationTests.ClientsApiFactory>
{
    private readonly ClientsApiFactory _factory;

    public ClientsApiIntegrationTests(ClientsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullCrudLifecycle_CreateReadUpdateDelete_BehavesAsExpected()
    {
        var client = _factory.CreateClient();

        // CREATE
        var createResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            Name = "Initech",
            Email = "ap@initech.test",
            HourlyRate = 150.00m,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Initech", created.Name);
        Assert.True(created.IsActive);

        var id = created.Id;

        // READ (single)
        var getResponse = await client.GetAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(fetched);
        Assert.Equal("Initech", fetched!.Name);
        Assert.Equal(150.00m, fetched.HourlyRate);

        // READ (list) — contains the created active client
        var listResponse = await client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<Client>>();
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == id);

        // UPDATE
        var updateResponse = await client.PutAsJsonAsync($"/api/clients/{id}", new
        {
            Name = "Initech Renamed",
            Email = "billing@initech.test",
            HourlyRate = 175.50m,
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(updated);
        Assert.Equal("Initech Renamed", updated!.Name);
        Assert.Equal("billing@initech.test", updated.Email);
        Assert.Equal(175.50m, updated.HourlyRate);

        // Verify update persisted via GET
        var getAfterUpdate = await client.GetAsync($"/api/clients/{id}");
        var afterUpdate = await getAfterUpdate.Content.ReadFromJsonAsync<Client>();
        Assert.Equal("Initech Renamed", afterUpdate!.Name);

        // DELETE (deactivate)
        var deleteResponse = await client.DeleteAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deactivation — record still exists via GetById but absent from active list
        var getAfterDelete = await client.GetAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.OK, getAfterDelete.StatusCode);
        var deactivated = await getAfterDelete.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);

        var listAfterDelete = await client.GetAsync("/api/clients");
        var activeList = await listAfterDelete.Content.ReadFromJsonAsync<List<Client>>();
        Assert.DoesNotContain(activeList!, c => c.Id == id);

        // DELETE again on an already-inactive client still returns NoContent (idempotent at service layer)
        var deleteAgain = await client.DeleteAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownClient_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/clients/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownClient_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/clients/999999", new
        {
            Name = "Ghost",
            Email = "ghost@example.test",
            HourlyRate = 50m,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownClient_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/clients/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public sealed class ClientsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ClientsApiIntegration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d =>
                    {
                        var name = d.ServiceType.FullName ?? string.Empty;
                        return d.ServiceType == typeof(DbContextOptions<TimeTrackerDbContext>)
                            || d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(TimeTrackerDbContext)
                            || name.Contains("Npgsql", StringComparison.Ordinal)
                            || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false)
                            || name.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal);
                    })
                    .ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TimeTrackerDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}
