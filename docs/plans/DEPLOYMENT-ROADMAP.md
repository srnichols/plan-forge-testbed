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
