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

### Slice 1: Model + Service Interface + Tests (TDD Red)
**Files created**: `ClientActivitySummary.cs`, `IClientSummaryService.cs`, `ClientSummaryServiceTests.cs`
**Validation gate**:
- [ ] `dotnet build` succeeds (tests compile)
- [ ] `dotnet test --filter ClientSummaryServiceTests` — tests fail (Red phase confirmed)
- [ ] No changes to files outside scope contract

### Slice 2: Service Implementation (TDD Green)
**Files created**: `ClientSummaryService.cs`
**Files modified**: `Program.cs` (DI registration — 1 line)
**Validation gate**:
- [ ] `dotnet test --filter ClientSummaryServiceTests` — all tests pass (Green phase)
- [ ] `dotnet test` — all existing tests still pass (regression check)
- [ ] No changes to files outside scope contract

### Slice 3: Controller + Final Validation
**Files created**: `ClientSummaryController.cs`
**Validation gate**:
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` — all tests pass (existing + new)
- [ ] Controller follows existing pattern (try-catch, CancellationToken, `[ApiController]`, 404 ProblemDetails on not-found)
- [ ] No TODOs, FIXMEs, stubs, or placeholder code

---

## Definition of Done
- [ ] All acceptance criteria met
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (all tests green)
- [ ] No TODOs, FIXMEs, or placeholder code
- [ ] Follows existing codebase patterns (primary constructors, async, CancellationToken)
- [ ] Code reviewed via Step 5 Review Gate
