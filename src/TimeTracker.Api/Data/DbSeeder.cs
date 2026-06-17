using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Data;

/// <summary>
/// Dev-only demonstration seed data. Populates two clients, their projects, a work-week of
/// time entries (mix of billable and non-billable), and one issued invoice each so a clean
/// build comes up with data to show instead of empty dropdowns.
/// </summary>
public static class DbSeeder
{
    /// <summary>Tax rate applied to every seeded invoice (8.5%).</summary>
    private const decimal TaxRate = 0.085m;

    /// <summary>Builds a UTC-kind date in June 2026 (PostgreSQL timestamptz requires UTC).</summary>
    private static DateTime JuneDay(int day) => new(2026, 6, day, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Idempotently seeds demonstration data. No-op when any client already exists, so it is
    /// safe to call on every startup.
    /// </summary>
    public static async Task SeedAsync(TimeTrackerDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Clients.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedContosoAsync(db, cancellationToken);
        await SeedAdventureWorksAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedContosoAsync(TimeTrackerDbContext db, CancellationToken cancellationToken)
    {
        var client = new Client
        {
            Name = "Contoso Ltd",
            Email = "demo@contoso.com",
            HourlyRate = 150m,
        };

        var websiteRedesign = new Project { Name = "Website Redesign", Description = "Public marketing site refresh", Client = client };
        var mobileApp = new Project { Name = "Mobile App", Description = "Companion iOS/Android app", Client = client };
        client.Projects.Add(websiteRedesign);
        client.Projects.Add(mobileApp);

        // Billable work on Website Redesign totalling 29.5h across the work week.
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(1), Hours = 6.5m, Description = "Homepage layout", IsBillable = true });
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(2), Hours = 6.0m, Description = "Component library", IsBillable = true });
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(3), Hours = 5.5m, Description = "Responsive breakpoints", IsBillable = true });
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(4), Hours = 6.0m, Description = "Accessibility pass", IsBillable = true });
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(5), Hours = 5.5m, Description = "QA and polish", IsBillable = true });

        // Non-billable time — intentionally excluded from invoice totals.
        websiteRedesign.TimeEntries.Add(new TimeEntry { Date = JuneDay(3), Hours = 1.5m, Description = "Internal sync and planning", IsBillable = false });

        var invoice = BuildInvoice(
            client,
            invoiceNumber: "INV-2026-0001",
            periodStart: JuneDay(1),
            periodEnd: JuneDay(5),
            lines: [(websiteRedesign, "Website Redesign — June 1-5", 29.5m)]);

        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await Task.CompletedTask;
    }

    private static async Task SeedAdventureWorksAsync(TimeTrackerDbContext db, CancellationToken cancellationToken)
    {
        var client = new Client
        {
            Name = "Adventure Works",
            Email = "demo@adventure-works.test",
            HourlyRate = 175m,
        };

        var azureIntegration = new Project { Name = "Azure Integration", Description = "Cloud platform integration", Client = client };
        var teamsBot = new Project { Name = "Teams Bot", Description = "Microsoft Teams chat bot", Client = client };
        var copilotPlugin = new Project { Name = "Copilot Plugin", Description = "Copilot extensibility plugin", Client = client };
        client.Projects.Add(azureIntegration);
        client.Projects.Add(teamsBot);
        client.Projects.Add(copilotPlugin);

        // Billable work: Azure Integration 14h, Teams Bot 12h, Copilot Plugin 8h.
        azureIntegration.TimeEntries.Add(new TimeEntry { Date = JuneDay(8), Hours = 7m, Description = "Service Bus wiring", IsBillable = true });
        azureIntegration.TimeEntries.Add(new TimeEntry { Date = JuneDay(9), Hours = 7m, Description = "Managed identity setup", IsBillable = true });
        teamsBot.TimeEntries.Add(new TimeEntry { Date = JuneDay(10), Hours = 6m, Description = "Adaptive cards", IsBillable = true });
        teamsBot.TimeEntries.Add(new TimeEntry { Date = JuneDay(11), Hours = 6m, Description = "Conversation flow", IsBillable = true });
        copilotPlugin.TimeEntries.Add(new TimeEntry { Date = JuneDay(12), Hours = 8m, Description = "Manifest and auth", IsBillable = true });

        // Non-billable time — intentionally excluded from invoice totals.
        azureIntegration.TimeEntries.Add(new TimeEntry { Date = JuneDay(11), Hours = 2m, Description = "Internal design review", IsBillable = false });

        var invoice = BuildInvoice(
            client,
            invoiceNumber: "INV-2026-0002",
            periodStart: JuneDay(8),
            periodEnd: JuneDay(12),
            lines:
            [
                (azureIntegration, "Azure Integration — June 8-12", 14m),
                (teamsBot, "Teams Bot — June 8-12", 12m),
                (copilotPlugin, "Copilot Plugin — June 8-12", 8m),
            ]);

        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Builds an <see cref="InvoiceStatus.Issued"/> invoice from billable line definitions using
    /// the client hourly rate, standard rate type, no discount, and the shared 8.5% tax rate.
    /// </summary>
    private static Invoice BuildInvoice(
        Client client,
        string invoiceNumber,
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<(Project Project, string Description, decimal Hours)> lines)
    {
        var invoice = new Invoice
        {
            Client = client,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Issued,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DiscountPercent = 0m,
            DiscountAmount = 0m,
            TaxRate = TaxRate,
            IssuedAt = DateTime.UtcNow,
        };

        decimal subtotal = 0m;
        foreach (var (project, description, hours) in lines)
        {
            var lineTotal = decimal.Round(hours * client.HourlyRate, 2, MidpointRounding.AwayFromZero);
            subtotal += lineTotal;
            invoice.InvoiceLines.Add(new InvoiceLine
            {
                Project = project,
                Description = description,
                Hours = hours,
                HourlyRate = client.HourlyRate,
                RateType = RateType.Standard,
                LineTotal = lineTotal,
            });
        }

        invoice.Subtotal = subtotal;
        invoice.TaxAmount = decimal.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        invoice.Total = invoice.Subtotal + invoice.TaxAmount;

        return invoice;
    }
}
