using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class ClientService(TimeTrackerDbContext dbContext) : IClientService
{
    public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(client.Name, nameof(client.Name));

        if (client.HourlyRate <= 0)
        {
            throw new ArgumentException("Hourly rate must be greater than zero.", nameof(client.HourlyRate));
        }

        client.CreatedAt = DateTime.UtcNow;
        client.IsActive = true;

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return client;
    }

    public async Task<Client> UpdateAsync(int id, Client client, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(client.Name, nameof(client.Name));

        if (client.HourlyRate <= 0)
        {
            throw new ArgumentException("Hourly rate must be greater than zero.", nameof(client.HourlyRate));
        }

        var existing = await dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Client with ID {id} not found.");

        existing.Name = client.Name;
        existing.Email = client.Email;
        existing.HourlyRate = client.HourlyRate;

        await dbContext.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Client with ID {id} not found.");

        existing.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
