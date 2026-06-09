using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(TimeTrackerDbContext db, CancellationToken ct = default)
    {
        if (await db.Clients.AnyAsync(ct)) return;

        var (contoso, contosoInvoice) = BuildContosoClient();
        var (adventure, adventureInvoice) = BuildAdventureWorksClient();

        db.Clients.Add(contoso);
        db.Clients.Add(adventure);
        db.Invoices.Add(contosoInvoice);
        db.Invoices.Add(adventureInvoice);

        await db.SaveChangesAsync(ct);
    }

    private static (Client Client, Invoice Invoice) BuildContosoClient()
    {
        var client = new Client
        {
            Name = "Contoso Ltd",
            Email = "billing@contoso.com",
            HourlyRate = 150m,
            TaxRate = 8.5m,
            IsActive = true
        };

        var project = new Project
        {
            Name = "Website Redesign",
            Description = "Marketing site refresh and CMS migration",
            Client = client,
            IsActive = true
        };
        project.TimeEntries.Add(new TimeEntry
        {
            Project = project,
            Date = DateTime.UtcNow.Date.AddDays(-10),
            Hours = 6.0m,
            Description = "Discovery workshop and IA outline",
            IsBillable = true
        });
        project.TimeEntries.Add(new TimeEntry
        {
            Project = project,
            Date = DateTime.UtcNow.Date.AddDays(-7),
            Hours = 4.5m,
            Description = "Homepage wireframes",
            IsBillable = true
        });
        client.Projects.Add(project);

        var (periodStart, periodEnd) = PriorMonthRange();
        var lines = new List<InvoiceLine>
        {
            BuildLine(project, "Discovery and IA", 6.0m, 150m),
            BuildLine(project, "Wireframes and prototypes", 4.5m, 150m)
        };
        var invoice = BuildInvoice(client, "INV-2026-0001", periodStart, periodEnd, lines);

        return (client, invoice);
    }

    private static (Client Client, Invoice Invoice) BuildAdventureWorksClient()
    {
        var client = new Client
        {
            Name = "Adventure Works",
            Email = "ap@adventure-works.com",
            HourlyRate = 175m,
            TaxRate = 8.5m,
            IsActive = true
        };

        var mobileApp = new Project
        {
            Name = "Mobile App",
            Description = "iOS/Android companion app",
            Client = client,
            IsActive = true
        };
        mobileApp.TimeEntries.Add(new TimeEntry
        {
            Project = mobileApp,
            Date = DateTime.UtcNow.Date.AddDays(-12),
            Hours = 8.0m,
            Description = "Auth flow implementation",
            IsBillable = true
        });

        var apiPlatform = new Project
        {
            Name = "API Platform",
            Description = "Public API gateway and SDKs",
            Client = client,
            IsActive = true
        };
        apiPlatform.TimeEntries.Add(new TimeEntry
        {
            Project = apiPlatform,
            Date = DateTime.UtcNow.Date.AddDays(-9),
            Hours = 5.5m,
            Description = "Rate limiter design",
            IsBillable = true
        });
        apiPlatform.TimeEntries.Add(new TimeEntry
        {
            Project = apiPlatform,
            Date = DateTime.UtcNow.Date.AddDays(-5),
            Hours = 3.25m,
            Description = "OpenAPI schema cleanup",
            IsBillable = true
        });

        var dataWarehouse = new Project
        {
            Name = "Data Warehouse",
            Description = "Analytics consolidation",
            Client = client,
            IsActive = true
        };
        dataWarehouse.TimeEntries.Add(new TimeEntry
        {
            Project = dataWarehouse,
            Date = DateTime.UtcNow.Date.AddDays(-3),
            Hours = 7.0m,
            Description = "ETL pipeline tuning",
            IsBillable = true
        });

        client.Projects.Add(mobileApp);
        client.Projects.Add(apiPlatform);
        client.Projects.Add(dataWarehouse);

        var (periodStart, periodEnd) = PriorMonthRange();
        var lines = new List<InvoiceLine>
        {
            BuildLine(mobileApp, "Auth flow implementation", 8.0m, 175m),
            BuildLine(apiPlatform, "Rate limiter and OpenAPI", 8.75m, 175m),
            BuildLine(dataWarehouse, "ETL pipeline tuning", 7.0m, 175m)
        };
        var invoice = BuildInvoice(client, "INV-2026-0002", periodStart, periodEnd, lines);

        return (client, invoice);
    }

    private static InvoiceLine BuildLine(Project project, string description, decimal hours, decimal hourlyRate)
    {
        return new InvoiceLine
        {
            Project = project,
            Description = description,
            Hours = hours,
            HourlyRate = hourlyRate,
            RateType = RateType.Standard,
            LineTotal = decimal.Round(hours * hourlyRate, 2, MidpointRounding.ToEven)
        };
    }

    private static Invoice BuildInvoice(Client client, string invoiceNumber,
                                         DateTime periodStart, DateTime periodEnd,
                                         IReadOnlyList<InvoiceLine> lines)
    {
        var subtotal = lines.Sum(l => l.LineTotal);
        var taxAmount = decimal.Round(subtotal * 0.085m, 2, MidpointRounding.ToEven);

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
            // stored as percent; calc uses 0.085m fraction
            TaxRate = 8.5m,
            TaxAmount = taxAmount,
            Total = subtotal + taxAmount,
            IssuedAt = DateTime.UtcNow,
            InvoiceLines = lines.ToList()
        };
    }

    private static (DateTime Start, DateTime End) PriorMonthRange()
    {
        var today = DateTime.UtcNow.Date;
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = firstOfThisMonth.AddMonths(-1);
        var end = firstOfThisMonth.AddDays(-1);
        return (start, end);
    }
}
