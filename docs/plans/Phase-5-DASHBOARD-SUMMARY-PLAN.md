# Phase 5: Dashboard Summary Endpoint

> **Pipeline Step**: 6 — Shipped  
> **Status**: Complete  
> **Author**: AI Agent (Step 0 — Specifier, Step 2 — Hardener)  
> **Created**: 2026-04-13  
> **Hardened**: 2026-04-13

---

## Scope Contract

### In Scope
- `GET /api/dashboard` endpoint returning aggregate metrics
- `DashboardSummary` response record in `TimeTracker.Core.Models`
- `IDashboardService` / `DashboardService` in `TimeTracker.Api.Services`
- `DashboardController` in `TimeTracker.Api.Controllers`
- `DashboardServiceTests` in `TimeTracker.Tests`
- DI registration in `Program.cs`

### Out of Scope — DO NOT TOUCH
- Existing controllers, services, models, or tests
- Database schema / migrations
- Authentication / authorization
- Caching infrastructure
- Docker / deployment files

### Forbidden Actions
- Do NOT modify any existing `*Controller.cs`, `*Service.cs`, or `*Tests.cs` files
- Do NOT add NuGet packages
- Do NOT modify `TimeTrackerDbContext.cs`
- Do NOT modify `appsettings.json` or `launchSettings.json`

### Files Created (Exhaustive)
| File | Layer |
|------|-------|
| `src/TimeTracker.Core/Models/DashboardSummary.cs` | Model (DTO) |
| `src/TimeTracker.Api/Services/IDashboardService.cs` | Service interface |
| `src/TimeTracker.Api/Services/DashboardService.cs` | Service implementation |
| `src/TimeTracker.Api/Controllers/DashboardController.cs` | Controller |
| `tests/TimeTracker.Tests/DashboardServiceTests.cs` | Tests |

### Files Modified (Exhaustive)
| File | Change |
|------|--------|
| `src/TimeTracker.Api/Program.cs` | Add `AddScoped<IDashboardService, DashboardService>()` — ONE line |

---

## Specification

### Problem Statement
The TimeTracker API has no single endpoint that provides aggregate metrics across all entities. Clients need a quick overview of their portfolio — total clients, projects, hours logged, and invoices — without making 4+ separate API calls.

### User Scenarios
1. **As an API consumer**, I want to call `GET /api/dashboard` and receive aggregate counts and totals so I can render a summary dashboard in one request.
2. **As a team lead**, I want to see total billable vs. non-billable hours so I can gauge utilization.
3. **As a project manager**, I want to see the total value of outstanding (non-paid) invoices.

### Acceptance Criteria
- [ ] `GET /api/dashboard` returns 200 with a `DashboardSummary` response
- [ ] Response includes: `totalClients`, `totalProjects`, `totalTimeEntries`, `totalHoursLogged`, `billableHours`, `nonBillableHours`, `totalInvoices`, `outstandingInvoiceTotal`
- [ ] Only active clients/projects counted in totals
- [ ] `outstandingInvoiceTotal` sums `Total` of invoices where `Status` is `Draft` or `Issued`
- [ ] Returns empty/zero values on an empty database (no 500)
- [ ] CancellationToken propagated through all layers
- [ ] Unit tests cover: happy path, empty database, mixed active/inactive entities

### Edge Cases
- Empty database → all zeros, 200 OK
- All clients inactive → `totalClients` = 0, but `totalTimeEntries` still counts if entries exist
- No invoices → `outstandingInvoiceTotal` = 0

### Out of Scope
- Date range filtering (future enhancement)
- Per-user breakdown (no user model yet)
- Caching (can add later with IDistributedCache)
- Authentication/authorization (not yet in the project)

### Open Questions
_None — all requirements are clear for this validation feature._

---

## Technical Approach

### Architecture (4-Layer)

| Layer | File | Responsibility |
|-------|------|----------------|
| **Model** | `DashboardSummary.cs` | Response DTO (record) |
| **Repository/Data** | Via `TimeTrackerDbContext` | Aggregate queries |
| **Service** | `IDashboardService` / `DashboardService` | Business logic — assemble summary |
| **Controller** | `DashboardController` | HTTP handling only |
| **Tests** | `DashboardServiceTests.cs` | Unit tests for service |

### Response Shape

```json
{
  "totalClients": 12,
  "totalProjects": 34,
  "totalTimeEntries": 456,
  "totalHoursLogged": 1234.50,
  "billableHours": 1100.00,
  "nonBillableHours": 134.50,
  "totalInvoices": 8,
  "outstandingInvoiceTotal": 15750.00
}
```

---

## Execution Slices

### Slice 1: Model + Service Interface + Tests (TDD Red)
**Files created**: `DashboardSummary.cs`, `IDashboardService.cs`, `DashboardServiceTests.cs`
**Validation gate**:
- [ ] `dotnet build` succeeds (tests compile)
- [ ] `dotnet test --filter DashboardServiceTests` — tests fail (Red phase confirmed)
- [ ] No changes to files outside scope contract

### Slice 2: Service Implementation (TDD Green)
**Files created**: `DashboardService.cs`
**Files modified**: `Program.cs` (DI registration — 1 line)
**Validation gate**:
- [ ] `dotnet test --filter DashboardServiceTests` — all tests pass (Green phase)
- [ ] `dotnet test` — all 42 existing tests still pass (regression check)
- [ ] No changes to files outside scope contract

### Slice 3: Controller + Final Validation
**Files created**: `DashboardController.cs`
**Validation gate**:
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` — all tests pass (existing + new)
- [ ] Controller follows existing pattern (try-catch, CancellationToken, `[ApiController]`)
- [ ] No TODOs, FIXMEs, stubs, or placeholder code

---

## Definition of Done
- [ ] All acceptance criteria met
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (all tests green)
- [ ] No TODOs, FIXMEs, or placeholder code
- [ ] Follows existing codebase patterns (primary constructors, async, CancellationToken)
- [ ] Code reviewed via Step 5 Review Gate
