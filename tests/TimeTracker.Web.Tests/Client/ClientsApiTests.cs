using System.Net;
using System.Text;
using TimeTracker.Web.Client;

namespace TimeTracker.Web.Tests.Client;

public class ClientsApiTests
{
    [Fact]
    public async Task GetAllAsync_DeserializesJsonResponseCorrectly()
    {
        const string json = """
            [
              {
                "id": 1,
                "name": "Acme Corp",
                "email": "acme@test.com",
                "hourlyRate": 150.00,
                "isActive": true,
                "createdAt": "2024-01-15T00:00:00"
              }
            ]
            """;

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        var api = new ClientsApi(httpClient);

        var result = await api.GetAllAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Acme Corp", result[0].Name);
        Assert.Equal("acme@test.com", result[0].Email);
        Assert.Equal(150.00m, result[0].HourlyRate);
        Assert.True(result[0].IsActive);

        // Verify the correct relative URL was requested
        Assert.Equal("api/clients", handler.LastRequestPath);
    }
}

internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
{
    public string? LastRequestPath { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestPath = request.RequestUri?.AbsolutePath.TrimStart('/');

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });
    }
}
