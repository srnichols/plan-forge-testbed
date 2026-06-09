using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Data;

/// <summary>
/// Seeds demonstration data when the database has no clients. Loads two clients,
/// each with multiple projects, a work week of time entries, and one generated
/// invoice computed from their billable hours. Idempotent: running it against a
/// populated database is a no-op, so it is safe to call on every startup.
/// </summary>
public static class DbSeeder
{
    private const decimal TaxRate = 8.5m;

    public static async Task SeedAsync(TimeTrackerDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Clients.AnyAsync(cancellationToken))
        {
            return;
        }

        SeedContoso(db);
        SeedAdventureWorks(db);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void SeedContoso(TimeTrackerDbContext db)
    {
        var client = new Client
        {
            Name = "Contoso Ltd",
            Email = "demo@contoso.com",
            HourlyRate = 150m,
            IsActive = true
        };

        var websiteProject = new Project
        {
            Name = "Website Redesign",
            Description = "Q2 corporate site refresh",
            Client = client,
            IsActive = true
        };

        var mobileProject = new Project
        {
            Name = "Mobile App",
            Description = "iOS and Android client portal",
            Client = client,
            IsActive = true
        };

        var entries = new List<TimeEntry>
        {
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 1), Hours = 8m, Description = "Discovery and wireframes", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 2), Hours = 7.5m, Description = "Homepage layout implementation", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 3), Hours = 6m, Description = "Component library setup", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 4), Hours = 8m, Description = "Responsive styling and QA", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 5), Hours = 4m, Description = "Internal sync and planning", IsBillable = false }
        };

        db.Clients.Add(client);
        db.Projects.AddRange(websiteProject, mobileProject);
        db.TimeEntries.AddRange(entries);

        var lines = new List<InvoiceLine>
        {
            BuildLine(websiteProject, "Website Redesign — billable work", 29.5m, client.HourlyRate)
        };

        db.Invoices.Add(BuildInvoice(
            client,
            "INV-2026-0001",
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 5),
            lines));
    }

    private static void SeedAdventureWorks(TimeTrackerDbContext db)
    {
        var client = new Client
        {
            Name = "Adventure Works",
            Email = "demo@adventure-works.test",
            HourlyRate = 175m,
            IsActive = true
        };

        var azureProject = new Project
        {
            Name = "Azure Integration",
            Description = "Resource provisioning and identity wiring",
            Client = client,
            IsActive = true
        };

        var teamsProject = new Project
        {
            Name = "Teams Bot",
            Description = "Conversational bot for internal support",
            Client = client,
            IsActive = true
        };

        var copilotProject = new Project
        {
            Name = "Copilot Plugin",
            Description = "Custom Copilot plugin for the support portal",
            Client = client,
            IsActive = true
        };

        var entries = new List<TimeEntry>
        {
            new() { Project = azureProject, Date = new DateTime(2026, 6, 8), Hours = 8m, Description = "Subscription and resource group setup", IsBillable = true },
            new() { Project = azureProject, Date = new DateTime(2026, 6, 9), Hours = 6m, Description = "Managed identity and RBAC", IsBillable = true },
            new() { Project = teamsProject, Date = new DateTime(2026, 6, 10), Hours = 7m, Description = "Bot framework scaffolding", IsBillable = true },
            new() { Project = teamsProject, Date = new DateTime(2026, 6, 11), Hours = 5m, Description = "Adaptive card dialogs", IsBillable = true },
            new() { Project = copilotProject, Date = new DateTime(2026, 6, 11), Hours = 3m, Description = "Internal design review", IsBillable = false },
            new() { Project = copilotProject, Date = new DateTime(2026, 6, 12), Hours = 8m, Description = "Plugin manifest and auth flow", IsBillable = true }
        };

        db.Clients.Add(client);
        db.Projects.AddRange(azureProject, teamsProject, copilotProject);
        db.TimeEntries.AddRange(entries);

        var lines = new List<InvoiceLine>
        {
            BuildLine(azureProject, "Azure Integration — billable work", 14m, client.HourlyRate),
            BuildLine(teamsProject, "Teams Bot — billable work", 12m, client.HourlyRate),
            BuildLine(copilotProject, "Copilot Plugin — billable work", 8m, client.HourlyRate)
        };

        db.Invoices.Add(BuildInvoice(
            client,
            "INV-2026-0002",
            new DateTime(2026, 6, 8),
            new DateTime(2026, 6, 12),
            lines));
    }

    private static InvoiceLine BuildLine(Project project, string description, decimal hours, decimal hourlyRate) =>
        new()
        {
            Project = project,
            Description = description,
            Hours = hours,
            HourlyRate = hourlyRate,
            RateType = RateType.Standard,
            LineTotal = hours * hourlyRate
        };

    private static Invoice BuildInvoice(
        Client client,
        string invoiceNumber,
        DateTime periodStart,
        DateTime periodEnd,
        List<InvoiceLine> lines)
    {
        decimal subtotal = lines.Sum(l => l.LineTotal);
        decimal taxAmount = Math.Round(subtotal * (TaxRate / 100m), 2);

        return new Invoice
        {
            Client = client,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Issued,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Subtotal = subtotal,
            DiscountPercent = 0m,
            DiscountAmount = 0m,
            TaxRate = TaxRate,
            TaxAmount = taxAmount,
            Total = subtotal + taxAmount,
            IssuedAt = DateTime.UtcNow,
            InvoiceLines = lines
        };
    }
}
