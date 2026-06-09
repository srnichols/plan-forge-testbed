using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public interface IInvoiceService
{
    Task<Invoice> GenerateInvoiceAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
    Task<Invoice?> GetInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetClientInvoicesAsync(int clientId, CancellationToken cancellationToken = default);
    Task<Invoice?> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<Invoice?> MarkPaidAsync(int id, CancellationToken cancellationToken = default);
    Task<Invoice?> VoidInvoiceAsync(int id, string reason, CancellationToken cancellationToken = default);
}
