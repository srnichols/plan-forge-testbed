using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class ClientsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ClientsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "IntegrationTests_" + Guid.NewGuid();
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL DbContext-related registrations to avoid dual-provider conflict
                var toRemove = services
                    .Where(d => d.ServiceType.FullName != null &&
                                (d.ServiceType.FullName.Contains("TimeTrackerDbContext")
                              || d.ServiceType.FullName.Contains("DbContextOptions")))
                    .ToList();
                foreach (var d in toRemove)
                {
                    services.Remove(d);
                }

                services.AddDbContext<TimeTrackerDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task FullCrudLifecycle_WorksCorrectly()
    {
        // Create
        var newClient = new { Name = "Integration Test Client", Email = "test@example.com", HourlyRate = 125.50m };
        var createResponse = await _client.PostAsJsonAsync("/api/clients", newClient);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(created);
        Assert.Equal("Integration Test Client", created.Name);
        var clientId = created.Id;

        // Read
        var getResponse = await _client.GetAsync($"/api/clients/{clientId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(fetched);
        Assert.Equal("Integration Test Client", fetched.Name);

        // Update
        var updatePayload = new { Name = "Updated Client", Email = "updated@example.com", HourlyRate = 200m };
        var updateResponse = await _client.PutAsJsonAsync($"/api/clients/{clientId}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Client", updated.Name);
        Assert.Equal(200m, updated.HourlyRate);

        // Delete (soft-delete)
        var deleteResponse = await _client.DeleteAsync($"/api/clients/{clientId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted (not found since it's deactivated)
        var getDeletedResponse = await _client.GetAsync($"/api/clients/{clientId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        var invalidClient = new { Name = "", HourlyRate = 0m };
        var response = await _client.PostAsJsonAsync("/api/clients", invalidClient);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/clients/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
