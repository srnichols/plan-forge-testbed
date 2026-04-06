# Phase 3: Invoice Generation Engine — Rate Tiers, Discounts, Tax, Line Items

> **Roadmap Reference**: [DEPLOYMENT-ROADMAP.md](./DEPLOYMENT-ROADMAP.md) → Phase 3
> **Status**: ✅ Hardened
> **Purpose**: A/B test — Quorum Mode vs Standard Execution
> **Pipeline**: Step 0 ✅ → Step 2 ✅ (hardened)

---

## Specification (Step 0)

### Problem Statement

The TimeTracker app can track time and produce simple billing summaries, but has no invoice generation. Clients need detailed invoices with line items per project, hourly rate tiers (standard vs overtime vs weekend), volume discounts, tax calculation, and structured output. The billing math must be precise (banker's rounding), handle edge cases (zero hours, inactive projects, overlapping date ranges), and produce a complete invoice model that a PDF renderer or API consumer can use directly.

### Acceptance Criteria

- **MUST**: `Invoice` entity with line items, subtotals, discount, tax, and grand total
- **MUST**: `InvoiceService` with rate tier calculation (standard ≤8h/day, overtime >8h, weekend 1.5x)
- **MUST**: Volume discount rules: >40h = 5% off, >80h = 10% off, >160h = 15% off
- **MUST**: Tax calculation with configurable rate per client (default 0%, some clients tax-exempt)
- **MUST**: Banker's rounding (`MidpointRounding.ToEven`) on all monetary calculations
- **MUST**: `InvoicesController` with generate, get-by-id, list-by-client, void endpoints
- **MUST**: Prevent duplicate invoices for same client + date range
- **MUST**: Invoice status lifecycle: Draft → Issued → Paid → Void
- **MUST**: Comprehensive unit tests covering all rate tiers, discount brackets, tax, edge cases
- **MUST**: Validation — date range must be valid, client must exist and be active, must have billable entries

---

## Scope Contract

### In Scope
- `src/TimeTracker.Core/Models/Invoice.cs` — Invoice + InvoiceLine entities
- `src/TimeTracker.Api/Services/InvoiceService.cs` — generation logic with rate tiers, discounts, tax
- `src/TimeTracker.Api/Controllers/InvoicesController.cs` — HTTP endpoints
- `src/TimeTracker.Api/Data/TimeTrackerDbContext.cs` — add Invoice + InvoiceLine DbSets
- `tests/TimeTracker.Tests/InvoiceServiceTests.cs` — comprehensive unit tests

### Out of Scope
- PDF rendering (future phase)
- Email delivery
- Payment processing
- Modifying existing Client/Project/TimeEntry entities

### Forbidden Actions
- Do NOT modify ClientService, ProjectService, or BillingService
- Do NOT modify existing test files
- Do NOT change the database connection or provider
- Do NOT add new NuGet packages

---

## Execution Slices

### Slice 1: Invoice + InvoiceLine entities and DbContext update [scope: src/TimeTracker.Core/Models/**, src/TimeTracker.Api/Data/**]

**Build command**: `dotnet build src/TimeTracker.Api`
**Test command**: `dotnet test tests/TimeTracker.Tests`

**Tasks**:
1. Create `Invoice` model: Id, ClientId (FK→Client), InvoiceNumber (unique string), Status (enum: Draft/Issued/Paid/Void), PeriodStart, PeriodEnd, Subtotal, DiscountPercent, DiscountAmount, TaxRate, TaxAmount, Total, CreatedAt, IssuedAt?, PaidAt?, VoidedAt?, VoidReason?
2. Create `InvoiceLine` model: Id, InvoiceId (FK→Invoice), ProjectId (FK→Project), Description, Hours, HourlyRate, RateType (enum: Standard/Overtime/Weekend), LineTotal
3. Create `InvoiceStatus` enum: Draft=0, Issued=1, Paid=2, Void=3
4. Create `RateType` enum: Standard=0, Overtime=1, Weekend=2
5. Add `DbSet<Invoice>` and `DbSet<InvoiceLine>` to TimeTrackerDbContext
6. Configure fluent API: Invoice.Total decimal(18,2), InvoiceLine.LineTotal decimal(18,2), InvoiceLine.HourlyRate decimal(10,2), InvoiceLine.Hours decimal(5,2), unique index on InvoiceNumber, cascade delete Invoice→InvoiceLines

**Validation Gate**:
```
dotnet build src/TimeTracker.Api
dotnet test tests/TimeTracker.Tests
```

### Slice 2: InvoiceService — rate tiers, discounts, tax calculation [depends: Slice 1] [scope: src/TimeTracker.Api/Services/**]

**Build command**: `dotnet build src/TimeTracker.Api`
**Test command**: `dotnet test tests/TimeTracker.Tests`

**Tasks**:
1. Create `IInvoiceService` interface with: GenerateInvoiceAsync(clientId, periodStart, periodEnd), GetInvoiceAsync(id), GetClientInvoicesAsync(clientId), IssueInvoiceAsync(id), MarkPaidAsync(id), VoidInvoiceAsync(id, reason)
2. Implement `InvoiceService` with constructor DI for TimeTrackerDbContext
3. GenerateInvoiceAsync logic:
   a. Validate client exists and is active
   b. Validate date range (start < end, not in future)
   c. Check for duplicate invoice (same client + overlapping period)
   d. Query billable TimeEntries for client within period, include Project
   e. Group entries by project, then by date to calculate daily hours
   f. Rate tier logic per day per project: first 8h = standard rate, hours >8h = overtime (1.5x client rate), weekend entries (Saturday/Sunday) = weekend rate (1.5x client rate)
   g. Build InvoiceLine per project with aggregated hours by rate type
   h. Calculate subtotal (sum of line totals)
   i. Volume discount: total billable hours >160h=15%, >80h=10%, >40h=5%, else 0%
   j. Apply discount: discountAmount = Round(subtotal * discountPercent, 2, MidpointRounding.ToEven)
   k. Tax: taxAmount = Round((subtotal - discountAmount) * client.TaxRate, 2, MidpointRounding.ToEven) — add TaxRate property to Client model (decimal, default 0)
   l. Total = subtotal - discountAmount + taxAmount
   m. Generate InvoiceNumber: "INV-{clientId:D4}-{yyyyMM}-{sequence:D3}"
   n. Save Invoice with status Draft, return the complete invoice
4. Status transitions: IssueInvoiceAsync sets IssuedAt, MarkPaidAsync sets PaidAt, VoidInvoiceAsync sets VoidedAt + VoidReason — all with status validation (can't pay a draft, can't void a paid invoice, etc.)
5. Register IInvoiceService in Program.cs DI container

**Validation Gate**:
```
dotnet build src/TimeTracker.Api
dotnet test tests/TimeTracker.Tests
```

### Slice 3: InvoiceServiceTests — comprehensive unit tests [depends: Slice 2] [scope: tests/**]

**Build command**: `dotnet build`
**Test command**: `dotnet test tests/TimeTracker.Tests`

**Tasks**:
1. Create InvoiceServiceTests class with in-memory EF Core database (same pattern as ClientServiceTests)
2. Test: Generate invoice for client with standard hours only — verify line totals, subtotal, total
3. Test: Generate invoice with overtime (>8h in one day) — verify 1.5x rate applied to excess hours
4. Test: Generate invoice with weekend entries — verify 1.5x rate on Saturday/Sunday
5. Test: Volume discount at each bracket: 41h (5%), 81h (10%), 161h (15%), 39h (0%)
6. Test: Tax calculation with non-zero rate — verify banker's rounding
7. Test: Tax-exempt client (rate=0) — verify taxAmount=0
8. Test: Empty period (no billable entries) — should throw ValidationException
9. Test: Inactive client — should throw ValidationException
10. Test: Duplicate invoice detection — same client + overlapping period should throw
11. Test: Status transitions — Draft→Issued→Paid (happy path)
12. Test: Invalid transitions — Draft→Paid should throw, Void→Issued should throw
13. Test: Invoice number format — "INV-0001-202604-001" pattern
14. Test: Banker's rounding edge case — $100.125 rounds to $100.12, $100.135 rounds to $100.14

**Validation Gate**:
```
dotnet build
dotnet test tests/TimeTracker.Tests
```

### Slice 4: InvoicesController — HTTP endpoints [depends: Slice 2, Slice 3] [scope: src/TimeTracker.Api/Controllers/**]

**Build command**: `dotnet build src/TimeTracker.Api`
**Test command**: `dotnet test tests/TimeTracker.Tests`

**Tasks**:
1. Create InvoicesController with constructor DI for IInvoiceService
2. POST /api/invoices/generate — body: { clientId, periodStart, periodEnd } → returns 201 with invoice
3. GET /api/invoices/{id} — returns invoice with line items
4. GET /api/invoices?clientId={id} — returns list of invoices for client
5. POST /api/invoices/{id}/issue — transition to Issued status
6. POST /api/invoices/{id}/pay — transition to Paid status
7. POST /api/invoices/{id}/void — body: { reason } → transition to Void status
8. Error handling: ValidationException→400, KeyNotFoundException→404, InvalidOperationException→409

**Validation Gate**:
```
dotnet build src/TimeTracker.Api
dotnet test tests/TimeTracker.Tests
```

---

## Definition of Done
- [ ] Invoice + InvoiceLine entities with all properties
- [ ] Rate tier calculation: standard, overtime (>8h/day), weekend (1.5x)
- [ ] Volume discounts at 4 brackets
- [ ] Tax calculation with banker's rounding
- [ ] Duplicate invoice prevention
- [ ] Status lifecycle: Draft → Issued → Paid → Void
- [ ] InvoicesController with 6 endpoints
- [ ] 14+ unit tests covering all tiers, discounts, tax, edge cases, transitions
- [ ] All existing tests still pass (zero regressions)
- [ ] `dotnet build` + `dotnet test` pass at every slice boundary

## Stop Conditions
- If Client model modification (adding TaxRate) breaks existing tests → fix client tests first
- If in-memory database doesn't support the query patterns → simplify query (no raw SQL)
