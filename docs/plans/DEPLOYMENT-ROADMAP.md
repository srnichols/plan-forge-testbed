# Deployment Roadmap

> **Purpose**: Master tracker for all project phases.
> **How to use**: Add phases as they're planned. Link to plan files. Update status as work progresses.

---

## Status Legend

| Icon | Meaning |
|------|---------|
| 📋 | Planned — not yet started |
| 🚧 | In Progress — actively being worked on |
| ✅ | Complete — all Definition of Done criteria met |
| ⏸️ | Paused — blocked or deprioritized |

---

## Phases

### Phase 1: Clients CRUD
**Goal**: Full CRUD API for client management with validation and soft-delete
**Plan**: [Phase-1-CLIENTS-CRUD-PLAN.md](./Phase-1-CLIENTS-CRUD-PLAN.md)
**Status**: ✅ Complete

---

### Phase 2: Projects CRUD
**Goal**: Full CRUD API for project management with client relationship
**Plan**: [Phase-2-PROJECTS-CRUD-PLAN.md](./Phase-2-PROJECTS-CRUD-PLAN.md)
**Status**: ✅ Complete

---

### Phase 3: Invoice Engine
**Goal**: Invoice generation with rate tiers, volume discounts, and state machine
**Plan**: [Phase-3-INVOICE-ENGINE-PLAN.md](./Phase-3-INVOICE-ENGINE-PLAN.md)
**Status**: ✅ Complete

---

### Phase 4: Time Entry Reports & Analytics
**Goal**: Reporting endpoints for hours summary, project breakdown, and daily timeline
**Plan**: [Phase-4-TIME-ENTRY-REPORTS-PLAN.md](./Phase-4-TIME-ENTRY-REPORTS-PLAN.md)
**Status**: 🚧 In Progress

---

### Phase 5: Dashboard Summary Endpoint
**Goal**: Single endpoint returning aggregate metrics across all entities
**Plan**: [Phase-5-DASHBOARD-SUMMARY-PLAN.md](./Phase-5-DASHBOARD-SUMMARY-PLAN.md)
**Status**: ✅ Complete

---

### Phase 6: Blazor Server Web UI
**Goal**: Enterprise-grade Blazor Server + Microsoft Fluent UI front-end (`TimeTracker.Web`) calling the existing REST API via a typed HttpClient SDK (`TimeTracker.Web.Client`). Demonstrates that pforge produces UI with strict layering (no `DbContext` in components), full WCAG 2.1 AA accessibility, and bUnit-tested components — not vibe-coded UI.
**Plan**: [Phase-6-WEB-UI-PLAN.md](./Phase-6-WEB-UI-PLAN.md)
**Status**: ✅ Complete (2026-05-05) — 7 slices passed in 39m 27s. Layering audit clean (no `DbContext`/`EntityFrameworkCore`/`TimeTracker.Api.*` references in any `.razor`/`.razor.cs`); 11 bUnit tests pass; full solution `dotnet test`: 57/57 (46 backend + 11 bUnit); 0 warnings, 0 errors.

---

<!-- Add more phases as needed. Each phase should link to its *-PLAN.md file. -->

---

## Demo Seed Data

> **Purpose**: Reproducible demonstration data so a clean build comes up with clients, projects, time entries, and invoices to show — without this, every dropdown is empty and there are no invoices to view.
> **File**: `src/TimeTracker.Api/Data/DbSeeder.cs` (static `DbSeeder.SeedAsync`)
> **Wire-up**: Called once at startup in `src/TimeTracker.Api/Program.cs` after `db.Database.EnsureCreated()` → `await DbSeeder.SeedAsync(db);`
> **Store**: EF Core **InMemory** database named `TimeTracker` (Docker-free fallback; data is rebuilt on every run).

**Behavior contract**:
- **Idempotent** — `SeedAsync` is a no-op if any client already exists, so it is safe to call on every startup.
- Shared invoice rules: tax rate constant **8.5%**, all seeded invoices are created in **`Issued`** status, every line uses **`RateType.Standard`**, and there is **no discount**. Invoice totals are computed from billable hours (`Subtotal = Σ line totals`, `TaxAmount = round(Subtotal × 8.5%, 2)`, `Total = Subtotal + TaxAmount`).
- Two clients are seeded, each with multiple projects, a work week of time entries (mix of billable and non-billable), and one generated invoice.

**Seeded clients**:

| Client | Email | Hourly Rate | Projects | Invoice | Period | Invoice lines (billable hours) |
|--------|-------|-------------|----------|---------|--------|-------------------------------|
| **Contoso Ltd** | demo@contoso.com | $150 | Website Redesign, Mobile App | `INV-2026-0001` | 2026-06-01 → 06-05 | Website Redesign — 29.5h |
| **Adventure Works** | demo@adventure-works.test | $175 | Azure Integration, Teams Bot, Copilot Plugin | `INV-2026-0002` | 2026-06-08 → 06-12 | Azure Integration — 14h; Teams Bot — 12h; Copilot Plugin — 8h |

> Time entries marked `IsBillable = false` (Contoso "Internal sync and planning"; Adventure Works "Internal design review") are intentionally excluded from invoice totals to demonstrate billable-vs-non-billable handling.

**Regeneration check** (a clean build should satisfy):
```bash
dotnet run --project src/TimeTracker.Api &
# then:
curl http://localhost:5000/api/clients        # → Contoso Ltd + Adventure Works
curl http://localhost:5000/api/invoices/1      # → INV-2026-0001 (Contoso, Issued)
curl http://localhost:5000/api/invoices/2      # → INV-2026-0002 (Adventure Works, Issued)
```

---

## API Host Wiring

> **Purpose**: Cross-phase composition-root facts that live in `src/TimeTracker.Api/Program.cs` and are assumed by every phase but owned by none.

- **Database provider selection** — `AddDbContext<TimeTrackerDbContext>` uses Npgsql when `ConnectionStrings:DefaultConnection` is set, otherwise falls back to **EF Core InMemory** (database name `TimeTracker`). This is what lets the demo run without Docker/PostgreSQL.
- **Dev-only seed** — inside `if (app.Environment.IsDevelopment())`: `db.Database.EnsureCreated()` then `await DbSeeder.SeedAsync(db)` (see **Demo Seed Data** above).
- **JSON cycle handling** — controllers add `ReferenceHandler.IgnoreCycles` so entity navigation properties (e.g. `Invoice.InvoiceLines`) serialize without reference loops.
- **Service registration (DI)** — every controller depends on a service interface, never `DbContext` directly (strict Controller → Service → data access layering per `architecture-principles.instructions.md`). Registered scoped services:
  - `IClientService → ClientService`
  - `IProjectService → ProjectService`
  - `ITimeEntryService → TimeEntryService`
  - `IInvoiceService → InvoiceService`
  - `IDashboardService → DashboardService`
- **Health endpoint** — `GET /health` returns `{ status = "healthy", timestamp }`.

---

## Completed Phases

<!-- Move phases here when they reach ✅ Complete status -->

| Phase | Goal | Plan | Completed |
|-------|------|------|-----------|
| — | — | — | — |

---

## Notes

- Each phase goes through the [Plan Forge Pipeline](./AI-Plan-Hardening-Runbook-Instructions.md) before execution
- Phase plans are stored in this directory (`docs/plans/`)
- Guardrail files are updated after each phase completion (Step 5 of the pipeline)
