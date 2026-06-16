using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class TimeEntryService(TimeTrackerDbContext dbContext, ILogger<TimeEntryService> logger) : ITimeEntryService
{
    public async Task<IReadOnlyList<TimeEntry>> GetAllAsync(DateTime? date = null, int? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TimeEntries.AsNoTracking();

        if (date.HasValue)
        {
            var day = date.Value.Date;
            query = query.Where(t => t.Date.Date == day);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.TimeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TimeEntry> CreateAsync(int projectId, DateTime date, decimal hours, string? description, bool isBillable, CancellationToken cancellationToken = default)
    {
        ValidateInput(hours, description);

        var projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            throw new ValidationException($"Project {projectId} does not exist.");
        }

        var entry = new TimeEntry
        {
            ProjectId = projectId,
            Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            Hours = hours,
            Description = description?.Trim(),
            IsBillable = isBillable,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.TimeEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogTimeEntryCreated(logger, entry.Id, entry.ProjectId);
        return entry;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.TimeEntries.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        dbContext.TimeEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogTimeEntryDeleted(logger, id);
        return true;
    }

    private static void ValidateInput(decimal hours, string? description)
    {
        if (hours is <= 0m or > 24m)
        {
            throw new ValidationException("Hours must be between 0.01 and 24.");
        }

        if (description is not null && description.Trim().Length > 1000)
        {
            throw new ValidationException("Description must be 1000 characters or fewer.");
        }
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Time entry created: {TimeEntryId} (project {ProjectId})")]
    private static partial void LogTimeEntryCreated(ILogger logger, int timeEntryId, int projectId);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Time entry deleted: {TimeEntryId}")]
    private static partial void LogTimeEntryDeleted(ILogger logger, int timeEntryId);
}
