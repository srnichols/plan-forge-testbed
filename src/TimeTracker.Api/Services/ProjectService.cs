using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class ProjectService(TimeTrackerDbContext dbContext) : IProjectService
{
    public async Task<IEnumerable<Project>> GetAllAsync(int? clientId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Projects.Where(p => p.IsActive);

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project.Name, nameof(project.Name));

        project.IsActive = true;
        project.CreatedAt = DateTime.UtcNow;

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<Project> UpdateAsync(int id, Project project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project.Name, nameof(project.Name));

        var existing = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {id} not found.");

        existing.Name = project.Name;
        existing.Description = project.Description;
        existing.ClientId = project.ClientId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {id} not found.");

        existing.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
