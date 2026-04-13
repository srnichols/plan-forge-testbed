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
