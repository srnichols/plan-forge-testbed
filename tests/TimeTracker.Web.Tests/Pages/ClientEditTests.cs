using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;
using TimeTracker.Web.Pages.Clients;

namespace TimeTracker.Web.Tests.Pages;

public class ClientEditTests : TestContext
{
    private readonly IClientsApi _clientsApi = Substitute.For<IClientsApi>();

    public ClientEditTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton(_clientsApi);
    }

    [Fact]
    public void ClientEdit_WhenNameEmptyAndSubmitted_ShowsValidationMessage()
    {
        // New client mode (no Id parameter) — _loading = false immediately, form is shown
        var cut = RenderComponent<ClientEdit>();

        // Submit the form with default (empty) Name to trigger validation
        cut.Find("form").Submit();

        Assert.Contains("Name is required", cut.Markup);
    }

    [Fact]
    public void ClientEdit_WhenSubmitting_ShowsSavingLabel()
    {
        // Keep the API call pending so _submitting stays true during the await
        var tcs = new TaskCompletionSource<ClientDto>();
        _clientsApi.CreateAsync(Arg.Any<ClientFormModel>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<ClientEdit>(); // new client mode — form is immediately visible

        // Set valid form data directly on the model so DataAnnotationsValidator passes
        var form = (ClientFormModel)typeof(ClientEdit)
            .GetField("_form", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
        form.Name = "Test Client";
        form.HourlyRate = 100m;

        // Submit — Blazor validates, OnValidSubmit fires, SubmitAsync sets _submitting=true,
        // then suspends at the pending TCS. bUnit re-renders before returning.
        cut.Find("form").Submit();

        // Button label changes to "Saving…" while the API call is in-flight
        Assert.Contains("Saving", cut.Markup);

        // Structural harden (Slice 6 §3): both submit and cancel buttons must
        // render with the `disabled` attribute while _submitting=true, so the
        // user cannot double-submit or cancel mid-flight. Label-only assertions
        // would silently pass if Disabled binding regressed.
        var disabledButtons = cut.FindAll("fluent-button[disabled]");
        Assert.Equal(2, disabledButtons.Count);
    }
}
