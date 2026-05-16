using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;
using TimeTracker.Web.Pages;

namespace TimeTracker.Web.Tests.Pages;

public class DashboardTests : TestContext
{
    private readonly IDashboardApi _dashboardApi = Substitute.For<IDashboardApi>();

    public DashboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton(_dashboardApi);
    }

    [Fact]
    public void Dashboard_WhenLoading_ShowsProgressRing()
    {
        _dashboardApi.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<DashboardSummaryDto>().Task);

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("fluent-progress-ring", cut.Markup);
    }

    [Fact]
    public void Dashboard_WhenApiThrows_ShowsErrorMessage()
    {
        _dashboardApi.GetSummaryAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("load the dashboard", cut.Markup);
    }

    [Fact]
    public void Dashboard_WhenLoaded_ShowsSummaryCards()
    {
        var summary = new DashboardSummaryDto(
            TotalClients: 5,
            TotalProjects: 12,
            TotalTimeEntries: 42,
            TotalHoursLogged: 150m,
            BillableHours: 100m,
            NonBillableHours: 50m,
            TotalInvoices: 8,
            OutstandingInvoiceTotal: 2500m);

        _dashboardApi.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);

        var cut = RenderComponent<Dashboard>();

        // Each summary value should appear in the rendered cards
        Assert.Contains(">5<", cut.Markup);
        Assert.Contains(">42<", cut.Markup);
    }
}
