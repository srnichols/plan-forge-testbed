<#
.SYNOPSIS
    Plan Forge Testbed — Test Runner

.DESCRIPTION
    Exercises all Plan Forge v2.0 capabilities against the TimeTracker app.
    Run from the plan-forge-testbed repo root.

.EXAMPLE
    .\run-tests.ps1
    .\run-tests.ps1 -TestName "estimate"
#>

param(
    [string]$TestName = "all"
)

$ErrorActionPreference = 'Continue'
$passed = 0
$failed = 0
$skipped = 0

function Test-Step([string]$Name, [scriptblock]$Action) {
    if ($TestName -ne "all" -and $TestName -ne $Name) {
        Write-Host "  ⏭️  $Name (skipped)" -ForegroundColor DarkGray
        $script:skipped++
        return
    }
    Write-Host "  ▶  $Name" -ForegroundColor Cyan -NoNewline
    try {
        $result = & $Action
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Exit code: $LASTEXITCODE" }
        Write-Host "  ✅" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "  ❌ $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Plan Forge Testbed — Test Runner                ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ─── Prerequisite: dotnet build ─────────────────────────────────────
Write-Host "Prerequisites:" -ForegroundColor Yellow

Test-Step "dotnet-build" {
    dotnet build --verbosity quiet 2>&1 | Out-Null
}

Test-Step "dotnet-test" {
    dotnet test --verbosity quiet 2>&1 | Out-Null
}

# ─── Test 1: Smith ──────────────────────────────────────────────────
Write-Host ""
Write-Host "CLI Commands:" -ForegroundColor Yellow

Test-Step "smith" {
    $output = pwsh -NoProfile -File pforge.ps1 smith 2>&1 | Out-String
    if ($output -notmatch "passed") { throw "Smith did not report passed" }
}

# ─── Test 2: Validate ──────────────────────────────────────────────
Test-Step "validate" {
    pwsh -NoProfile -File pforge.ps1 check 2>&1 | Out-Null
}

# ─── Test 3: Status ────────────────────────────────────────────────
Test-Step "status" {
    $output = pwsh -NoProfile -File pforge.ps1 status 2>&1 | Out-String
    if ($output -notmatch "Phase|Planned") { throw "Status did not find phases" }
}

# ─── Test 4: Sweep ─────────────────────────────────────────────────
Test-Step "sweep" {
    pwsh -NoProfile -File pforge.ps1 sweep 2>&1 | Out-Null
}

# ─── Test 5: Estimate ──────────────────────────────────────────────
Write-Host ""
Write-Host "Orchestrator:" -ForegroundColor Yellow

Test-Step "estimate" {
    $output = pwsh -NoProfile -File pforge.ps1 run-plan docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md --estimate 2>&1 | Out-String
    if ($output -notmatch "sliceCount") { throw "Estimate did not return sliceCount" }
    if ($output -notmatch "estimatedCostUSD") { throw "Estimate did not return cost" }
}

# ─── Test 6: Dry Run ───────────────────────────────────────────────
Test-Step "dry-run" {
    $output = pwsh -NoProfile -File pforge.ps1 run-plan docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md --dry-run 2>&1 | Out-String
    if ($output -notmatch "dry-run") { throw "Dry run did not return status" }
}

# ─── Test 7: Plan Parse ────────────────────────────────────────────
Test-Step "plan-parse" {
    $output = node mcp/orchestrator.mjs --parse docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md 2>&1 | Out-String
    if ($output -notmatch "ClientsController") { throw "Parse did not find slice 1 title" }
    if ($output -notmatch "parallel.*true") { throw "Parse did not detect [P] tags" }
    if ($output -notmatch "depends") { throw "Parse did not detect dependencies" }
}

# ─── Test 8: Orchestrator Self-Test ─────────────────────────────────
Test-Step "orchestrator-self-test" {
    $output = node mcp/orchestrator.mjs --test 2>&1 | Out-String
    if ($output -notmatch "passed, 0 failed") { throw "Self-test did not pass all" }
}

# ─── Test 9: Analyze ───────────────────────────────────────────────
Test-Step "analyze" {
    $output = pwsh -NoProfile -File pforge.ps1 analyze docs/plans/Phase-1-CLIENTS-CRUD-PLAN.md 2>&1 | Out-String
    # analyze may warn about coverage but should not crash
}

# ─── Test 10: Cost Report (empty) ──────────────────────────────────
Write-Host ""
Write-Host "Cost Tracking:" -ForegroundColor Yellow

Test-Step "cost-report-empty" {
    $output = node --input-type=module -e "import{getCostReport}from'./mcp/orchestrator.mjs';console.log(JSON.stringify(getCostReport(process.cwd())))" 2>&1 | Out-String
    if ($output -notmatch "runs|No cost") { throw "Cost report failed" }
}

# ─── Summary ────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Gray
$color = if ($failed -gt 0) { 'Red' } else { 'Green' }
Write-Host "  Results: $passed passed, $failed failed, $skipped skipped" -ForegroundColor $color
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

exit $failed
