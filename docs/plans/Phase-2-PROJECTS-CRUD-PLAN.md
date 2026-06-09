# Phase 2: Projects CRUD

> **Status**: 🟡 HARDENED — Ready for execution
> **Estimated Effort**: 0.5 day (3 execution slices)
> **Feature Branch**: `feature/phase-2-projects-crud`

---

## Overview

Add CRUD operations for the `Project` entity — API endpoints linked to clients, business validation, and tests. Projects belong to clients and track billable work.

## Prerequisites

- [x] Phase 1 complete (Client model + ClientService exist)
- [x] Database schema with `Projects` table (exists in `TimeTrackerDbContext`)

## Acceptance Criteria

- [ ] `GET /api/projects` returns all projects (filterable by client)
- [ ] `POST /api/projects` creates a project linked to a client
- [ ] `PUT /api/projects/{id}` updates project details
- [ ] `DELETE /api/projects/{id}` soft-deletes (sets IsActive=false)
- [ ] Unit tests for ProjectService
- [ ] `dotnet test` passes with 0 failures

---

## Scope Contract

### In Scope
- `src/TimeTracker.Api/Controllers/ProjectsController.cs` — CRUD endpoints
- `src/TimeTracker.Api/Services/ProjectService.cs` — business logic + validation
- `tests/TimeTracker.Tests/ProjectServiceTests.cs` — unit tests

### Out of Scope
- Time entry management (already exists)
- Billing reports (already exists)
- Client management (Phase 1)
- UI frontend

### Forbidden Actions
- Do NOT modify `TimeEntriesController.cs` or `BillingController.cs`
- Do NOT modify `Client.cs` model
- Do NOT change `docker-compose.yml`
- Do NOT inject `TimeTrackerDbContext` into `ProjectsController` — the controller is HTTP-only and must depend on the `IProjectService` abstraction (Controller → Service → data access)

---

## Execution Slices

### Slice 1: ProjectService with business logic [scope: src/TimeTracker.Api/Services/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `IProjectService` interface with `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`
2. Implement `ProjectService` with EF Core queries
3. Validation: name required, client must exist, no duplicate project names per client
4. Soft-delete: `DeactivateAsync` sets `IsActive = false`
5. Register `IProjectService` in DI (`Program.cs`)

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

### Slice 2: Unit tests for ProjectService [depends: Slice 1] [scope: tests/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `ProjectServiceTests.cs` with in-memory database
2. Test: CreateAsync with valid data succeeds
3. Test: CreateAsync with empty name throws validation error
4. Test: CreateAsync with non-existent client throws
5. Test: DeactivateAsync sets IsActive to false
6. Test: GetAllAsync filters by clientId when provided

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

**Stop Condition**: If any test fails → STOP, do not proceed to Slice 3.

### Slice 3: ProjectsController with all endpoints [depends: Slice 1, Slice 2] [scope: src/TimeTracker.Api/Controllers/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `ProjectsController.cs` with `[ApiController]` and `[Route("api/[controller]")]`, injecting `IProjectService` via constructor DI — the controller must NOT use `TimeTrackerDbContext` directly (HTTP-only layer; all data access goes through the service)
2. Implement `GET /api/projects` — return all active projects, optional `?clientId=N` filter
3. Implement `GET /api/projects/{id}` — return single project or 404
4. Implement `POST /api/projects` — create with validation (name required, clientId required)
5. Implement `PUT /api/projects/{id}` — update name, description
6. Implement `DELETE /api/projects/{id}` — soft-delete via DeactivateAsync

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

> **Layer check**: `ProjectsController` must declare a constructor parameter of type `IProjectService` and reference no `TimeTrackerDbContext` — confirm before closing the slice.

---

## Definition of Done
- [ ] All 3 slices pass validation gates
- [ ] `dotnet test` passes with 0 failures
- [ ] API endpoints respond correctly
- [ ] No TODO/FIXME markers in new code
