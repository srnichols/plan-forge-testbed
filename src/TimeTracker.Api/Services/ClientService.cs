using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public sealed partial class ClientService(TimeTrackerDbContext dbContext, ILogger<ClientService> logger) : IClientService
{
    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Clients
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client> CreateAsync(string name, string? email, decimal hourlyRate, CancellationToken cancellationToken = default)
    {
        ValidateInput(name, email, hourlyRate);

        var client = new Client
        {
            Name = name.Trim(),
            Email = (email ?? string.Empty).Trim(),
            HourlyRate = hourlyRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogClientCreated(logger, client.Id, client.Name);
        return client;
    }

    public async Task<Client?> UpdateAsync(int id, string name, string? email, decimal hourlyRate, CancellationToken cancellationToken = default)
    {
        ValidateInput(name, email, hourlyRate);

        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return null;
        }

        client.Name = name.Trim();
        client.Email = (email ?? string.Empty).Trim();
        client.HourlyRate = hourlyRate;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogClientUpdated(logger, client.Id);
        return client;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        if (!client.IsActive)
        {
            return true;
        }

        client.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        LogClientDeactivated(logger, client.Id);
        return true;
    }

    private static void ValidateInput(string name, string? email, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Client name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ValidationException("Client name must be 200 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var trimmed = email.Trim();
            if (trimmed.Length > 200)
            {
                throw new ValidationException("Client email must be 200 characters or fewer.");
            }

            if (!EmailRegex().IsMatch(trimmed))
            {
                throw new ValidationException("Client email is not a valid email address.");
            }
        }

        if (hourlyRate <= 0)
        {
            throw new ValidationException("Hourly rate must be greater than zero.");
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial System.Text.RegularExpressions.Regex EmailRegex();

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Client created: {ClientId} ({ClientName})")]
    private static partial void LogClientCreated(ILogger logger, int clientId, string clientName);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Client updated: {ClientId}")]
    private static partial void LogClientUpdated(ILogger logger, int clientId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Client deactivated: {ClientId}")]
    private static partial void LogClientDeactivated(ILogger logger, int clientId);
}
