using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class ProjectService(TimeTrackerDbContext dbContext, ILogger<ProjectService> logger) : IProjectService
{
    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project> CreateAsync(string name, string? description, int clientId, CancellationToken cancellationToken = default)
    {
        ValidateInput(name, description);

        await EnsureClientExistsAsync(clientId, cancellationToken);
        await EnsureNoDuplicateNameAsync(clientId, name.Trim(), projectIdToExclude: null, cancellationToken);

        var project = new Project
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ClientId = clientId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogProjectCreated(logger, project.Id, project.Name, project.ClientId);
        return project;
    }

    public async Task<Project?> UpdateAsync(int id, string name, string? description, int clientId, CancellationToken cancellationToken = default)
    {
        ValidateInput(name, description);

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        await EnsureClientExistsAsync(clientId, cancellationToken);
        await EnsureNoDuplicateNameAsync(clientId, name.Trim(), projectIdToExclude: id, cancellationToken);

        project.Name = name.Trim();
        project.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        project.ClientId = clientId;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogProjectUpdated(logger, project.Id);
        return project;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        if (!project.IsActive)
        {
            return true;
        }

        project.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        LogProjectDeactivated(logger, project.Id);
        return true;
    }

    private async Task EnsureClientExistsAsync(int clientId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Clients.AnyAsync(c => c.Id == clientId, cancellationToken);
        if (!exists)
        {
            throw new ValidationException($"Client with id {clientId} does not exist.");
        }
    }

    private async Task EnsureNoDuplicateNameAsync(int clientId, string name, int? projectIdToExclude, CancellationToken cancellationToken)
    {
        var duplicate = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p =>
                    p.ClientId == clientId
                    && p.Name == name
                    && (projectIdToExclude == null || p.Id != projectIdToExclude),
                cancellationToken);

        if (duplicate)
        {
            throw new ValidationException($"A project named '{name}' already exists for this client.");
        }
    }

    private static void ValidateInput(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Project name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ValidationException("Project name must be 200 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 2000)
        {
            throw new ValidationException("Project description must be 2000 characters or fewer.");
        }
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Project created: {ProjectId} ({ProjectName}) for client {ClientId}")]
    private static partial void LogProjectCreated(ILogger logger, int projectId, string projectName, int clientId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Project updated: {ProjectId}")]
    private static partial void LogProjectUpdated(ILogger logger, int projectId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Project deactivated: {ProjectId}")]
    private static partial void LogProjectDeactivated(ILogger logger, int projectId);
}
