using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class TimeEntryReportService(TimeTrackerDbContext db) : ITimeEntryReportService
{
    public async Task<HoursSummaryResponse> GetHoursSummaryAsync(
        DateOnly start, DateOnly end, int? projectId = null, CancellationToken ct = default)
    {
        (DateTime startDt, DateTime endExclusiveDt) = ToDateTimeBounds(start, end);

        IQueryable<TimeEntry> query = db.TimeEntries
            .AsNoTracking()
            .Where(e => e.Date >= startDt && e.Date < endExclusiveDt);

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        var agg = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Sum(e => e.Hours),
                Billable = g.Sum(e => e.IsBillable ? e.Hours : 0m),
                EntryCount = g.Count()
            })
            .SingleOrDefaultAsync(ct);

        if (agg is null)
        {
            return new HoursSummaryResponse(
                TotalHours: 0m,
                BillableHours: 0m,
                NonBillableHours: 0m,
                EntryCount: 0,
                PeriodStart: start,
                PeriodEnd: end);
        }

        return new HoursSummaryResponse(
            TotalHours: agg.Total,
            BillableHours: agg.Billable,
            NonBillableHours: agg.Total - agg.Billable,
            EntryCount: agg.EntryCount,
            PeriodStart: start,
            PeriodEnd: end);
    }

    public async Task<ProjectBreakdownResponse> GetProjectBreakdownAsync(
        DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        (DateTime startDt, DateTime endExclusiveDt) = ToDateTimeBounds(start, end);

        List<ProjectRow> rows = await db.TimeEntries
            .AsNoTracking()
            .Where(e => e.Date >= startDt && e.Date < endExclusiveDt)
            .GroupBy(e => new { e.ProjectId, ProjectName = e.Project!.Name })
            .Select(g => new ProjectRow(
                g.Key.ProjectId,
                g.Key.ProjectName,
                g.Sum(e => e.Hours),
                g.Sum(e => e.IsBillable ? e.Hours : 0m)))
            .ToListAsync(ct);

        decimal totalHours = rows.Sum(r => r.Total);

        List<ProjectBreakdownItem> projects = rows
            .Select(r => new ProjectBreakdownItem(
                ProjectId: r.ProjectId,
                ProjectName: r.ProjectName,
                TotalHours: r.Total,
                BillableHours: r.Billable,
                NonBillableHours: r.Total - r.Billable,
                PercentageOfTotal: totalHours > 0m
                    ? Math.Round(r.Total / totalHours * 100m, 2)
                    : 0m))
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
        (DateTime startDt, DateTime endExclusiveDt) = ToDateTimeBounds(start, end);

        IQueryable<TimeEntry> query = db.TimeEntries
            .AsNoTracking()
            .Where(e => e.Date >= startDt && e.Date < endExclusiveDt);

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        var rows = await query
            .GroupBy(e => e.Date.Date)
            .Select(g => new
            {
                Day = g.Key,
                Total = g.Sum(e => e.Hours),
                Billable = g.Sum(e => e.IsBillable ? e.Hours : 0m),
                Count = g.Count()
            })
            .OrderBy(x => x.Day)
            .ToListAsync(ct);

        List<DailyTimelineEntry> days = rows
            .Select(x => new DailyTimelineEntry(
                Date: DateOnly.FromDateTime(x.Day),
                TotalHours: x.Total,
                BillableHours: x.Billable,
                NonBillableHours: x.Total - x.Billable,
                EntryCount: x.Count))
            .ToList();

        return new DailyTimelineResponse(
            PeriodStart: start,
            PeriodEnd: end,
            TotalHours: rows.Sum(r => r.Total),
            Days: days);
    }

    private static (DateTime StartDt, DateTime EndExclusiveDt) ToDateTimeBounds(DateOnly start, DateOnly end)
        => (start.ToDateTime(TimeOnly.MinValue), end.AddDays(1).ToDateTime(TimeOnly.MinValue));

    private sealed record ProjectRow(int ProjectId, string ProjectName, decimal Total, decimal Billable);
}
