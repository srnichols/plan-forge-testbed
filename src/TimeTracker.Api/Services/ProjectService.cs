using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync(int? clientId = null, CancellationToken ct = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Project> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<Project> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public record CreateProjectRequest(string Name, string? Description, int ClientId);

public record UpdateProjectRequest(string Name, string? Description);

public class ProjectService(TimeTrackerDbContext db) : IProjectService
{
    public async Task<IReadOnlyList<Project>> GetAllAsync(int? clientId = null, CancellationToken ct = default)
    {
        var query = db.Projects.AsNoTracking().Where(p => p.IsActive);
        if (clientId.HasValue)
            query = query.Where(p => p.ClientId == clientId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Projects.AsNoTracking().Where(p => p.Id == id && p.IsActive).FirstOrDefaultAsync(ct);

    public async Task<Project> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string name = ValidateAndNormalizeName(request.Name);

        var client = await db.Clients
            .AsNoTracking()
            .Where(c => c.Id == request.ClientId)
            .Select(c => new { c.Id, c.IsActive })
            .FirstOrDefaultAsync(ct);

        if (client == null)
            throw new ValidationException($"Client {request.ClientId} does not exist");

        if (!client.IsActive)
            throw new ValidationException($"Client {request.ClientId} is not active");

        var duplicate = await db.Projects
            .Where(p => p.ClientId == request.ClientId && p.Name == name && p.IsActive)
            .AnyAsync(ct);

        if (duplicate)
            throw new ValidationException($"A project named {name} already exists for this client");

        var project = new Project
        {
            Name = name,
            Description = request.Description?.Trim(),
            ClientId = request.ClientId,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string name = ValidateAndNormalizeName(request.Name);

        var project = await db.Projects
            .Where(p => p.Id == id && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Project {id} not found");

        var duplicate = await db.Projects
            .Where(p => p.ClientId == project.ClientId && p.Name == name && p.IsActive && p.Id != id)
            .AnyAsync(ct);

        if (duplicate)
            throw new ValidationException($"A project named {name} already exists for this client");

        project.Name = name;
        project.Description = request.Description?.Trim();

        await db.SaveChangesAsync(ct);
        return project;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var project = await db.Projects
            .Where(p => p.Id == id && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Project {id} not found");

        project.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static string ValidateAndNormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name is required");
        return name.Trim();
    }
}
