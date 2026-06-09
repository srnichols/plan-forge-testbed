using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetByClientAsync(int? clientId, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Invoice> GenerateAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
    Task<Invoice> IssueAsync(int id, CancellationToken cancellationToken = default);
    Task<Invoice> MarkPaidAsync(int id, CancellationToken cancellationToken = default);
    Task<Invoice> VoidAsync(int id, string reason, CancellationToken cancellationToken = default);
}
