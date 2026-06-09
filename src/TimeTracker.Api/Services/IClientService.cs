using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Client> CreateAsync(string name, string? email, decimal hourlyRate, CancellationToken cancellationToken = default);
    Task<Client?> UpdateAsync(int id, string name, string? email, decimal hourlyRate, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
