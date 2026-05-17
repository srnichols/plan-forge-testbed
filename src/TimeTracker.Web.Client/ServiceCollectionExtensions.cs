using Microsoft.Extensions.DependencyInjection;

namespace TimeTracker.Web.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeTrackerClient(
        this IServiceCollection services, string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        var uri = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");

        services.AddHttpClient<IClientsApi, ClientsApi>(c => c.BaseAddress = uri);
        services.AddHttpClient<IProjectsApi, ProjectsApi>(c => c.BaseAddress = uri);
        services.AddHttpClient<ITimeEntriesApi, TimeEntriesApi>(c => c.BaseAddress = uri);
        services.AddHttpClient<IInvoicesApi, InvoicesApi>(c => c.BaseAddress = uri);
        services.AddHttpClient<IDashboardApi, DashboardApi>(c => c.BaseAddress = uri);

        return services;
    }
}
