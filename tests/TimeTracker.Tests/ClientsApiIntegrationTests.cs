using System.Net;
using System.Net.Http.Json;
using TimeTracker.Core.Models;
using TimeTracker.Tests.Infrastructure;

namespace TimeTracker.Tests;

[Trait("Category", "Integration")]
public class ClientsApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientsApiIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullCrudLifecycle_Succeeds()
    {
        // 1. Create
        var createPayload = new { name = "Acme Corp", email = "acme@example.com", hourlyRate = 150m };
        var createResponse = await _client.PostAsJsonAsync("/api/clients", createPayload, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Client>(CancellationToken.None);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Acme Corp", created.Name);
        Assert.Equal("acme@example.com", created.Email);
        Assert.Equal(150m, created.HourlyRate);
        Assert.Contains($"/api/clients/{created.Id}", createResponse.Headers.Location?.PathAndQuery, StringComparison.OrdinalIgnoreCase);

        // 2. Read
        var getResponse = await _client.GetAsync($"/api/clients/{created.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Client>(CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("Acme Corp", fetched.Name);
        Assert.Equal("acme@example.com", fetched.Email);
        Assert.Equal(150m, fetched.HourlyRate);

        // 3. List
        var listResponse = await _client.GetAsync("/api/clients", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var allClients = await listResponse.Content.ReadFromJsonAsync<List<Client>>(CancellationToken.None);
        Assert.NotNull(allClients);
        Assert.Contains(allClients, c => c.Id == created.Id);

        // 4. Update
        var updatePayload = new { name = "Acme Updated", email = "new@example.com", hourlyRate = 200m };
        var updateResponse = await _client.PutAsJsonAsync($"/api/clients/{created.Id}", updatePayload, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Client>(CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Acme Updated", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal(200m, updated.HourlyRate);

        // 5. Read-after-update
        var getAfterUpdate = await _client.GetAsync($"/api/clients/{created.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, getAfterUpdate.StatusCode);
        var refetched = await getAfterUpdate.Content.ReadFromJsonAsync<Client>(CancellationToken.None);
        Assert.NotNull(refetched);
        Assert.Equal("Acme Updated", refetched.Name);
        Assert.Equal("new@example.com", refetched.Email);
        Assert.Equal(200m, refetched.HourlyRate);

        // 6. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/clients/{created.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. Soft-delete verify — GET returns 404
        var getAfterDelete = await _client.GetAsync($"/api/clients/{created.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);

        // 8. List exclusion — deleted client absent
        var listAfterDelete = await _client.GetAsync("/api/clients", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, listAfterDelete.StatusCode);
        var remaining = await listAfterDelete.Content.ReadFromJsonAsync<List<Client>>(CancellationToken.None);
        Assert.NotNull(remaining);
        Assert.DoesNotContain(remaining, c => c.Id == created.Id);
    }
}
