using System.Net.Http.Json;
using System.Text;
using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public sealed class TimeEntriesApi(HttpClient httpClient) : ITimeEntriesApi
{
    public async Task<List<TimeEntryDto>> GetAllAsync(DateTime? date = null, int? projectId = null, CancellationToken ct = default)
    {
        var sb = new StringBuilder("api/timeentries");
        var sep = '?';
        if (date.HasValue) { sb.Append($"{sep}date={date.Value:yyyy-MM-dd}"); sep = '&'; }
        if (projectId.HasValue) { sb.Append($"{sep}projectId={projectId}"); }
        return await httpClient.GetFromJsonAsync<List<TimeEntryDto>>(sb.ToString(), ct) ?? [];
    }

    public async Task<TimeEntryDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/timeentries/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TimeEntryDto>(ct);
    }

    public async Task<TimeEntryDto> CreateAsync(TimeEntryFormModel model, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/timeentries", model, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeEntryDto>(ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/timeentries/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
