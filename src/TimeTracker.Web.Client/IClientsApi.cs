using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public interface IClientsApi
{
    Task<List<ClientDto>> GetAllAsync(CancellationToken ct = default);
    Task<ClientDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ClientDto> CreateAsync(ClientFormModel model, CancellationToken ct = default);
    Task<ClientDto> UpdateAsync(int id, ClientFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
