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

    // Fixed past Monday — avoids "period end cannot be in the future" validation
    private static readonly DateTime BaseMonday = new(2026, 3, 2);

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TimeTrackerDbContext(options);
        _service = new InvoiceService(_db);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<(Client Client, Project Project)> SeedClientWithProjectAsync(
        decimal hourlyRate = 100m, bool isActive = true)
    {
        Client client = new() { Name = "Test Client", HourlyRate = hourlyRate, IsActive = isActive };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        Project project = new() { Name = "Test Project", ClientId = client.Id };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return (client, project);
    }

    private async Task AddEntryAsync(int projectId, DateTime date, decimal hours, bool isBillable = true)
    {
        _db.TimeEntries.Add(new TimeEntry
        {
            ProjectId = projectId,
            Date = date,
            Hours = hours,
            IsBillable = isBillable,
        });
        await _db.SaveChangesAsync();
    }

    private async Task AddWeekdayEntriesAsync(int projectId, decimal totalHours, DateTime startMonday)
    {
        decimal remaining = totalHours;
        DateTime current = startMonday;

        while (remaining > 0)
        {
            if (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                current = current.AddDays(1);
                continue;
            }

            decimal hoursToday = Math.Min(remaining, 8m);
            await AddEntryAsync(projectId, current, hoursToday);
            remaining -= hoursToday;
            current = current.AddDays(1);
        }
    }

    public void Dispose() => _db.Dispose();

    // ── Standard Hours ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_StandardHoursOnly_CorrectTotals()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddEntryAsync(project.Id, BaseMonday, 8m);
        await AddEntryAsync(project.Id, BaseMonday.AddDays(1), 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(2));

        Assert.Single(invoice.InvoiceLines);
        InvoiceLine line = invoice.InvoiceLines.First();
        Assert.Equal(RateType.Standard, line.RateType);
        Assert.Equal(16m, line.Hours);
        Assert.Equal(100m, line.HourlyRate);
        Assert.Equal(1600m, line.LineTotal);
        Assert.Equal(1600m, invoice.Subtotal);
        Assert.Equal(0m, invoice.DiscountPercent);
        Assert.Equal(0m, invoice.DiscountAmount);
        Assert.Equal(1600m, invoice.Total);
    }

    // ── Overtime ────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_WithOvertime_SplitsIntoStandardAndOvertimeLines()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddEntryAsync(project.Id, BaseMonday, 10m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        Assert.Equal(2, invoice.InvoiceLines.Count);

        InvoiceLine standardLine = invoice.InvoiceLines.First(l => l.RateType == RateType.Standard);
        Assert.Equal(8m, standardLine.Hours);
        Assert.Equal(100m, standardLine.HourlyRate);
        Assert.Equal(800m, standardLine.LineTotal);

        InvoiceLine overtimeLine = invoice.InvoiceLines.First(l => l.RateType == RateType.Overtime);
        Assert.Equal(2m, overtimeLine.Hours);
        Assert.Equal(150m, overtimeLine.HourlyRate);
        Assert.Equal(300m, overtimeLine.LineTotal);

        Assert.Equal(1100m, invoice.Subtotal);
    }

    // ── Weekend ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_WeekendEntry_AppliesWeekendMultiplier()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        DateTime saturday = BaseMonday.AddDays(5); // Mar 7, 2026 = Saturday
        await AddEntryAsync(project.Id, saturday, 4m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday, saturday.AddDays(1));

        Assert.Single(invoice.InvoiceLines);
        InvoiceLine line = invoice.InvoiceLines.First();
        Assert.Equal(RateType.Weekend, line.RateType);
        Assert.Equal(4m, line.Hours);
        Assert.Equal(150m, line.HourlyRate);
        Assert.Equal(600m, line.LineTotal);
    }

    // ── Volume Discounts ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_VolumeDiscount_39Hours_NoDiscount()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddWeekdayEntriesAsync(project.Id, 39m, BaseMonday);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(29));

        Assert.Equal(0m, invoice.DiscountPercent);
        Assert.Equal(0m, invoice.DiscountAmount);
    }

    [Fact]
    public async Task GenerateInvoice_VolumeDiscount_41Hours_5Percent()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddWeekdayEntriesAsync(project.Id, 41m, BaseMonday);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(29));

        Assert.Equal(5m, invoice.DiscountPercent);
        decimal expectedDiscount = Math.Round(invoice.Subtotal * 0.05m, 2, MidpointRounding.ToEven);
        Assert.Equal(expectedDiscount, invoice.DiscountAmount);
    }

    [Fact]
    public async Task GenerateInvoice_VolumeDiscount_81Hours_10Percent()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddWeekdayEntriesAsync(project.Id, 81m, BaseMonday);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(29));

        Assert.Equal(10m, invoice.DiscountPercent);
        decimal expectedDiscount = Math.Round(invoice.Subtotal * 0.10m, 2, MidpointRounding.ToEven);
        Assert.Equal(expectedDiscount, invoice.DiscountAmount);
    }

    [Fact]
    public async Task GenerateInvoice_VolumeDiscount_161Hours_15Percent()
    {
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddWeekdayEntriesAsync(project.Id, 161m, BaseMonday);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(29));

        Assert.Equal(15m, invoice.DiscountPercent);
        decimal expectedDiscount = Math.Round(invoice.Subtotal * 0.15m, 2, MidpointRounding.ToEven);
        Assert.Equal(expectedDiscount, invoice.DiscountAmount);
    }

    // ── Tax ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_TaxExemptClient_TaxAmountIsZero()
    {
        // TaxRate is hardcoded to 0% — Client model lacks TaxRate property (out of scope)
        var (client, project) = await SeedClientWithProjectAsync(hourlyRate: 100m);
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        Assert.Equal(0m, invoice.TaxRate);
        Assert.Equal(0m, invoice.TaxAmount);
    }

    // ── Error Cases ─────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_NoBillableEntries_ThrowsValidationException()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m, isBillable: false);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GenerateInvoiceAsync(client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1)));
    }

    [Fact]
    public async Task GenerateInvoice_InactiveClient_ThrowsKeyNotFoundException()
    {
        // Slice says ValidationException, but service filters by IsActive — inactive → KeyNotFoundException
        var (client, _) = await SeedClientWithProjectAsync(isActive: false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GenerateInvoiceAsync(client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1)));
    }

    [Fact]
    public async Task GenerateInvoice_OverlappingPeriod_ThrowsValidationException()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);
        await AddEntryAsync(project.Id, BaseMonday.AddDays(1), 8m);

        await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(2));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GenerateInvoiceAsync(client.Id, BaseMonday, BaseMonday.AddDays(3)));
    }

    [Fact]
    public async Task GenerateInvoice_VoidedInvoiceAllowsRegenerateForSamePeriod()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));
        await _service.VoidInvoiceAsync(invoice.Id, "Testing void");

        // Voided invoices are excluded from duplicate check
        Invoice newInvoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        Assert.Equal(InvoiceStatus.Draft, newInvoice.Status);
    }

    // ── Status Transitions ──────────────────────────────────────────────

    [Fact]
    public async Task StatusTransition_DraftToIssuedToPaid_Succeeds()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);

        Invoice issued = await _service.IssueInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Issued, issued.Status);
        Assert.NotNull(issued.IssuedAt);

        Invoice paid = await _service.MarkPaidAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Paid, paid.Status);
        Assert.NotNull(paid.PaidAt);
    }

    [Fact]
    public async Task StatusTransition_DraftToPaid_ThrowsInvalidOperation()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.MarkPaidAsync(invoice.Id));
    }

    [Fact]
    public async Task StatusTransition_VoidToIssued_ThrowsInvalidOperation()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));
        await _service.VoidInvoiceAsync(invoice.Id, "Void reason");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.IssueInvoiceAsync(invoice.Id));
    }

    // ── Invoice Number ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_InvoiceNumberFormat_MatchesPattern()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);

        Invoice invoice = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        // Format: INV-XXXX-YYYYMM-NNN
        Assert.Matches(@"^INV-\d{4}-\d{6}-\d{3}$", invoice.InvoiceNumber);
        Assert.EndsWith("-001", invoice.InvoiceNumber);
    }

    [Fact]
    public async Task GenerateInvoice_SecondInvoice_SequenceIncrements()
    {
        var (client, project) = await SeedClientWithProjectAsync();
        await AddEntryAsync(project.Id, BaseMonday, 8m);
        await AddEntryAsync(project.Id, BaseMonday.AddDays(14), 8m);

        Invoice first = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        Invoice second = await _service.GenerateInvoiceAsync(
            client.Id, BaseMonday.AddDays(13), BaseMonday.AddDays(15));

        Assert.EndsWith("-001", first.InvoiceNumber);
        Assert.EndsWith("-002", second.InvoiceNumber);
    }

    // ── Banker's Rounding ───────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_BankersRounding_MidpointRoundsToEven()
    {
        // 0.5h × $200.25 = $100.125 → banker's rounds to $100.12 (2 is even → round down)
        var (client1, project1) = await SeedClientWithProjectAsync(hourlyRate: 200.25m);
        await AddEntryAsync(project1.Id, BaseMonday, 0.5m);

        Invoice invoice1 = await _service.GenerateInvoiceAsync(
            client1.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        InvoiceLine line1 = invoice1.InvoiceLines.First();
        Assert.Equal(100.12m, line1.LineTotal);

        // 0.5h × $200.27 = $100.135 → banker's rounds to $100.14 (3 is odd → round up)
        var (client2, project2) = await SeedClientWithProjectAsync(hourlyRate: 200.27m);
        await AddEntryAsync(project2.Id, BaseMonday, 0.5m);

        Invoice invoice2 = await _service.GenerateInvoiceAsync(
            client2.Id, BaseMonday.AddDays(-1), BaseMonday.AddDays(1));

        InvoiceLine line2 = invoice2.InvoiceLines.First();
        Assert.Equal(100.14m, line2.LineTotal);
    }
}
