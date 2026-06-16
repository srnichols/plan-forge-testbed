# Phase 2.5: Time Entry CRUD

> **Ordering note**: This phase was backfilled after the v1 demo rebuild revealed that the
> Time Entry CRUD surface was *assumed by every phase but owned by none*. It must run **after
> Phase 2 (Projects CRUD)** — `TimeEntry.ProjectId` is a foreign key to `Project` — and
> **before Phase 3 (Invoice Engine), Phase 4 (Reports), Phase 5 (Dashboard), and Phase 6
> (Web UI)**, all of which read or surface time entries.

## Feature Specification: Time Entry CRUD

### Problem Statement
TimeTracker's entire value proposition is logging billable hours, yet there is no API surface to
create, list, retrieve, or delete time entries. The Invoice Engine, Reports, Dashboard, and Web UI
all read `TimeEntry` data and the roadmap's DI contract lists `ITimeEntryService → TimeEntryService`
as a required registration — but no phase plan ever built it. The Blazor Time Entries page
(`/time-entries`) calls `GET/POST/DELETE api/timeentries` and fails with 404 against a missing
controller. This phase makes the Time Entry CRUD surface a first-class, owned deliverable.

### User Scenarios

**Scenario 1: List time entries**
1. A user navigates to GET /api/timeentries
2. The API returns all time entries, most recent first
3. Optional `?date=2026-06-06` and `?projectId=4` filters narrow the result set

**Scenario 2: Log a time entry**
1. A user POSTs `{ projectId, date, hours, description, isBillable }` to /api/timeentries
2. The API validates the project exists and hours are in range, persists the entry, returns 201 Created with a Location header
3. The new entry appears in the list and contributes to dashboard/report aggregates

**Scenario 3: Delete a time entry**
1. A user issues DELETE /api/timeentries/5
2. The API removes the entry and returns 204 No Content
3. Deleting a non-existent id returns 404

### Acceptance Criteria
- [ ] MUST: GET /api/timeentries returns all entries ordered by date descending
- [ ] MUST: GET /api/timeentries supports optional `date` (yyyy-MM-dd) and `projectId` query filters
- [ ] MUST: GET /api/timeentries/{id} returns a single entry or 404
- [ ] MUST: POST /api/timeentries creates an entry and returns 201 with `CreatedAtAction` Location
- [ ] MUST: POST validates hours are between 0.01 and 24, description ≤ 1000 chars, and the referenced project exists
- [ ] MUST: POST returns 400 ProblemDetails (via `ValidationProblem`) on any validation failure
- [ ] MUST: DELETE /api/timeentries/{id} returns 204 on success, 404 when the entry does not exist
- [ ] MUST: Controller depends only on `ITimeEntryService` — no `DbContext` in the controller
- [ ] SHOULD: Service uses `AsNoTracking` for reads and source-generated logging
- [ ] SHOULD: Unit tests cover create validation, project-existence check, filters, and delete

### Edge Cases
| Scenario | Expected Behavior |
|----------|-------------------|
| Hours ≤ 0 or > 24 | Return 400 ProblemDetails with validation error |
| Description > 1000 chars | Return 400 ProblemDetails with validation error |
| `projectId` does not exist | Return 400 ProblemDetails ("Project {id} does not exist.") |
| `date` filter with no matching entries | Return empty array — not 404 |
| DELETE non-existent id | Return 404 |

### Out of Scope
- Update/PUT of existing entries (Web UI uses create + delete only)
- Approval workflows, locking, or timesheet submission
- User authentication/authorization (not yet implemented in app)
- Modifying the `TimeEntry` model or `TimeTrackerDbContext`

### Open Questions
None — feature scope is well-defined and the Web UI client (`ITimeEntriesApi`) already pins the contract.

### Complexity Estimate
- Estimated effort: Small (1-2 hours)
- Estimated files: 4 (service interface, service impl, controller, test file) + DI registration
- Recommended pipeline: Full pipeline — all steps

---

## Scope Contract

