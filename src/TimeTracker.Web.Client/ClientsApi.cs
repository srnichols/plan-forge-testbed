using System.Net.Http.Json;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public class ClientsApi(HttpClient httpClient) : IClientsApi
{
    public async Task<List<ClientDto>> GetAllAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<ClientDto>>("api/clients", ct) ?? [];

    public async Task<ClientDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/clients/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClientDto>(ct);
    }

    public async Task<ClientDto> CreateAsync(ClientFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/clients", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientDto>(ct))!;
    }

    public async Task<ClientDto> UpdateAsync(int id, ClientFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/clients/{id}", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientDto>(ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/clients/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
