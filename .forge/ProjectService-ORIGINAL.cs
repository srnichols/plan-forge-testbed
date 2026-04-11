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
        var query = db.Projects.Where(p => p.IsActive);
        if (clientId.HasValue)
            query = query.Where(p => p.ClientId == clientId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Projects.Where(p => p.Id == id && p.IsActive).FirstOrDefaultAsync(ct);

    public async Task<Project> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required");

        var client = await db.Clients
            .Where(c => c.Id == request.ClientId)
            .FirstOrDefaultAsync(ct);

        if (client == null)
            throw new ValidationException("Client " + request.ClientId + " does not exist");

        if (!client.IsActive)
            throw new ValidationException("Client " + request.ClientId + " is not active");

        var duplicate = await db.Projects
            .Where(p => p.ClientId == request.ClientId && p.Name == request.Name.Trim() && p.IsActive)
            .AnyAsync(ct);

        if (duplicate)
            throw new ValidationException("A project named " + request.Name.Trim() + " already exists for this client");

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description == null ? null : request.Description.Trim(),
            ClientId = request.ClientId,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required");

        var project = await db.Projects
            .Where(p => p.Id == id && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Project " + id + " not found");

        var duplicate = await db.Projects
            .Where(p => p.ClientId == project.ClientId && p.Name == request.Name.Trim() && p.IsActive && p.Id != id)
            .AnyAsync(ct);

        if (duplicate)
            throw new ValidationException("A project named " + request.Name.Trim() + " already exists for this client");

        project.Name = request.Name.Trim();
        project.Description = request.Description == null ? null : request.Description.Trim();

        await db.SaveChangesAsync(ct);
        return project;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var project = await db.Projects
            .Where(p => p.Id == id && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Project " + id + " not found");

        project.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
