using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class TimeEntryReportService(TimeTrackerDbContext db) : ITimeEntryReportService
{
    public async Task<HoursSummaryResponse> GetHoursSummaryAsync(
        DateOnly start, DateOnly end, int? projectId = null, CancellationToken ct = default)
    {
        var query = db.TimeEntries
            .Where(e => DateOnly.FromDateTime(e.Date) >= start && DateOnly.FromDateTime(e.Date) <= end);

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        var entries = await query.ToListAsync(ct);

        decimal totalHours = entries.Sum(e => e.Hours);
        decimal billableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
        decimal nonBillableHours = totalHours - billableHours;

        return new HoursSummaryResponse(
            TotalHours: totalHours,
            BillableHours: billableHours,
            NonBillableHours: nonBillableHours,
            EntryCount: entries.Count,
            PeriodStart: start,
            PeriodEnd: end);
    }

    public async Task<ProjectBreakdownResponse> GetProjectBreakdownAsync(
        DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var entries = await db.TimeEntries
            .Include(e => e.Project)
            .Where(e => DateOnly.FromDateTime(e.Date) >= start && DateOnly.FromDateTime(e.Date) <= end)
            .ToListAsync(ct);

        decimal totalHours = entries.Sum(e => e.Hours);

        var projects = entries
            .GroupBy(e => new { e.ProjectId, e.Project.Name })
            .Select(g => new ProjectBreakdownItem(
                ProjectId: g.Key.ProjectId,
                ProjectName: g.Key.Name,
                TotalHours: g.Sum(e => e.Hours),
                BillableHours: g.Where(e => e.IsBillable).Sum(e => e.Hours),
                NonBillableHours: g.Where(e => !e.IsBillable).Sum(e => e.Hours),
                PercentageOfTotal: totalHours > 0
                    ? Math.Round(g.Sum(e => e.Hours) / totalHours * 100, 2)
                    : 0))
            .OrderByDescending(p => p.TotalHours)
            .ToList();

        return new ProjectBreakdownResponse(
            PeriodStart: start,
            PeriodEnd: end,
            TotalHours: totalHours,
            Projects: projects);
    }

    public async Task<DailyTimelineResponse> GetDailyTimelineAsync(
        DateOnly start, DateOnly end, int? projectId = null, CancellationToken ct = default)
    {
        var query = db.TimeEntries
            .Where(e => DateOnly.FromDateTime(e.Date) >= start && DateOnly.FromDateTime(e.Date) <= end);

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        var entries = await query.ToListAsync(ct);

        var days = entries
            .GroupBy(e => DateOnly.FromDateTime(e.Date))
            .Select(g => new DailyTimelineEntry(
                Date: g.Key,
                TotalHours: g.Sum(e => e.Hours),
                BillableHours: g.Where(e => e.IsBillable).Sum(e => e.Hours),
                NonBillableHours: g.Where(e => !e.IsBillable).Sum(e => e.Hours),
                EntryCount: g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return new DailyTimelineResponse(
            PeriodStart: start,
            PeriodEnd: end,
            TotalHours: entries.Sum(e => e.Hours),
            Days: days);
    }
}