### Inputs
- Existing `TimeEntry` model (`src/TimeTracker.Core/Models/TimeEntry.cs`) and `TimeTrackerDbContext`
- Existing `Project` model (FK target for `TimeEntry.ProjectId`)
- Existing layering convention (Controller → Service → data access)
- Web client contract: `ITimeEntriesApi` calls `GET/GET{id}/POST/DELETE api/timeentries`

### Outputs
- `ITimeEntryService` interface + `TimeEntryService` implementation
- `TimeEntriesController` with GET (list, filtered), GET/{id}, POST, DELETE endpoints
- DI registration `ITimeEntryService → TimeEntryService` in `Program.cs`
- `TimeEntryServiceTests` with unit tests for validation, filters, and delete

### Forbidden Actions
- DO NOT modify existing controllers (ClientsController, ProjectsController, InvoicesController, ReportsController, DashboardController)
- DO NOT modify existing models (Client, Project, TimeEntry, Invoice, InvoiceLine)
- DO NOT modify existing services (ClientService, ProjectService, InvoiceService, TimeEntryReportService, DashboardService)
- DO NOT inject `TimeTrackerDbContext` into `TimeEntriesController` — depend on `ITimeEntryService`
- DO NOT add new NuGet packages

### Definition of Done
- [ ] All CRUD endpoints return correct responses and status codes
- [ ] Service layer properly separated from controller (no `DbContext` in controller)
- [ ] All edge cases handled with ProblemDetails responses
- [ ] Unit tests pass for service logic
- [ ] `dotnet build` succeeds with zero warnings
- [ ] `dotnet test` passes all existing + new tests
- [ ] `GET /api/timeentries` returns the seeded entries (the `/time-entries` Web page loads)

---

## Execution Slices

### Slice 1: Service interface + implementation [scope: src/TimeTracker.Api/Services/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Files**:
- `src/TimeTracker.Api/Services/ITimeEntryService.cs`
- `src/TimeTracker.Api/Services/TimeEntryService.cs`

**Tasks**:
1. Create `ITimeEntryService` with `GetAllAsync(date?, projectId?)`, `GetByIdAsync`, `CreateAsync`, `DeleteAsync`
2. Implement `TimeEntryService` as `sealed partial class` with primary constructor `(TimeTrackerDbContext dbContext, ILogger<TimeEntryService> logger)`
3. Reads use `AsNoTracking`; list ordered by `Date` descending then `Id` descending
4. `CreateAsync` validates hours (0.01–24) and description (≤ 1000), verifies the project exists, stores `Date` as UTC, uses source-generated logging
5. `DeleteAsync` removes the entry, returns `false` when not found

**Validation Gate**: Service compiles, implements interface, no `DbContext` leaks outside the service.

### Slice 2: Controller + DI registration [scope: src/TimeTracker.Api/Controllers/**, src/TimeTracker.Api/Program.cs]
**Build command**: `dotnet build`

**Files**:
- `src/TimeTracker.Api/Controllers/TimeEntriesController.cs`
- `src/TimeTracker.Api/Program.cs` (DI registration only)

**Tasks**:
1. Create `TimeEntriesController` (`[ApiController]`, `[Route("api/[controller]")]`) depending only on `ITimeEntryService`
2. `GET` with `[FromQuery] DateTime? date, int? projectId`; `GET {id:int}`; `POST` with a `CreateTimeEntryRequest` record + DataAnnotations; `DELETE {id:int}`
3. POST returns `CreatedAtAction(nameof(GetById), …)`; catch `ValidationException` → `ValidationProblem`
4. Register `builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();`

**Validation Gate**: `dotnet build` succeeds; `GET /api/timeentries` returns 200 with seeded entries.

### Slice 3: Unit tests [scope: tests/TimeTracker.Tests/**]
**Test command**: `dotnet test --verbosity quiet`

**Files**:
- `tests/TimeTracker.Tests/TimeEntryServiceTests.cs`

**Tasks**:
1. Test create with valid input persists and returns the entry
2. Test create with out-of-range hours and oversized description throw `ValidationException`
3. Test create with a non-existent `projectId` throws `ValidationException`
4. Test `GetAllAsync` filters by `date` and `projectId`
5. Test `DeleteAsync` returns `false` for a missing id and `true` after removing an existing entry

**Validation Gate**: `dotnet test` passes all new + existing tests.
