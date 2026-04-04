# Phase 1: Clients CRUD

> **Status**: 🟡 HARDENED — Ready for execution
> **Estimated Effort**: 1 day (4 execution slices)
> **Feature Branch**: `feature/phase-1-clients-crud`

---

## Overview

Add full CRUD operations for the `Client` entity — API endpoints, business validation, and tests.

## Prerequisites

- [x] Database schema with `Clients` table (exists in `TimeTrackerDbContext`)
- [x] Docker Compose for PostgreSQL

## Acceptance Criteria

- [ ] `GET /api/clients` returns all clients
- [ ] `POST /api/clients` creates a client with validation
- [ ] `PUT /api/clients/{id}` updates a client
- [ ] `DELETE /api/clients/{id}` soft-deletes (sets IsActive=false)
- [ ] Unit tests for validation logic
- [ ] Integration test for CRUD lifecycle

---

## Scope Contract

### In Scope
- `src/TimeTracker.Api/Controllers/ClientsController.cs` — CRUD endpoints
- `src/TimeTracker.Api/Services/ClientService.cs` — business logic
- `tests/TimeTracker.Tests/ClientServiceTests.cs` — unit tests

### Out of Scope
- Project management (Phase 2)
- Billing reports (existing)
- UI frontend

### Forbidden Actions
- Do NOT modify the `TimeEntry` or `Billing` code
- Do NOT change `docker-compose.yml`

---

## Execution Slices

### Slice 1: ClientsController with GET/POST [P] [scope: src/TimeTracker.Api/Controllers/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `ClientsController.cs` with `[ApiController]` and `[Route("api/[controller]")]`
2. Implement `GET /api/clients` — return all active clients
3. Implement `GET /api/clients/{id}` — return single client or 404
4. Implement `POST /api/clients` — create with validation (name required, hourlyRate > 0)

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

### Slice 2: ClientService with business logic [P] [scope: src/TimeTracker.Api/Services/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `IClientService` interface with `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`
2. Implement `ClientService` with EF Core queries
3. Validation: name required, email format, hourlyRate > 0
4. Soft-delete: `DeactivateAsync` sets `IsActive = false`

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

### Slice 3: Unit tests for ClientService [depends: Slice 2] [scope: tests/**]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Create `ClientServiceTests.cs` with in-memory database
2. Test: CreateAsync with valid data succeeds
3. Test: CreateAsync with empty name throws
4. Test: DeactivateAsync sets IsActive to false
5. Test: GetAllAsync returns only active clients

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

**Stop Condition**: If any test fails → STOP, do not proceed to Slice 4.

### Slice 4: PUT/DELETE endpoints + integration [depends: Slice 1, Slice 3]
**Build command**: `dotnet build`
**Test command**: `dotnet test --verbosity quiet`

**Tasks**:
1. Add `PUT /api/clients/{id}` to controller — update name, email, hourlyRate
2. Add `DELETE /api/clients/{id}` to controller — calls `DeactivateAsync`
3. Wire `IClientService` into DI in `Program.cs`
4. Integration test: full CRUD lifecycle (create → read → update → delete → verify)

**Validation Gate**:
```bash
dotnet build
dotnet test --verbosity quiet
```

---

## Definition of Done
- [ ] All 4 slices pass validation gates
- [ ] `dotnet test` passes with 0 failures
- [ ] API endpoints respond correctly
- [ ] No TODO/FIXME markers in new code
