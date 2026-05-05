using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Client;

public interface IInvoicesApi
{
    Task<InvoiceDto> GenerateAsync(GenerateInvoiceFormModel model, CancellationToken ct = default);
    Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<InvoiceDto>> GetByClientAsync(int clientId, CancellationToken ct = default);
    Task<InvoiceDto> IssueAsync(int id, CancellationToken ct = default);
    Task<InvoiceDto> MarkPaidAsync(int id, CancellationToken ct = default);
    Task<InvoiceDto> VoidAsync(int id, VoidInvoiceFormModel model, CancellationToken ct = default);
}
