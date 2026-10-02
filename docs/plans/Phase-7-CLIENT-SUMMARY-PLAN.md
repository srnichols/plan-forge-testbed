# Phase 7: Client Activity Summary Endpoint

> **Pipeline Step**: 2 — Hardened
> **Status**: Planned
> **Author**: AI Agent (Step 0 — Specifier, Step 2 — Hardener)
> **Created**: 2026-06-16
> **Hardened**: 2026-06-16
> **Demo note**: Intended for **repeatable dry-run demos** — see [../PHASE-7-DRYRUN-DEMO.md](../PHASE-7-DRYRUN-DEMO.md). Keep this plan **unmarked-complete** (no ✅) so dry-runs show live slice flow instead of a skipped no-op.

---

## Scope Contract

### In Scope
- `GET /api/clients/{id}/summary` endpoint returning per-client aggregate metrics
- `ClientActivitySummary` response record in `TimeTracker.Core.Models`
- `IClientSummaryService` / `ClientSummaryService` in `TimeTracker.Api.Services`
- `ClientSummaryController` in `TimeTracker.Api.Controllers`
- `ClientSummaryServiceTests` in `TimeTracker.Tests`
- DI registration in `Program.cs`

### Out of Scope — DO NOT TOUCH
- Existing controllers, services, models, or tests
- Database schema / migrations
- Authentication / authorization
- Caching infrastructure
- Docker / deployment files

### Forbidden Actions
- Do NOT modify any existing `*Controller.cs`, `*Service.cs`, or `*Tests.cs` files
- Do NOT modify `ClientsController.cs` — add a new controller instead
- Do NOT add NuGet packages
- Do NOT modify `TimeTrackerDbContext.cs`
- Do NOT modify `appsettings.json` or `launchSettings.json`

### Files Created (Exhaustive)
| File | Layer |
|------|-------|
| `src/TimeTracker.Core/Models/ClientActivitySummary.cs` | Model (DTO) |
| `src/TimeTracker.Api/Services/IClientSummaryService.cs` | Service interface |
| `src/TimeTracker.Api/Services/ClientSummaryService.cs` | Service implementation |
| `src/TimeTracker.Api/Controllers/ClientSummaryController.cs` | Controller |
| `tests/TimeTracker.Tests/ClientSummaryServiceTests.cs` | Tests |

### Files Modified (Exhaustive)
| File | Change |
|------|--------|
| `src/TimeTracker.Api/Program.cs` | Add `AddScoped<IClientSummaryService, ClientSummaryService>()` — ONE line |

---

## Specification

### Problem Statement
The TimeTracker API exposes a portfolio-wide dashboard (`GET /api/dashboard`) but no equivalent rollup scoped to a **single client**. To render a client detail page, a consumer must call several endpoints and aggregate client-specific figures by hand. A single per-client summary endpoint removes that work.

### User Scenarios
1. **As an API consumer**, I want to call `GET /api/clients/{id}/summary` and receive that client's aggregate counts and totals in one request so I can render a client detail header.
2. **As an account manager**, I want to see one client's billable vs. non-billable hours so I can gauge their utilization.
3. **As a finance user**, I want to see one client's total outstanding (unpaid) invoice value at a glance.

### Acceptance Criteria
- [ ] `GET /api/clients/{id}/summary` returns 200 with a `ClientActivitySummary` response for an existing client
- [ ] Returns 404 (ProblemDetails) when the client does not exist
- [ ] Response includes: `clientId`, `clientName`, `projectCount`, `totalHours`, `billableHours`, `nonBillableHours`, `invoiceCount`, `outstandingTotal`
- [ ] Only active projects counted in `projectCount`
- [ ] `outstandingTotal` sums `Total` of that client's invoices where `Status` is `Draft` or `Issued`
- [ ] Returns zero values (not 500) when the client exists but has no projects/entries/invoices
- [ ] CancellationToken propagated through all layers
- [ ] Unit tests cover: happy path, client-not-found, client with no activity, mixed billable/non-billable

### Edge Cases
- Client exists, no activity → all numeric fields `0`, 200 OK
- Client not found → 404 ProblemDetails, no 500
- All projects inactive → `projectCount` = 0, but hours still counted if entries exist

### Out of Scope
- Date range filtering (future enhancement)
- Per-user breakdown (no user model yet)
- Caching (can add later with `IDistributedCache`)
- Authentication/authorization (not yet in the project)

### Open Questions
_None — all requirements are clear for this validation feature._

---

## Technical Approach

### Architecture (4-Layer)

| Layer | File | Responsibility |
|-------|------|----------------|
| **Model** | `ClientActivitySummary.cs` | Response DTO (record) |
| **Repository/Data** | Via `TimeTrackerDbContext` | Aggregate queries scoped to one client |
| **Service** | `IClientSummaryService` / `ClientSummaryService` | Business logic — assemble per-client summary, signal not-found |
| **Controller** | `ClientSummaryController` | HTTP handling only — map not-found to 404 |
| **Tests** | `ClientSummaryServiceTests.cs` | Unit tests for service |

### Response Shape

