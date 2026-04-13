# Phase 4: Time Entry Reports & Analytics

## Feature Specification: Time Entry Reports

### Problem Statement
Project managers and freelancers using TimeTracker need to understand their time allocation, productivity trends, and project profitability without generating full invoices. Currently, the only reporting available is the basic billing summary endpoint. Users need richer analytics: hours breakdown by project, daily/weekly trends, and utilization rates across date ranges.

### User Scenarios

**Scenario 1: Weekly Hours Summary**
1. A project manager navigates to GET /api/reports/hours-summary?start=2026-04-06&end=2026-04-12
2. The API returns total hours, billable vs non-billable breakdown, and hours by project
3. The manager sees they logged 42 billable hours and 3 non-billable hours across 4 projects

**Scenario 2: Project Breakdown**
1. A freelancer queries GET /api/reports/project-breakdown?start=2026-04-01&end=2026-04-30
2. The API returns each project with hours, percentage of total, and average hours per day
3. The freelancer sees Project Alpha consumed 60% of their time

**Scenario 3: Daily Timeline**
1. A team lead queries GET /api/reports/daily-timeline?start=2026-04-01&end=2026-04-30
2. The API returns daily hour totals with billable/non-billable split
3. The lead identifies low-productivity days and overwork days (>8h)

### Acceptance Criteria
- [ ] MUST: GET /api/reports/hours-summary returns total hours, billable hours, non-billable hours, and count of entries for a date range
- [ ] MUST: GET /api/reports/project-breakdown returns per-project hours with percentage of total
- [ ] MUST: GET /api/reports/daily-timeline returns daily hour aggregations for a date range
- [ ] MUST: All endpoints require start and end date query parameters
- [ ] MUST: All endpoints return 400 with ProblemDetails when start > end
- [ ] MUST: Empty date ranges return zero-value responses (not 404)
- [ ] SHOULD: Endpoints support optional projectId filter
- [ ] SHOULD: All new code follows service layer pattern (no direct DbContext in controller)
- [ ] SHOULD: Unit tests cover all aggregation logic
- [ ] MAY: Support clientId filter on summary endpoints

### Edge Cases
| Scenario | Expected Behavior |
|----------|-------------------|
| Start date after end date | Return 400 ProblemDetails with validation error |
| No time entries in range | Return response with zeros, empty arrays — not 404 |
| Extremely large date range (10+ years) | Process normally (data volume is bounded by entries) |
| Invalid date format | Return 400 ProblemDetails |
| Non-existent projectId filter | Return empty results (not 404) |

### Out of Scope
- PDF/Excel export (future phase)
- Caching layer for reports (future optimization)
- Real-time dashboard or WebSocket updates
- User authentication/authorization (not yet implemented in app)
- Modifying existing TimeEntry or Invoice endpoints

### Open Questions
None — feature scope is well-defined.

### Complexity Estimate
- Estimated effort: Medium (2-4 hours)
- Estimated files: 8 (models/DTOs, service interface, service impl, controller, 2 test files, DI registration)
- Recommended pipeline: Full pipeline — all steps

---

## Scope Contract

### Inputs
- Existing `TimeEntry` model and `TimeTrackerDbContext`
- Existing project structure (Controller → Service → Data Access)

### Outputs
- `ITimeEntryReportService` interface + `TimeEntryReportService` implementation
- `ReportsController` with 3 GET endpoints
- Response DTOs: `HoursSummaryResponse`, `ProjectBreakdownResponse`, `DailyTimelineResponse`
- `TimeEntryReportServiceTests` with comprehensive unit tests

### Forbidden Actions
- DO NOT modify existing controllers (ClientsController, InvoicesController, BillingController, TimeEntriesController)
- DO NOT modify existing models (Client, Project, TimeEntry, Invoice)
- DO NOT modify existing services (ClientService, InvoiceService, BillingService, ProjectService)
- DO NOT modify existing tests
- DO NOT add new NuGet packages

### Definition of Done
- [ ] All 3 report endpoints return correct data
- [ ] Service layer properly separated from controller
- [ ] All edge cases handled with ProblemDetails responses
- [ ] Unit tests pass for all aggregation logic
- [ ] `dotnet build` succeeds with zero warnings
- [ ] `dotnet test` passes all existing + new tests

---

## Execution Slices

### Slice 1: DTOs and Service Interface
**Files**: 
- `src/TimeTracker.Core/Models/HoursSummaryResponse.cs`
- `src/TimeTracker.Core/Models/ProjectBreakdownResponse.cs`
- `src/TimeTracker.Core/Models/DailyTimelineResponse.cs`
- `src/TimeTracker.Api/Services/ITimeEntryReportService.cs`

**Validation Gate**: Files compile, DTOs have correct properties with explicit types

### Slice 2: Service Implementation
**Files**:
- `src/TimeTracker.Api/Services/TimeEntryReportService.cs`

**Validation Gate**: Service compiles, implements interface, uses LINQ aggregations with async/await

### Slice 3: Controller + DI Registration
**Files**:
- `src/TimeTracker.Api/Controllers/ReportsController.cs`
- `src/TimeTracker.Api/Program.cs` (DI registration only)

**Validation Gate**: `dotnet build` succeeds, endpoints return structured responses

### Slice 4: Unit Tests
**Files**:
- `tests/TimeTracker.Tests/TimeEntryReportServiceTests.cs`

**Validation Gate**: `dotnet test` passes all new + existing tests
