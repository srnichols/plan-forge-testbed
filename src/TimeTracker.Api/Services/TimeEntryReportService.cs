using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class TimeEntryReportService(TimeTrackerDbContext dbContext, ILogger<TimeEntryReportService> logger) : ITimeEntryReportService
{
    public async Task<HoursSummaryResponse> GetHoursSummaryAsync(
        DateOnly start,
        DateOnly end,
        int? projectId = null,
        int? clientId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(start, end);

        var entries = await QueryEntries(start, end, projectId, clientId)
            .Select(t => new { t.Hours, t.IsBillable })
            .ToListAsync(cancellationToken);

        var totalHours = entries.Sum(e => e.Hours);
        var billableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
        var nonBillableHours = totalHours - billableHours;

        LogReportGenerated(logger, nameof(GetHoursSummaryAsync), start, end, entries.Count);

        return new HoursSummaryResponse(
            TotalHours: totalHours,
            BillableHours: billableHours,
            NonBillableHours: nonBillableHours,
            EntryCount: entries.Count,
            PeriodStart: start,
            PeriodEnd: end);
    }

    public async Task<ProjectBreakdownResponse> GetProjectBreakdownAsync(
        DateOnly start,
        DateOnly end,
        int? clientId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(start, end);

        var rows = await QueryEntries(start, end, projectId: null, clientId: clientId)
            .Select(t => new
            {
                t.ProjectId,
                ProjectName = t.Project.Name,
                t.Hours,
                t.IsBillable,
            })
            .ToListAsync(cancellationToken);

        var totalHours = rows.Sum(r => r.Hours);

        var projects = rows
            .GroupBy(r => new { r.ProjectId, r.ProjectName })
            .Select(g =>
            {
                var projectTotal = g.Sum(r => r.Hours);
                var billable = g.Where(r => r.IsBillable).Sum(r => r.Hours);
                var percentage = totalHours == 0m
                    ? 0m
                    : decimal.Round(projectTotal / totalHours * 100m, 2, MidpointRounding.ToEven);

                return new ProjectBreakdownItem(
                    ProjectId: g.Key.ProjectId,
                    ProjectName: g.Key.ProjectName,
                    TotalHours: projectTotal,
                    BillableHours: billable,
                    NonBillableHours: projectTotal - billable,
                    PercentageOfTotal: percentage);
            })
            .OrderByDescending(p => p.TotalHours)
            .ThenBy(p => p.ProjectName)
            .ToList();

        LogReportGenerated(logger, nameof(GetProjectBreakdownAsync), start, end, rows.Count);

        return new ProjectBreakdownResponse(
            PeriodStart: start,
            PeriodEnd: end,
            TotalHours: totalHours,
            Projects: projects);
    }

    public async Task<DailyTimelineResponse> GetDailyTimelineAsync(
        DateOnly start,
        DateOnly end,
        int? projectId = null,
        int? clientId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(start, end);

        var rows = await QueryEntries(start, end, projectId, clientId)
            .Select(t => new { t.Date, t.Hours, t.IsBillable })
            .ToListAsync(cancellationToken);

        var totalHours = rows.Sum(r => r.Hours);

        var days = rows
            .GroupBy(r => DateOnly.FromDateTime(r.Date))
            .Select(g =>
            {
                var dayTotal = g.Sum(r => r.Hours);
                var billable = g.Where(r => r.IsBillable).Sum(r => r.Hours);
                return new DailyTimelineEntry(
                    Date: g.Key,
                    TotalHours: dayTotal,
                    BillableHours: billable,
                    NonBillableHours: dayTotal - billable,
                    EntryCount: g.Count());
            })
            .OrderBy(d => d.Date)
            .ToList();

        LogReportGenerated(logger, nameof(GetDailyTimelineAsync), start, end, rows.Count);

        return new DailyTimelineResponse(
            PeriodStart: start,
            PeriodEnd: end,
            TotalHours: totalHours,
            Days: days);
    }

    private IQueryable<TimeEntry> QueryEntries(DateOnly start, DateOnly end, int? projectId, int? clientId)
    {
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = end.ToDateTime(TimeOnly.MaxValue);

        var query = dbContext.TimeEntries
            .AsNoTracking()
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (projectId is int pid)
        {
            query = query.Where(t => t.ProjectId == pid);
        }

        if (clientId is int cid)
        {
            query = query.Where(t => t.Project.ClientId == cid);
        }

        return query;
    }

    private static void ValidateDateRange(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new ValidationException("Period start must be on or before period end.");
        }
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Report generated: {ReportName} for {Start}..{End} ({EntryCount} entries)")]
    private static partial void LogReportGenerated(ILogger logger, string reportName, DateOnly start, DateOnly end, int entryCount);
}
