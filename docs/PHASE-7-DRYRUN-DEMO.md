# Phase 7 — Repeatable Dry-Run Demo

> How to demo pforge executing a phase live in the dashboard, repeatedly, with zero risk.
>
> **Plan file**: [plans/Phase-7-CLIENT-SUMMARY-PLAN.md](./plans/Phase-7-CLIENT-SUMMARY-PLAN.md)

## Why it's safe to run over and over

- pforge operates on repo **source + git**, NOT the running TimeTracker Docker container (separate artifact; its in-memory DB + idempotent `DbSeeder` rebuild on restart).
- `--dry-run` = *"parse and validate without executing"* — no files written, no commits. `git status` stays clean.
- Competitive/quorum slices run in isolated `.forge/worktrees/` (detached; never writes `refs/heads/`), so `master` is untouched.
- Keep the Phase 7 plan **unmarked-complete** (no ✅) — a completed plan makes pforge skip all slices (*"all slices were skipped — plan was already complete. No action required; this was a no-op re-run."*).

## The command (Windows PowerShell)

**Core** (auto mode is default; `gh-copilot` is the default worker):

```powershell
pforge run-plan docs/plans/Phase-7-CLIENT-SUMMARY-PLAN.md --dry-run --worker gh-copilot
```

**Background, logged to file:**

```powershell
Start-Process -FilePath pforge `
  -ArgumentList 'run-plan','docs/plans/Phase-7-CLIENT-SUMMARY-PLAN.md','--dry-run','--worker','gh-copilot' `
  -WorkingDirectory 'e:\GitHub\plan-forge-testbed' `
  -RedirectStandardOutput 'phase7-dryrun.log' `
  -RedirectStandardError 'phase7-dryrun.err.log' `
  -NoNewWindow
```

**Background as a pollable job:**

```powershell
Start-Job -Name phase7 -ScriptBlock {
  Set-Location 'e:\GitHub\plan-forge-testbed'
  .\pforge.ps1 run-plan docs/plans/Phase-7-CLIENT-SUMMARY-PLAN.md --dry-run --worker gh-copilot
}
# then:
Receive-Job -Name phase7 -Keep
```

## Flag reference

| Part | Effect |
|------|--------|
| `run-plan <plan>` | Executes the plan; **auto mode is default** (no `--assisted` = no human gates) |
| `--dry-run` | *Parse and validate without executing* — no files written, no commits, infinitely repeatable |
| `--worker gh-copilot` | GitHub Copilot CLI worker (also the auto-detected default; specifying it is optional) |

## Honest nuance

- In **pure dry-run**, pforge validates + streams the slice flow to the dashboard but does **not** actually spawn `gh copilot` to write code. That is exactly what makes it loop-safe.
- For a demo where the worker writes **real** code/commits, drop `--dry-run` — but that is a one-time real run, not repeatable.

## Dashboard equivalent

- Open the plan browser → select **Phase 7** → run with the **dry-run** toggle on.
- MCP equivalent: `forge_run_plan` with `dryRun: true`.
- Inbound trigger: `POST /api/runs/trigger { plan, dryRun: true }`.

## Verify it left nothing behind

```powershell
git status        # should be clean after a dry-run
```

The running TimeTracker container (host `8081` → container `8080`) is unaffected — it is a separately-built artifact.
