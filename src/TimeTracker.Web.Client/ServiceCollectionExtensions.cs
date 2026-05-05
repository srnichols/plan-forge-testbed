using Microsoft.Extensions.DependencyInjection;

namespace TimeTracker.Web.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeTrackerClient(this IServiceCollection services, string baseUrl)
    {
        services.AddHttpClient<IClientsApi, ClientsApi>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<IProjectsApi, ProjectsApi>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<ITimeEntriesApi, TimeEntriesApi>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<IInvoicesApi, InvoicesApi>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<IDashboardApi, DashboardApi>(client =>
            client.BaseAddress = new Uri(baseUrl));

        return services;
    }
}
