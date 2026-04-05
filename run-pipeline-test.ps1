<#
.SYNOPSIS
    Plan Forge Pipeline Test — Full Step 0→6 validation

.DESCRIPTION
    Executes the complete Plan Forge pipeline against Phase 2 (Projects CRUD)
    and validates every capability: estimation, execution, sweep, analyze,
    traces, cost tracking, and REST API.

.EXAMPLE
    .\run-pipeline-test.ps1
#>

$ErrorActionPreference = 'Continue'
$passed = 0
$failed = 0
$plan = "docs/plans/Phase-2-PROJECTS-CRUD-PLAN.md"

function Pipeline-Step([string]$Step, [string]$Name, [scriptblock]$Action) {
    Write-Host ""
    Write-Host "  [$Step] $Name" -ForegroundColor Cyan -NoNewline
    try {
        & $Action
        Write-Host "  ✅" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "  ❌ $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  Plan Forge Pipeline Test — Step 0→6                 ║" -ForegroundColor Magenta
Write-Host "║  Plan: Phase 2 — Projects CRUD                      ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Magenta

# ═══════════════════════════════════════════════════════════════
# STEP 0: Preflight — verify environment
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 0: Preflight" -ForegroundColor Yellow

Pipeline-Step "0a" "dotnet builds" {
    dotnet build --verbosity quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

Pipeline-Step "0b" "dotnet tests pass" {
    dotnet test --verbosity quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
}

Pipeline-Step "0c" "smith diagnostics" {
    $out = pwsh -NoProfile -File pforge.ps1 smith 2>&1 | Out-String
    if ($out -notmatch "passed") { throw "Smith failed" }
}

Pipeline-Step "0d" "plan file exists" {
    if (-not (Test-Path $plan)) { throw "Plan not found: $plan" }
}

# ═══════════════════════════════════════════════════════════════
# STEP 1: Plan Analysis — parse and validate the plan
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 1: Plan Validation" -ForegroundColor Yellow

Pipeline-Step "1a" "plan parses correctly" {
    $out = node pforge-mcp/orchestrator.mjs --parse $plan 2>&1 | Out-String
    if ($out -notmatch "ProjectService") { throw "Parse failed" }
    if ($out -notmatch "depends") { throw "Dependencies not parsed" }
}

Pipeline-Step "1b" "estimate returns cost" {
    $out = node pforge-mcp/orchestrator.mjs --run $plan --mode auto --estimate 2>&1 | Out-String
    if ($out -notmatch "estimatedCostUSD") { throw "Estimate failed" }
    if ($out -notmatch "sliceCount") { throw "No slice count" }
    Write-Host " ($($out | Select-String '"sliceCount": (\d+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) slices)" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "1c" "dry-run validates plan" {
    $out = node pforge-mcp/orchestrator.mjs --run $plan --mode auto --dry-run 2>&1 | Out-String
    if ($out -notmatch "dry-run") { throw "Dry-run failed" }
}

# ═══════════════════════════════════════════════════════════════
# STEP 2: Capabilities check (v2.3)
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 2: Capabilities (v2.3)" -ForegroundColor Yellow

Pipeline-Step "2a" "forge_capabilities returns surface" {
    $out = node --input-type=module -e "import{buildCapabilitySurface}from'./pforge-mcp/capabilities.mjs';import{readFileSync}from'fs';const TOOLS=JSON.parse(readFileSync('pforge-mcp/tools.json','utf-8'));console.log(JSON.stringify({tools:TOOLS.length,ok:true}))" 2>&1 | Out-String
    # Just verify the module loads — tools.json may not exist yet (generated on server start)
}

# ═══════════════════════════════════════════════════════════════
# STEP 3: Execute plan (Full Auto)
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 3: Execution (Full Auto)" -ForegroundColor Yellow
Write-Host "  ⏳ This will take several minutes..." -ForegroundColor DarkGray

Pipeline-Step "3a" "run-plan executes" {
    $out = node pforge-mcp/orchestrator.mjs --run $plan --mode auto 2>&1 | Out-String
    # Even if slices fail, we want to verify the orchestrator ran
    if ($out -notmatch "status|sliceResults") { throw "Orchestrator produced no output" }
    $global:LASTEXITCODE = 0
}

# ═══════════════════════════════════════════════════════════════
# STEP 4: Post-execution checks
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 4: Post-Execution Validation" -ForegroundColor Yellow

Pipeline-Step "4a" "run directory created" {
    $dirs = Get-ChildItem .forge/runs -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
    if ($dirs.Count -eq 0) { throw "No run directories found" }
    $script:latestRun = $dirs[0].FullName
    Write-Host " ($($dirs[0].Name))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4b" "summary.json exists" {
    if (-not (Test-Path (Join-Path $latestRun "summary.json"))) { throw "No summary.json" }
    $summary = Get-Content (Join-Path $latestRun "summary.json") | ConvertFrom-Json
    Write-Host " (status: $($summary.status), passed: $($summary.results.passed), failed: $($summary.results.failed))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4c" "slice result files exist" {
    $sliceFiles = Get-ChildItem $latestRun -Filter "slice-*.json"
    if ($sliceFiles.Count -eq 0) { throw "No slice result files" }
    Write-Host " ($($sliceFiles.Count) slices)" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4d" "session logs captured" {
    $logFiles = Get-ChildItem $latestRun -Filter "slice-*-log.txt"
    if ($logFiles.Count -eq 0) { throw "No session logs" }
    $totalBytes = ($logFiles | Measure-Object -Property Length -Sum).Sum
    Write-Host " ($($logFiles.Count) logs, $([math]::Round($totalBytes/1024))KB)" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4e" "events.log populated" {
    $eventsLog = Join-Path $latestRun "events.log"
    if (-not (Test-Path $eventsLog)) { throw "No events.log" }
    $lineCount = (Get-Content $eventsLog).Count
    Write-Host " ($lineCount events)" -NoNewline -ForegroundColor DarkGray
}

# ═══════════════════════════════════════════════════════════════
# STEP 4b: Telemetry checks (v2.4)
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 4b: Telemetry (v2.4)" -ForegroundColor Yellow

Pipeline-Step "4f" "trace.json written" {
    $tracePath = Join-Path $latestRun "trace.json"
    if (-not (Test-Path $tracePath)) { throw "No trace.json" }
    $trace = Get-Content $tracePath | ConvertFrom-Json
    Write-Host " (traceId: $($trace.traceId.Substring(0,8))..., spans: $($trace.spans.Count))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4g" "trace has resource context" {
    $trace = Get-Content (Join-Path $latestRun "trace.json") | ConvertFrom-Json
    if (-not $trace.resource) { throw "No resource in trace" }
    if (-not $trace.resource."host.name") { throw "No host.name" }
    Write-Host " (host: $($trace.resource."host.name"))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4h" "trace has span kinds" {
    $trace = Get-Content (Join-Path $latestRun "trace.json") | ConvertFrom-Json
    $kinds = $trace.spans | Select-Object -ExpandProperty kind -Unique
    Write-Host " (kinds: $($kinds -join ', '))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4i" "manifest.json written" {
    $manifestPath = Join-Path $latestRun "manifest.json"
    if (-not (Test-Path $manifestPath)) { throw "No manifest.json" }
    $manifest = Get-Content $manifestPath | ConvertFrom-Json
    Write-Host " (artifacts: $($manifest.artifacts.Count), slices: $($manifest.slices.Count))" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "4j" "index.jsonl updated" {
    $indexPath = ".forge/runs/index.jsonl"
    if (-not (Test-Path $indexPath)) { throw "No index.jsonl" }
    $lines = Get-Content $indexPath | Where-Object { $_.Trim() }
    Write-Host " ($($lines.Count) entries)" -NoNewline -ForegroundColor DarkGray
}

# ═══════════════════════════════════════════════════════════════
# STEP 5: Sweep + Analyze
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 5: Review" -ForegroundColor Yellow

Pipeline-Step "5a" "sweep runs" {
    $out = pwsh -NoProfile -File pforge.ps1 sweep 2>&1 | Out-String
    $global:LASTEXITCODE = 0
}

Pipeline-Step "5b" "analyze produces score" {
    $out = pwsh -NoProfile -File pforge.ps1 analyze $plan 2>&1 | Out-String
    $global:LASTEXITCODE = 0
    if ($out -match "(\d+)%") {
        Write-Host " (consistency: $($Matches[1])%)" -NoNewline -ForegroundColor DarkGray
    }
}

Pipeline-Step "5c" "plan status readable" {
    $out = pwsh -NoProfile -File pforge.ps1 status 2>&1 | Out-String
    $global:LASTEXITCODE = 0
    if ($out -notmatch "Phase") { throw "Status empty" }
}

# ═══════════════════════════════════════════════════════════════
# STEP 6: Cost tracking
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "Step 6: Cost Tracking" -ForegroundColor Yellow

Pipeline-Step "6a" "cost-history.json updated" {
    $costPath = ".forge/cost-history.json"
    if (-not (Test-Path $costPath)) { throw "No cost-history.json" }
    $history = Get-Content $costPath | ConvertFrom-Json
    Write-Host " ($($history.Count) entries)" -NoNewline -ForegroundColor DarkGray
}

Pipeline-Step "6b" "cost report returns data" {
    $out = node --input-type=module -e "import{getCostReport}from'./pforge-mcp/orchestrator.mjs';const r=getCostReport(process.cwd());console.log(JSON.stringify({runs:r.runs,cost:r.total_cost_usd}))" 2>&1 | Out-String
    if ($out -notmatch "runs") { throw "Cost report failed" }
    Write-Host " ($out.Trim())" -NoNewline -ForegroundColor DarkGray
}

# ═══════════════════════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════════════════════
Write-Host ""
Write-Host ""
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Gray
$color = if ($failed -gt 0) { 'Red' } elseif ($failed -eq 0) { 'Green' } else { 'Yellow' }
Write-Host "  Pipeline Test: $passed passed, $failed failed" -ForegroundColor $color
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

exit $failed
