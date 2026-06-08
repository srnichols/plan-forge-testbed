using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Data;

/// <summary>
/// Seeds demonstration data (one client, two projects, one work week of time
/// entries) when the database has no clients. Idempotent: running it against a
/// populated database is a no-op, so it is safe to call on every startup.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(TimeTrackerDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Clients.AnyAsync(cancellationToken))
        {
            return;
        }

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

        var timeEntries = new List<TimeEntry>
        {
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 1), Hours = 8m, Description = "Discovery and wireframes", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 2), Hours = 7.5m, Description = "Homepage layout implementation", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 3), Hours = 6m, Description = "Component library setup", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 4), Hours = 8m, Description = "Responsive styling and QA", IsBillable = true },
            new() { Project = websiteProject, Date = new DateTime(2026, 6, 5), Hours = 4m, Description = "Internal sync and planning", IsBillable = false }
        };

        db.Clients.Add(client);
        db.Projects.AddRange(websiteProject, mobileProject);
        db.TimeEntries.AddRange(timeEntries);

        await db.SaveChangesAsync(cancellationToken);
    }
}
