using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using TimeTracker.Web.Client;
using TimeTracker.Web.Client.Models;
using TimeTracker.Web.Pages.TimeEntries;

namespace TimeTracker.Web.Tests.Pages;

public class TimeEntryCreateTests : TestContext
{
    private readonly ITimeEntriesApi _timeEntriesApi = Substitute.For<ITimeEntriesApi>();
    private readonly IProjectsApi _projectsApi = Substitute.For<IProjectsApi>();

    public TimeEntryCreateTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton(_timeEntriesApi);
        Services.AddSingleton(_projectsApi);
    }

    [Fact]
    public void TimeEntryCreate_WhenHoursIsZero_ShowsValidationMessage()
    {
        _projectsApi.GetAllAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectDto>());

        var cut = RenderComponent<TimeEntryCreate>();

        // Default model: Hours = 0 — submit triggers range validation
        cut.Find("form").Submit();

        Assert.Contains("Hours must be between", cut.Markup);
    }

    [Fact]
    public async Task TimeEntryCreate_WithValidData_CallsCreateAsync()
    {
        var project = new ProjectDto { Id = 1, Name = "Test Project", ClientId = 1, IsActive = true };
        _projectsApi.GetAllAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectDto> { project });

        var timeEntry = new TimeEntryDto { Id = 1, ProjectId = 1, Hours = 8m, Date = DateTime.Today };
        _timeEntriesApi.CreateAsync(Arg.Any<TimeEntryFormModel>(), Arg.Any<CancellationToken>())
            .Returns(timeEntry);

        var cut = RenderComponent<TimeEntryCreate>();

        // Set valid model values via reflection — bypasses FluentUI web component event binding
        await cut.InvokeAsync(() =>
        {
            var modelField = typeof(TimeEntryCreate)
                .GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var model = (TimeEntryFormModel)modelField.GetValue(cut.Instance)!;
            model.ProjectId = 1;
            model.Hours = 8m;
            model.Date = DateTime.Today;
        });

        cut.Find("form").Submit();

        await _timeEntriesApi.Received(1)
            .CreateAsync(Arg.Any<TimeEntryFormModel>(), Arg.Any<CancellationToken>());
    }
}