```json
{
  "clientId": 1,
  "clientName": "Contoso Ltd",
  "projectCount": 2,
  "totalHours": 41.5,
  "billableHours": 29.5,
  "nonBillableHours": 12.0,
  "invoiceCount": 1,
  "outstandingTotal": 4799.81
}
```

### Implementation Notes
- **Npgsql / DateTime**: if any date literals are constructed, use `DateTimeKind.Utc` — Npgsql rejects `DateTime` with `Kind = Unspecified` against `timestamp with time zone` columns.
- Follow existing patterns: primary constructors, `async`/`await`, `CancellationToken` on every async method, `[ApiController]`, try-catch returning ProblemDetails.

---

## Execution Slices

### Slice 1: Model + Service Interface + Tests (TDD Red) [scope: src/TimeTracker.Core/Models/ClientActivitySummary.cs, src/TimeTracker.Api/Services/IClientSummaryService.cs, tests/TimeTracker.Tests/ClientSummaryServiceTests.cs]

**Goal**: Define the response shape and the service contract, and write the failing tests that pin the acceptance criteria.

**Tasks**:
1. Create `src/TimeTracker.Core/Models/ClientActivitySummary.cs`: a `public record ClientActivitySummary` with `ClientId`, `ClientName`, `ProjectCount`, `TotalHours`, `BillableHours`, `NonBillableHours`, `InvoiceCount`, `OutstandingTotal` (decimal hours and totals, matching the existing `DashboardSummary`).
2. Create `src/TimeTracker.Api/Services/IClientSummaryService.cs` with `Task<ClientActivitySummary?> GetSummaryAsync(int clientId, CancellationToken ct = default)`; `null` means the client does not exist.
3. Create `tests/TimeTracker.Tests/ClientSummaryServiceTests.cs` following the existing service tests' setup. Cover the happy path, client not found (returns `null`), a client with no activity (all zeros), and mixed billable/non-billable hours. Reference `ClientSummaryService`, which does not exist yet, so the test project does not compile until Slice 2 (TDD Red).

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Api/TimeTracker.Api.csproj --nologo -v q
node -e "const f=require('fs');['src/TimeTracker.Core/Models/ClientActivitySummary.cs','src/TimeTracker.Api/Services/IClientSummaryService.cs','tests/TimeTracker.Tests/ClientSummaryServiceTests.cs'].forEach(p=>f.statSync(p));if(!f.readFileSync('tests/TimeTracker.Tests/ClientSummaryServiceTests.cs','utf8').includes('ClientSummaryService'))throw new Error('tests do not exercise ClientSummaryService');console.log('OK')"
```

---

### Slice 2: Service Implementation (TDD Green) [depends: Slice 1] [scope: src/TimeTracker.Api/Services/ClientSummaryService.cs, src/TimeTracker.Api/Program.cs]

**Goal**: Implement the service so the Slice 1 tests pass, without breaking any existing test.

**Tasks**:
1. Create `src/TimeTracker.Api/Services/ClientSummaryService.cs` implementing `IClientSummaryService` with a primary constructor taking `TimeTrackerDbContext`. Return `null` when the client does not exist. Count only active projects; sum hours from the client's projects' time entries, split by `IsBillable`; `OutstandingTotal` sums `Total` of the client's invoices whose `Status` is `Draft` or `Issued`. Pass the `CancellationToken` to every EF Core call.
2. In `src/TimeTracker.Api/Program.cs`, add exactly one line: `builder.Services.AddScoped<IClientSummaryService, ClientSummaryService>();` next to the other service registrations.

**Validation Gate**:
```bash
dotnet test tests/TimeTracker.Tests/TimeTracker.Tests.csproj --nologo --filter ClientSummaryServiceTests
dotnet test TimeTracker.slnx --nologo
```

---

### Slice 3: Controller + Final Validation [depends: Slice 2] [scope: src/TimeTracker.Api/Controllers/ClientSummaryController.cs]

**Goal**: Expose the summary over HTTP with the codebase's controller conventions.

**Tasks**:
1. Create `src/TimeTracker.Api/Controllers/ClientSummaryController.cs`: `[ApiController]`, route `api/clients/{id:int}/summary`, primary constructor taking `IClientSummaryService`. `GET` returns `200` with the summary, or `404` ProblemDetails when the service returns `null`; propagate the request's `CancellationToken`; wrap unexpected errors in a `500` ProblemDetails like the existing controllers.
2. Leave no TODOs, FIXMEs, stubs or placeholder code in any file this phase created.

**Validation Gate**:
```bash
dotnet build TimeTracker.slnx --nologo -v q
dotnet test TimeTracker.slnx --nologo
node -e "const s=require('fs').readFileSync('src/TimeTracker.Api/Controllers/ClientSummaryController.cs','utf8');for(const t of ['[ApiController]','summary','CancellationToken','NotFound']){if(!s.includes(t))throw new Error('controller missing '+t)};if(/TODO|FIXME/.test(s))throw new Error('placeholder left');console.log('OK')"
```

---

## Definition of Done
- [ ] All acceptance criteria met
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (all tests green)
- [ ] No TODOs, FIXMEs, or placeholder code
- [ ] Follows existing codebase patterns (primary constructors, async, CancellationToken)
- [ ] Code reviewed via Step 5 Review Gate
