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

# Run the app
dotnet run --project src/TimeTracker.Api

# Run tests
dotnet test
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
