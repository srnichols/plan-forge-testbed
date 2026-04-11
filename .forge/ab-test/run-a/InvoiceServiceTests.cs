using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeTracker.Api.Data;
using TimeTracker.Api.Services;
using TimeTracker.Core.Models;

namespace TimeTracker.Tests;

public class InvoiceServiceTests : IDisposable
{
    private readonly TimeTrackerDbContext _db;
    private readonly InvoiceService _service;

    private static readonly DateTime PeriodStart = new(2025, 4, 1);
    private static readonly DateTime PeriodEnd = new(2025, 5, 1);

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new InvoiceService(_db);
    }

    #region Helpers

    private async Task<Client> CreateClientAsync(
        decimal hourlyRate = 100m,
        decimal taxRate = 0m,
        bool isActive = true)
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "test@example.com",
            HourlyRate = hourlyRate,
            TaxRate = taxRate,
            IsActive = isActive,
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    private async Task<Project> CreateProjectAsync(int clientId, string name = "Test Project")
    {
        var project = new Project
        {
            Name = name,
            ClientId = clientId,
            IsActive = true,
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    private async Task CreateTimeEntryAsync(int projectId, DateTime date, decimal hours)
    {
        _db.TimeEntries.Add(new TimeEntry
        {
            ProjectId = projectId,
            Date = date,
            Hours = hours,
            IsBillable = true,
            Description = "Test entry",
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates standard-rate entries on weekdays (max 8h/day to avoid overtime).
    /// </summary>
    private async Task CreateWeekdayEntriesAsync(int projectId, int totalHours)
    {
        int remaining = totalHours;
        DateTime date = PeriodStart;
        while (remaining > 0)
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
                continue;
            }
            int hours = Math.Min(remaining, 8);
            _db.TimeEntries.Add(new TimeEntry
            {
                ProjectId = projectId,
                Date = date,
                Hours = hours,
                IsBillable = true,
                Description = "Test entry",
            });
            remaining -= hours;
            date = date.AddDays(1);
        }
        await _db.SaveChangesAsync();
    }

    #endregion

    #region Standard Hours

    [Fact]
    public async Task GenerateInvoice_StandardHoursOnly_CalculatesCorrectTotals()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        // Mon Apr 7 = 6h, Tue Apr 8 = 4h → 10h standard
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 6m);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 8), 4m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Single(invoice.InvoiceLines);
        InvoiceLine line = invoice.InvoiceLines.First();
        Assert.Equal(RateType.Standard, line.RateType);
        Assert.Equal(10m, line.Hours);
        Assert.Equal(100m, line.HourlyRate);
        Assert.Equal(1000m, line.LineTotal);
        Assert.Equal(1000m, invoice.Subtotal);
        Assert.Equal(1000m, invoice.Total);
    }

    #endregion

    #region Overtime

    [Fact]
    public async Task GenerateInvoice_WithOvertime_Applies1_5xRate()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        // Mon Apr 7 = 10h → 8h standard + 2h overtime
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 10m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(2, invoice.InvoiceLines.Count);
        InvoiceLine standard = invoice.InvoiceLines.First(l => l.RateType == RateType.Standard);
        InvoiceLine overtime = invoice.InvoiceLines.First(l => l.RateType == RateType.Overtime);

        Assert.Equal(8m, standard.Hours);
        Assert.Equal(100m, standard.HourlyRate);
        Assert.Equal(800m, standard.LineTotal);

        Assert.Equal(2m, overtime.Hours);
        Assert.Equal(150m, overtime.HourlyRate);
        Assert.Equal(300m, overtime.LineTotal);

        Assert.Equal(1100m, invoice.Subtotal);
    }

    #endregion

    #region Weekend

    [Fact]
    public async Task GenerateInvoice_WeekendEntries_Applies1_5xRate()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        // Sat Apr 5 = 4h, Sun Apr 6 = 4h → 8h weekend
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 5), 4m);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 6), 4m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Single(invoice.InvoiceLines);
        InvoiceLine weekend = invoice.InvoiceLines.First();
        Assert.Equal(RateType.Weekend, weekend.RateType);
        Assert.Equal(8m, weekend.Hours);
        Assert.Equal(150m, weekend.HourlyRate);
        Assert.Equal(1200m, weekend.LineTotal);
    }

    #endregion

    #region Volume Discounts

    public static IEnumerable<object[]> DiscountBracketData =>
        new List<object[]>
        {
            new object[] { 39, 0.00m },
            new object[] { 41, 0.05m },
            new object[] { 81, 0.10m },
            new object[] { 161, 0.15m },
        };

    [Theory]
    [MemberData(nameof(DiscountBracketData))]
    public async Task GenerateInvoice_VolumeDiscount_CorrectBracket(int totalHours, decimal expectedDiscountPercent)
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateWeekdayEntriesAsync(project.Id, totalHours);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(expectedDiscountPercent, invoice.DiscountPercent);
    }

    #endregion

    #region Tax Calculation

    [Fact]
    public async Task GenerateInvoice_WithTaxRate_CalculatesTaxCorrectly()
    {
        var client = await CreateClientAsync(hourlyRate: 100m, taxRate: 0.13m);
        var project = await CreateProjectAsync(client.Id);
        // 8h + 2h = 10h standard → subtotal $1,000
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 8m);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 8), 2m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(1000m, invoice.Subtotal);
        Assert.Equal(0m, invoice.DiscountPercent);
        Assert.Equal(0m, invoice.DiscountAmount);
        Assert.Equal(0.13m, invoice.TaxRate);
        Assert.Equal(130m, invoice.TaxAmount);
        Assert.Equal(1130m, invoice.Total);
    }

    [Fact]
    public async Task GenerateInvoice_TaxExemptClient_ZeroTaxAmount()
    {
        var client = await CreateClientAsync(hourlyRate: 100m, taxRate: 0m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(0m, invoice.TaxRate);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(invoice.Subtotal - invoice.DiscountAmount, invoice.Total);
    }

    #endregion

    #region Validation Errors

    [Fact]
    public async Task GenerateInvoice_EmptyPeriod_ThrowsValidationException()
    {
        var client = await CreateClientAsync();
        await CreateProjectAsync(client.Id);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd));
    }

    [Fact]
    public async Task GenerateInvoice_InactiveClient_ThrowsValidationException()
    {
        var client = await CreateClientAsync(isActive: false);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd));
    }

    [Fact]
    public async Task GenerateInvoice_OverlappingPeriod_ThrowsValidationException()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);
        await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd));
    }

    #endregion

    #region Status Transitions

    [Fact]
    public async Task StatusTransitions_DraftToIssuedToPaid_Succeeds()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);
        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);

        var issued = await _service.IssueInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Issued, issued.Status);
        Assert.NotNull(issued.IssuedAt);

        var paid = await _service.MarkPaidAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Paid, paid.Status);
        Assert.NotNull(paid.PaidAt);
    }

    [Fact]
    public async Task MarkPaid_DraftInvoice_ThrowsInvalidOperationException()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);
        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.MarkPaidAsync(invoice.Id));
    }

    [Fact]
    public async Task IssueInvoice_VoidedInvoice_ThrowsInvalidOperationException()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);
        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);
        await _service.VoidInvoiceAsync(invoice.Id, "Testing void");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.IssueInvoiceAsync(invoice.Id));
    }

    #endregion

    #region Invoice Number Format

    [Fact]
    public async Task GenerateInvoice_InvoiceNumber_FollowsExpectedFormat()
    {
        var client = await CreateClientAsync(hourlyRate: 100m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 5m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        string expected = $"INV-{client.Id:D4}-202504-001";
        Assert.Equal(expected, invoice.InvoiceNumber);
    }

    #endregion

    #region Banker's Rounding

    [Fact]
    public async Task GenerateInvoice_BankersRounding_HalfToEvenDown()
    {
        // subtotal=1001.25 * taxRate=0.10 → raw=100.125 → rounds to 100.12
        var client = await CreateClientAsync(hourlyRate: 1001.25m, taxRate: 0.10m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 1m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(100.12m, invoice.TaxAmount);
    }

    [Fact]
    public async Task GenerateInvoice_BankersRounding_HalfToEvenUp()
    {
        // subtotal=1001.35 * taxRate=0.10 → raw=100.135 → rounds to 100.14
        var client = await CreateClientAsync(hourlyRate: 1001.35m, taxRate: 0.10m);
        var project = await CreateProjectAsync(client.Id);
        await CreateTimeEntryAsync(project.Id, new DateTime(2025, 4, 7), 1m);

        var invoice = await _service.GenerateInvoiceAsync(client.Id, PeriodStart, PeriodEnd);

        Assert.Equal(100.14m, invoice.TaxAmount);
    }

    #endregion

    public void Dispose() => _db.Dispose();
}
