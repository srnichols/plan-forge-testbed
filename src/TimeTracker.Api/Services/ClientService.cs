using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;
public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task<Client?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Client> CreateAsync(CreateClientRequest request, CancellationToken ct = default);
    Task<Client> UpdateAsync(int id, UpdateClientRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public record CreateClientRequest(string Name, string? Email, decimal HourlyRate);

public record UpdateClientRequest(string Name, string? Email, decimal HourlyRate);

public partial class ClientService(TimeTrackerDbContext db) : IClientService
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
        => await db.Clients.Where(c => c.IsActive).ToListAsync(ct);

    public async Task<Client?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Clients.Where(c => c.Id == id && c.IsActive).FirstOrDefaultAsync(ct);

    public async Task<Client> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        ValidateRequest(request.Name, request.Email, request.HourlyRate);

        var client = new Client
        {
            Name = request.Name.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            HourlyRate = request.HourlyRate,
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client;
    }

    public async Task<Client> UpdateAsync(int id, UpdateClientRequest request, CancellationToken ct = default)
    {
        ValidateRequest(request.Name, request.Email, request.HourlyRate);

        var client = await db.Clients
            .Where(c => c.Id == id && c.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Client {id} not found");

        client.Name = request.Name.Trim();
        client.Email = request.Email?.Trim() ?? string.Empty;
        client.HourlyRate = request.HourlyRate;

        await db.SaveChangesAsync(ct);
        return client;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var client = await db.Clients
            .Where(c => c.Id == id && c.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Client {id} not found");

        client.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateRequest(string name, string? email, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name is required");

        if (!string.IsNullOrEmpty(email) && !EmailRegex().IsMatch(email))
            throw new ValidationException("Email format is invalid");

        if (hourlyRate <= 0)
            throw new ValidationException("HourlyRate must be greater than 0");
    }
}
