using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Models;

namespace TimeTracker.Api.Data;

public class TimeTrackerDbContext : DbContext
{
    public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.HourlyRate).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.HasOne(p => p.Client).WithMany(c => c.Projects).HasForeignKey(p => p.ClientId);
        });

        modelBuilder.Entity<TimeEntry>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Hours).HasPrecision(5, 2);
            e.HasOne(t => t.Project).WithMany(p => p.TimeEntries).HasForeignKey(t => t.ProjectId);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.Subtotal).HasPrecision(18, 2);
            e.Property(i => i.DiscountPercent).HasPrecision(5, 2);
            e.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            e.Property(i => i.TaxRate).HasPrecision(5, 2);
            e.Property(i => i.TaxAmount).HasPrecision(18, 2);
            e.Property(i => i.Total).HasPrecision(18, 2);
            e.HasOne(i => i.Client).WithMany().HasForeignKey(i => i.ClientId);
            e.HasMany(i => i.InvoiceLines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Description).HasMaxLength(500);
            e.Property(l => l.Hours).HasPrecision(5, 2);
            e.Property(l => l.HourlyRate).HasPrecision(10, 2);
            e.Property(l => l.LineTotal).HasPrecision(18, 2);
            e.HasOne(l => l.Project).WithMany().HasForeignKey(l => l.ProjectId);
        });
    }
}
