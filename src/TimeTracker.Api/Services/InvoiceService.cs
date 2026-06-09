using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Data;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Services;

public class InvoiceService(TimeTrackerDbContext dbContext) : IInvoiceService
{
    public async Task<IEnumerable<Invoice>> GetByClientAsync(int? clientId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.Include(i => i.InvoiceLines).AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(i => i.ClientId == clientId.Value);
        }

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Invoices
            .Include(i => i.InvoiceLines)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice> GenerateAsync(int clientId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var invoice = new Invoice
        {
            ClientId = clientId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            Status = InvoiceStatus.Draft,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Subtotal = 0m,
            Total = 0m,
            CreatedAt = DateTime.UtcNow,
            InvoiceLines = []
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<Invoice> IssueAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await FindOrThrowAsync(id, cancellationToken);

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<Invoice> MarkPaidAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await FindOrThrowAsync(id, cancellationToken);

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<Invoice> VoidAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await FindOrThrowAsync(id, cancellationToken);

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    private async Task<Invoice> FindOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Invoices.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");
    }
}
