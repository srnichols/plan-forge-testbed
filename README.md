# Plan Forge Testbed

QA harness for testing all Plan Forge v2.0 capabilities against a real .NET application.

## The App: TimeTracker

A simple billable hours tracker:
- **Clients** — manage clients with hourly rates
- **Projects** — track projects per client
- **Time Entries** — log hours per project per day
- **Billing** — calculate billable hours and totals

**Stack**: .NET 10 Web API + EF Core + PostgreSQL + Docker

## Quick Start

```bash
# Start database
docker compose up -d db

# Run the API
dotnet run --project src/TimeTracker.Api

# Run the web UI (Blazor Server)
dotnet run --project src/TimeTracker.Web
# Browse to http://localhost:5100

# Run tests
dotnet test
```

> **Note**: The Blazor Web app calls the REST API at `http://localhost:5000`. Run **both** `TimeTracker.Api` and `TimeTracker.Web` for a full end-to-end demo.

## Solution Layout

```
src/
├── TimeTracker.Api/          REST API (ASP.NET Core, EF Core, PostgreSQL)
├── TimeTracker.Core/         Domain entities & contracts
├── TimeTracker.Web/          Blazor Server UI (Fluent UI, port 5100)
└── TimeTracker.Web.Client/   Typed HttpClient SDK used by Web
```

## Plan Forge Testing

```powershell
# Run all tests
.\run-tests.ps1

# Run specific test
.\run-tests.ps1 -TestName estimate
.\run-tests.ps1 -TestName orchestrator-self-test

# Manual test: estimate a plan
.\pforge.ps1 run-plan docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md --estimate

# Manual test: full auto execution
.\pforge.ps1 run-plan docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md

# Manual test: assisted mode
.\pforge.ps1 run-plan --assisted docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md

# Dashboard
# Start MCP server, then visit http://localhost:3100/dashboard
```

## Test Matrix

| Test | What It Validates |
|------|------------------|
| `dotnet-build` | App compiles |
| `dotnet-test` | Unit tests pass |
| `smith` | Plan Forge environment diagnostics |
| `validate` | Setup file validation |
| `status` | Roadmap phase parsing |
| `sweep` | Completeness marker scanning |
| `estimate` | Plan parsing + cost estimation |
| `dry-run` | Full orchestrator dry run |
| `plan-parse` | DAG, `[P]` tags, `[depends:]`, `[scope:]` |
| `orchestrator-self-test` | 69 internal tests |
| `analyze` | Cross-artifact analysis |
| `cost-report-empty` | Cost report with no history |

## Plans

- `Phase-1-CLIENTS-CRUD-PLAN.md` — 4 slices with `[P]` tags, `[depends:]`, `[scope:]`, validation gates
- `Phase-2-PROJECTS-CRUD-PLAN.md` through `Phase-5-DASHBOARD-SUMMARY-PLAN.md` — backend feature phases (all complete)
- `Phase-6-WEB-UI-PLAN.md` — **NEW**: Blazor Server + Microsoft Fluent UI front-end (`TimeTracker.Web` + typed `TimeTracker.Web.Client` SDK). 7 slices covering Web project scaffold, typed HttpClient, Dashboard, Clients CRUD, Projects/Time Entries/Invoices, bUnit tests, and solution wiring. Demonstrates pforge produces enterprise-grade UI (strict layering, WCAG 2.1 AA, bUnit-tested) — see plan header for the rationale.
