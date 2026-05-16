using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using TimeTracker.Api.Data;

namespace TimeTracker.Tests.Infrastructure;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TimeTrackerTest-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Remove all EF Core / DbContext registrations to avoid dual-provider conflict
            var descriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         || d.ServiceType == typeof(DbContextOptions<TimeTrackerDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(TimeTrackerDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TimeTrackerDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));
        });
    }
}
