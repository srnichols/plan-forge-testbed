using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;
using TimeTracker.Web.Pages.Clients;

namespace TimeTracker.Web.Tests.Pages;

public class ClientsListTests : TestContext
{
    private readonly IClientsApi _clientsApi = Substitute.For<IClientsApi>();

    public ClientsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton(_clientsApi);
    }

    [Fact]
    public void ClientsList_WhenLoading_ShowsProgressRing()
    {
        _clientsApi.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<List<ClientDto>>().Task);

        var cut = RenderComponent<ClientsList>();

        Assert.Contains("fluent-progress-ring", cut.Markup);
    }

    [Fact]
    public void ClientsList_WhenApiThrows_ShowsErrorMessage()
    {
        _clientsApi.GetAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var cut = RenderComponent<ClientsList>();

        Assert.Contains("load clients", cut.Markup);
    }

    [Fact]
    public void ClientsList_WhenLoaded_ShowsClientsInGrid()
    {
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Acme Corp", Email = "acme@test.com", HourlyRate = 100m, IsActive = true },
            new() { Id = 2, Name = "Beta LLC",  Email = "beta@test.com",  HourlyRate = 150m, IsActive = true }
        };

        _clientsApi.GetAllAsync(Arg.Any<CancellationToken>()).Returns(clients);

        var cut = RenderComponent<ClientsList>();

        Assert.Contains("Acme Corp", cut.Markup);
        Assert.Contains("Beta LLC", cut.Markup);
    }
}
