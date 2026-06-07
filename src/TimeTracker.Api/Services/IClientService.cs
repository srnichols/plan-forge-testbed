using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);
    Task<Client> UpdateAsync(int id, Client client, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
