using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TimeTracker.Api.Data;

namespace TimeTracker.Tests;

public class ClientsIntegrationTests
{
    private HttpClient CreateTestClient()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<TimeTrackerDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);
                services.AddDbContext<TimeTrackerDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTest_" + Guid.NewGuid()));
            });
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateDeleteVerify()
    {
        var client = CreateTestClient();

        // CREATE
