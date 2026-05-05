using System.Net.Http.Json;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public class InvoicesApi(HttpClient httpClient) : IInvoicesApi
{
    public async Task<InvoiceDto> GenerateAsync(GenerateInvoiceFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/invoices/generate", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceDto>(ct))!;
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/invoices/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvoiceDto>(ct);
    }

    public async Task<List<InvoiceDto>> GetByClientAsync(int clientId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<InvoiceDto>>($"api/invoices?clientId={clientId}", ct) ?? [];

    public async Task<InvoiceDto> IssueAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"api/invoices/{id}/issue", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceDto>(ct))!;
    }

    public async Task<InvoiceDto> MarkPaidAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"api/invoices/{id}/pay", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceDto>(ct))!;
    }

    public async Task<InvoiceDto> VoidAsync(int id, VoidInvoiceFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/invoices/{id}/void", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceDto>(ct))!;
    }
}
