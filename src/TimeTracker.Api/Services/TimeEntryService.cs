using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class TimeEntryService(TimeTrackerDbContext dbContext) : ITimeEntryService
{
    public async Task<IEnumerable<TimeEntry>> GetAllAsync(DateTime? date, int? projectId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TimeEntries.AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(t => t.Date.Date == date.Value.Date);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        return await query
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TimeEntry> CreateAsync(TimeEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Hours <= 0 || entry.Hours > 24)
        {
            throw new ArgumentException("Hours must be between 0.01 and 24.", nameof(entry.Hours));
        }

        entry.Description ??= string.Empty;
        entry.CreatedAt = DateTime.UtcNow;

        dbContext.TimeEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Time entry with ID {id} not found.");

        dbContext.TimeEntries.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
