# Phase 6: Blazor Server Web UI (TimeTracker.Web)

> **Pipeline Step**: 2 — Hardened
> **Status**: Ready for execution
> **Author**: AI Agent (Step 0 — Specifier, Step 2 — Hardener)
> **Created**: 2026-05-05
> **Hardened**: 2026-05-05
> **Feature Branch**: `feature/phase-6-web-ui`

---

## Why This Plan Exists

Phases 1–5 built a complete .NET 10 REST API (Clients, Projects, Time Entries, Invoices, Dashboard). Until now the testbed has had no front-end — so it has only proven that pforge can build *backend* code well. Most teams' UI code is where vibe-coding wins on speed and loses on quality: `DbContext` injected into `.razor` files, no separation of concerns, no accessibility, no tests.

This phase exists to demonstrate the opposite: **pforge can build a non-trivial Blazor Server + Microsoft Fluent UI front-end that passes an enterprise-grade code review** — strict layering (page → typed HttpClient → REST API), full accessibility (WCAG 2.1 AA), bUnit-tested components, and zero `DbContext` reach-throughs.

The new guardrails it relies on (added to the `dotnet` preset in Plan-Forge v2.86.0+):
- `.github/instructions/blazor-fluent-ui.instructions.md` — auto-loads on `*.razor` edits
- `.github/agents/blazor-reviewer.agent.md` — read-only reviewer agent
- `.github/skills/ui-scaffold/SKILL.md` — opinionated scaffolder

---

## Scope Contract

### In Scope
- New project `src/TimeTracker.Web` — Blazor Server host with Microsoft Fluent UI 4.x
- New project `src/TimeTracker.Web.Client` — typed HttpClient SDK over the REST API (NOT a Blazor WASM project; it's a plain `Microsoft.NET.Sdk` class library)
- New test project `tests/TimeTracker.Web.Tests` — bUnit + xUnit
- Pages: Dashboard, Clients (list + edit), Projects (list + edit), Time Entries (list + create), Invoices (list view)
- Layout shell: `MainLayout.razor`, `NavMenu.razor`, `App.razor`, `Routes.razor`, page title, breadcrumbs
- Solution wiring (`TimeTracker.slnx` updated)
- README quick-start updated with `dotnet run --project src/TimeTracker.Web`

### Out of Scope — DO NOT TOUCH
- Existing `src/TimeTracker.Api/**` — controllers, services, DbContext, models stay exactly as they are
- Existing `src/TimeTracker.Core/**` — model classes are reused as-is via project reference
- Existing `tests/TimeTracker.Tests/**` — backend tests untouched
- Database schema or migrations
- Authentication / authorization (project has none today; not adding it here)
- Docker / deployment files
- All other `docs/plans/*` files except `DEPLOYMENT-ROADMAP.md`

### Forbidden Actions
- Do NOT inject `TimeTrackerDbContext` into any `.razor` or `.razor.cs` file. The Web app talks **only** to the REST API via the typed client. (See `.github/instructions/blazor-fluent-ui.instructions.md` Layering Rule.)
- Do NOT add a project reference from `TimeTracker.Web` to `TimeTracker.Api` (Web only references `TimeTracker.Core` and `TimeTracker.Web.Client`).
- Do NOT add Bootstrap, MudBlazor, Tailwind, or any UI framework other than `Microsoft.FluentUI.AspNetCore.Components`.
- Do NOT modify any existing `*.cs` file under `src/TimeTracker.Api/` or `src/TimeTracker.Core/`.
- Do NOT modify `docker-compose.yml`, `Dockerfile`, `appsettings.json` of the Api.
- Do NOT use `static` fields for shared component state (Blazor Server shares statics across users).
- Do NOT use `@inject` directives in code-behind files — use `[Inject]` properties on the partial class.
- Do NOT bind `EditForm` directly to entity types from `TimeTracker.Core.Models` — always go through a Web-project DTO/form model.

### Files Created (Exhaustive)

#### Project files
| File | Purpose |
|------|---------|
| `src/TimeTracker.Web/TimeTracker.Web.csproj` | Blazor Server host project (net10.0, Fluent UI 4.x, ApexCharts) |
| `src/TimeTracker.Web/Program.cs` | DI wiring, Fluent UI registration, typed HttpClient registration |
| `src/TimeTracker.Web/appsettings.json` | API base URL config |
| `src/TimeTracker.Web/appsettings.Development.json` | Dev API base URL |
| `src/TimeTracker.Web/Properties/launchSettings.json` | Dev port (5100) |
| `src/TimeTracker.Web/_Imports.razor` | Common usings |
| `src/TimeTracker.Web/App.razor` | Root component |
| `src/TimeTracker.Web/Routes.razor` | Router |
| `src/TimeTracker.Web/wwwroot/app.css` | Minimal app-level CSS (theme tokens only) |
| `src/TimeTracker.Web.Client/TimeTracker.Web.Client.csproj` | Class library SDK (typed HttpClient) |

#### Typed client
| File | Purpose |
|------|---------|
| `src/TimeTracker.Web.Client/Models/ClientListItem.cs` | DTO mirroring REST response |
| `src/TimeTracker.Web.Client/Models/ClientFormModel.cs` | Form DTO with DataAnnotations |
| `src/TimeTracker.Web.Client/Models/ProjectListItem.cs` | DTO |
| `src/TimeTracker.Web.Client/Models/ProjectFormModel.cs` | Form DTO |
| `src/TimeTracker.Web.Client/Models/TimeEntryListItem.cs` | DTO |
| `src/TimeTracker.Web.Client/Models/TimeEntryFormModel.cs` | Form DTO |
| `src/TimeTracker.Web.Client/Models/InvoiceListItem.cs` | DTO |
| `src/TimeTracker.Web.Client/Models/DashboardSummaryDto.cs` | DTO mirroring `DashboardSummary` record |
| `src/TimeTracker.Web.Client/IClientsApi.cs` | Interface |
| `src/TimeTracker.Web.Client/ClientsApi.cs` | Implementation |
| `src/TimeTracker.Web.Client/IProjectsApi.cs` | Interface |
| `src/TimeTracker.Web.Client/ProjectsApi.cs` | Implementation |
| `src/TimeTracker.Web.Client/ITimeEntriesApi.cs` | Interface |
| `src/TimeTracker.Web.Client/TimeEntriesApi.cs` | Implementation |
| `src/TimeTracker.Web.Client/IInvoicesApi.cs` | Interface |
| `src/TimeTracker.Web.Client/InvoicesApi.cs` | Implementation |
| `src/TimeTracker.Web.Client/IDashboardApi.cs` | Interface |
| `src/TimeTracker.Web.Client/DashboardApi.cs` | Implementation |
| `src/TimeTracker.Web.Client/ServiceCollectionExtensions.cs` | `AddTimeTrackerClient(string baseUrl)` extension |

#### Layout + shared
| File | Purpose |
|------|---------|
| `src/TimeTracker.Web/Components/Layout/MainLayout.razor` | Top-bar + side-nav layout |
| `src/TimeTracker.Web/Components/Layout/MainLayout.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Components/Layout/NavMenu.razor` | Side navigation |
| `src/TimeTracker.Web/Components/Shared/PageHeader.razor` | Reusable page title + breadcrumb component |
| `src/TimeTracker.Web/Components/Shared/LoadingState.razor` | `<FluentProgressRing>` wrapper with aria-live |
| `src/TimeTracker.Web/Components/Shared/ErrorState.razor` | `<FluentMessageBar>` wrapper |

#### Pages
| File | Purpose |
|------|---------|
| `src/TimeTracker.Web/Pages/Dashboard.razor` | Dashboard page (markup) |
| `src/TimeTracker.Web/Pages/Dashboard.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/Clients/ClientsList.razor` | List page |
| `src/TimeTracker.Web/Pages/Clients/ClientsList.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/Clients/ClientEdit.razor` | Create/edit form |
| `src/TimeTracker.Web/Pages/Clients/ClientEdit.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/Projects/ProjectsList.razor` | List page |
| `src/TimeTracker.Web/Pages/Projects/ProjectsList.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/Projects/ProjectEdit.razor` | Create/edit form |
| `src/TimeTracker.Web/Pages/Projects/ProjectEdit.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/TimeEntries/TimeEntriesList.razor` | List page |
| `src/TimeTracker.Web/Pages/TimeEntries/TimeEntriesList.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/TimeEntries/TimeEntryCreate.razor` | Create form |
| `src/TimeTracker.Web/Pages/TimeEntries/TimeEntryCreate.razor.cs` | Code-behind |
| `src/TimeTracker.Web/Pages/Invoices/InvoicesList.razor` | List page |
| `src/TimeTracker.Web/Pages/Invoices/InvoicesList.razor.cs` | Code-behind |

#### Tests
| File | Purpose |
|------|---------|
| `tests/TimeTracker.Web.Tests/TimeTracker.Web.Tests.csproj` | bUnit + xUnit + NSubstitute |
| `tests/TimeTracker.Web.Tests/Pages/DashboardTests.cs` | Renders KPIs, loading, error |
| `tests/TimeTracker.Web.Tests/Pages/ClientsListTests.cs` | Renders grid, loading, error |
| `tests/TimeTracker.Web.Tests/Pages/ClientEditTests.cs` | Form validation, submit-disabled-while-in-flight |
| `tests/TimeTracker.Web.Tests/Pages/TimeEntryCreateTests.cs` | Form validation |
| `tests/TimeTracker.Web.Tests/Client/ClientsApiTests.cs` | Typed client serialization round-trip |

### Files Modified (Exhaustive)
| File | Change |
|------|--------|
| `TimeTracker.slnx` | Add `src/TimeTracker.Web/TimeTracker.Web.csproj`, `src/TimeTracker.Web.Client/TimeTracker.Web.Client.csproj`, `tests/TimeTracker.Web.Tests/TimeTracker.Web.Tests.csproj` |
| `README.md` | Add quick-start line for the Web project; document the new src/ layout |
| `docs/plans/DEPLOYMENT-ROADMAP.md` | Mark Phase 6 status |

---

## Specification

### Problem Statement
The TimeTracker REST API has full CRUD + reporting + invoicing capabilities, but no human-facing surface. To demonstrate that pforge produces **enterprise-grade UI** (not vibe-coded UI), we need a Blazor Server front-end that:
- Talks to the existing REST API via a typed HttpClient (no `DbContext` reach-through)
- Uses Microsoft Fluent UI exclusively for components and theming
- Renders loading, success, and error states for every async operation
- Validates forms client-side (UX) and trusts only server-side validation (truth)
- Passes WCAG 2.1 AA accessibility checks
- Has bUnit tests for every non-trivial page

### Acceptance Criteria
- [ ] `dotnet run --project src/TimeTracker.Web` starts the app on `http://localhost:5100`
- [ ] Visiting `/` redirects to `/dashboard`
- [ ] Dashboard shows the KPIs from `GET /api/dashboard` (counts, hours, outstanding invoice total)
- [ ] `/clients` lists clients from `GET /api/clients`; row "Edit" navigates to `/clients/{id}/edit`
- [ ] `/clients/new` and `/clients/{id}/edit` render an `EditForm` bound to `ClientFormModel` (NOT to `Client` entity)
- [ ] Client create/update/delete go through `POST/PUT/DELETE /api/clients`
- [ ] `/projects`, `/time-entries`, `/invoices` parallel structure
- [ ] Every page has `<PageTitle>` and a `PageHeader` with breadcrumb
- [ ] Every async lifecycle method propagates a `CancellationToken`
- [ ] Every page implements `IDisposable` with a `CancellationTokenSource` cancelled in `Dispose`
- [ ] No `.razor` or `.razor.cs` file imports `Microsoft.EntityFrameworkCore` or `TimeTracker.Api.*` namespaces
- [ ] No `.razor.cs` file references `TimeTrackerDbContext`
- [ ] `dotnet test` passes with 0 failures (existing + new bUnit tests)
- [ ] Solution builds with 0 warnings via `dotnet build`

### Edge Cases
- API unavailable → page shows `<FluentMessageBar Intent="Error">` with retry guidance, **not** a stack trace
- Empty list → page shows an empty-state component, not a blank grid
- Slow API → loading spinner, submit button disabled while in-flight (prevents double-submit)
- User navigates away during load → `OperationCanceledException` is silently swallowed (expected)
- Validation failure on server → server `ProblemDetails` surfaced as a form-level error, not a crash

### Out of Scope (this phase)
- Authentication / authorization (no user model in the testbed yet)
- Real-time updates via SignalR (Blazor Server uses SignalR for transport, but no app-level pub/sub)
- Reports page (Phase 7 candidate)
- Invoice creation / state transitions (read-only list only this phase)
- Mobile-optimized layouts (responsive defaults from Fluent UI are sufficient)

### Open Questions
_None — all behavior is determined by the existing REST API contract._

---

## Technical Approach

### Architecture (Strict 4-Layer)

```
┌────────────────────────────────────────────────┐
│ TimeTracker.Web (.razor / .razor.cs)           │  Presentation
│   ↓ [Inject] IClientsApi (etc.)                │
├────────────────────────────────────────────────┤
│ TimeTracker.Web.Client (typed HttpClient)      │  API Client SDK
│   ↓ HttpClient → http://localhost:5000         │
├────────────────────────────────────────────────┤
│ TimeTracker.Api (Controllers)                  │  HTTP Boundary  ◀── existing, untouched
│   ↓                                            │
│ TimeTracker.Api/Services                       │  Business Logic ◀── existing, untouched
│   ↓                                            │
│ TimeTracker.Api/Data (EF Core)                 │  Data Access    ◀── existing, untouched
└────────────────────────────────────────────────┘
```

### Project References (the only allowed graph)

```
TimeTracker.Web ─────► TimeTracker.Web.Client
                  └──► TimeTracker.Core         (DTO/model reuse only)

TimeTracker.Web.Client ─► TimeTracker.Core      (DTO/model reuse only)

TimeTracker.Web.Tests ─► TimeTracker.Web
                    └──► TimeTracker.Web.Client

(NO reference from TimeTracker.Web → TimeTracker.Api)
(NO reference from .Web.Client → TimeTracker.Api)
```

### Typed HttpClient Pattern

```csharp
// In Program.cs
builder.Services.AddTimeTrackerClient(builder.Configuration["Api:BaseUrl"]!);

// In ServiceCollectionExtensions.cs
public static IServiceCollection AddTimeTrackerClient(this IServiceCollection services, string baseUrl)
{
    services.AddHttpClient<IClientsApi, ClientsApi>(c => c.BaseAddress = new Uri(baseUrl));
    services.AddHttpClient<IProjectsApi, ProjectsApi>(c => c.BaseAddress = new Uri(baseUrl));
    services.AddHttpClient<ITimeEntriesApi, TimeEntriesApi>(c => c.BaseAddress = new Uri(baseUrl));
    services.AddHttpClient<IInvoicesApi, InvoicesApi>(c => c.BaseAddress = new Uri(baseUrl));
    services.AddHttpClient<IDashboardApi, DashboardApi>(c => c.BaseAddress = new Uri(baseUrl));
    return services;
}
```

### Page Component Anatomy (the only allowed shape)
- Markup file: `<PageTitle>`, `@rendermode InteractiveServer`, three render branches (loading / error / data), Fluent UI components only
- Code-behind partial class: `[Inject]` properties, single `CancellationTokenSource _cts = new();`, `OnInitializedAsync` wrapped in try/catch with `OperationCanceledException` / `Exception` / `finally`, `Dispose()` cancels `_cts`
- DTO-bound `EditForm` with `DataAnnotationsValidator`, submit button disabled while `_submitting`

See `.github/instructions/blazor-fluent-ui.instructions.md` for the canonical examples.

---

## Execution Slices

### Slice 1: Solution scaffold + Web.Client SDK [scope: src/TimeTracker.Web.Client/**, TimeTracker.slnx]

**Goal**: Lay down the typed HttpClient project with all five API interfaces and DTOs. No Web project yet, no pages, no tests for the API yet (those are Slice 6).

**Tasks**:
1. Create `src/TimeTracker.Web.Client/TimeTracker.Web.Client.csproj` (`Microsoft.NET.Sdk`, net10.0, project ref to `TimeTracker.Core`).
2. Create all 8 DTO files under `src/TimeTracker.Web.Client/Models/` (mirror REST contract, plain POCOs + DataAnnotations on FormModels).
3. Create the 5 interface + 5 implementation pairs (`IClientsApi/ClientsApi`, `IProjectsApi/ProjectsApi`, `ITimeEntriesApi/TimeEntriesApi`, `IInvoicesApi/InvoicesApi`, `IDashboardApi/DashboardApi`). Each implementation:
   - Constructor takes `HttpClient`
   - All methods accept and propagate `CancellationToken`
   - Uses `System.Net.Http.Json` (`GetFromJsonAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`)
   - Throws `HttpRequestException` on non-success status (let it bubble — pages catch and render error UI)
4. Create `src/TimeTracker.Web.Client/ServiceCollectionExtensions.cs` with `AddTimeTrackerClient(string baseUrl)`.
5. Add `src/TimeTracker.Web.Client/TimeTracker.Web.Client.csproj` to `TimeTracker.slnx` under the `/src/` folder.

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Web.Client/TimeTracker.Web.Client.csproj
node -e "const f=require('fs');['src/TimeTracker.Web.Client/IClientsApi.cs','src/TimeTracker.Web.Client/ServiceCollectionExtensions.cs'].forEach(p=>f.statSync(p));if(!f.readFileSync('src/TimeTracker.Web.Client/ServiceCollectionExtensions.cs','utf8').includes('AddTimeTrackerClient'))throw new Error('AddTimeTrackerClient missing');console.log('OK')"
```

---

### Slice 2: TimeTracker.Web project scaffold + layout shell [depends: Slice 1] [scope: src/TimeTracker.Web/**, TimeTracker.slnx]

**Goal**: Blazor Server host project with Fluent UI registered, layout shell, but no domain pages yet (only a placeholder `Index` that redirects to `/dashboard`). App must build and serve a "Hello, world" route.

**Tasks**:
1. Create `src/TimeTracker.Web/TimeTracker.Web.csproj` (`Microsoft.NET.Sdk.Web`, net10.0, package refs to `Microsoft.FluentUI.AspNetCore.Components` 4.*, `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.*, `Blazor-ApexCharts` 6.*; project refs to `TimeTracker.Core` and `TimeTracker.Web.Client`).
2. Create `Program.cs`:
   - `AddRazorComponents().AddInteractiveServerComponents()`
   - `AddFluentUIComponents()`
   - `AddTimeTrackerClient(builder.Configuration["Api:BaseUrl"]!)`
   - `MapRazorComponents<App>().AddInteractiveServerRenderMode()`
3. Create `appsettings.json` with `"Api": { "BaseUrl": "http://localhost:5000" }`; `appsettings.Development.json` mirrors with dev URL.
4. Create `Properties/launchSettings.json` with profile `TimeTracker.Web` on `http://localhost:5100`.
5. Create `App.razor`, `Routes.razor`, `_Imports.razor`.
6. Create `Components/Layout/MainLayout.razor` + `.razor.cs` (top bar with app name, side nav, content area — pure Fluent UI, NO inline `style=` for layout).
7. Create `Components/Layout/NavMenu.razor` (Dashboard, Clients, Projects, Time Entries, Invoices links — placeholder hrefs OK; pages added in later slices).
8. Create `Components/Shared/PageHeader.razor`, `LoadingState.razor`, `ErrorState.razor`.
9. Create `Pages/Index.razor` with `@page "/"` that redirects to `/dashboard`.
10. Create minimal `wwwroot/app.css` (theme tokens only — no layout CSS).
11. Add `src/TimeTracker.Web/TimeTracker.Web.csproj` to `TimeTracker.slnx`.

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Web/TimeTracker.Web.csproj
node -e "const f=require('fs');['src/TimeTracker.Web/Program.cs','src/TimeTracker.Web/Components/Layout/MainLayout.razor'].forEach(p=>f.statSync(p));const prog=f.readFileSync('src/TimeTracker.Web/Program.cs','utf8');if(!prog.includes('AddFluentUIComponents'))throw new Error('AddFluentUIComponents missing');if(!prog.includes('AddTimeTrackerClient'))throw new Error('AddTimeTrackerClient missing');console.log('OK')"
```

---

### Slice 3: Dashboard page [depends: Slice 2] [scope: src/TimeTracker.Web/Pages/Dashboard.razor, src/TimeTracker.Web/Pages/Dashboard.razor.cs]

**Goal**: First real page — proves the typed-client → Fluent UI → KPI display pipeline end-to-end. Dashboard is the highest-impact page and the simplest (read-only, no forms).

**Tasks**:
1. Create `Pages/Dashboard.razor`:
   - `@page "/dashboard"`, `@rendermode InteractiveServer`, `<PageTitle>TimeTracker — Dashboard</PageTitle>`
   - `<PageHeader Title="Dashboard" />`
   - Three branches: `<LoadingState>` while `_loading`, `<ErrorState>` when `_loadError`, otherwise a `<FluentGrid>` of KPI cards (clients, projects, hours, outstanding invoice total) using `<FluentCard>`
2. Create `Pages/Dashboard.razor.cs` partial class:
   - `[Inject] IDashboardApi DashboardApi`
   - `[Inject] ILogger<Dashboard> Logger`
   - `IDisposable`, `CancellationTokenSource _cts = new()`
   - `OnInitializedAsync` calls `DashboardApi.GetSummaryAsync(_cts.Token)`, three-branch try/catch as in instructions file
   - `Dispose()` cancels `_cts`
3. NO inline `style=` for layout. NO `static` fields. NO `DbContext` (anywhere — verified by gate).

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Web/TimeTracker.Web.csproj
node -e "const f=require('fs');['src/TimeTracker.Web/Pages/Dashboard.razor','src/TimeTracker.Web/Pages/Dashboard.razor.cs'].forEach(p=>f.statSync(p));const r=f.readFileSync('src/TimeTracker.Web/Pages/Dashboard.razor','utf8');const c=f.readFileSync('src/TimeTracker.Web/Pages/Dashboard.razor.cs','utf8');if(!r.includes('@page \"/dashboard\"'))throw new Error('@page /dashboard missing');if(!r.includes('PageTitle'))throw new Error('PageTitle missing');if(!c.includes('IDashboardApi'))throw new Error('IDashboardApi missing');if(!c.includes('CancellationTokenSource'))throw new Error('CancellationTokenSource missing');if(/DbContext|EntityFrameworkCore|TimeTracker\.Api\./.test(c))throw new Error('layer violation in Dashboard.razor.cs');console.log('OK')"
```

---

### Slice 4: Clients CRUD pages [P] [depends: Slice 2] [scope: src/TimeTracker.Web/Pages/Clients/**]

**Goal**: List + Create/Edit form. Demonstrates `FluentDataGrid` + `EditForm` + DTO binding + submit-disabled-while-in-flight.

**Tasks**:
1. Create `Pages/Clients/ClientsList.razor` + `.razor.cs`:
   - `@page "/clients"`, `<FluentDataGrid>` over `_clients`
   - "New Client" button → `/clients/new`; row Edit button → `/clients/{id}/edit`
   - Three render branches; CTS disposal pattern
2. Create `Pages/Clients/ClientEdit.razor` + `.razor.cs`:
   - `@page "/clients/new"` AND `@page "/clients/{Id:int}/edit"`
   - `[Parameter] public int? Id { get; set; }`
   - `OnParametersSetAsync` loads existing client into `_form` (a `ClientFormModel`, NOT a `Client` entity) when `Id` is set
   - `<EditForm Model="_form" OnValidSubmit="SubmitAsync">` with `<DataAnnotationsValidator />`
   - Fluent fields: `<FluentTextField Label="Name" @bind-Value="_form.Name" Required />`, etc.
   - `<ValidationMessage>` per field
   - Submit button: `Disabled="@_submitting"`, label switches to "Saving…" while in flight
   - On success: `_toast.ShowSuccess("Client saved")` then `Nav.NavigateTo("/clients")`
   - On `HttpRequestException`: surface as form-level `<FluentMessageBar Intent="Error">`

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Web/TimeTracker.Web.csproj
node -e "const f=require('fs'),p=require('path');['src/TimeTracker.Web/Pages/Clients/ClientsList.razor','src/TimeTracker.Web/Pages/Clients/ClientEdit.razor'].forEach(x=>f.statSync(x));const list=f.readFileSync('src/TimeTracker.Web/Pages/Clients/ClientsList.razor','utf8');const edit=f.readFileSync('src/TimeTracker.Web/Pages/Clients/ClientEdit.razor','utf8');if(!list.includes('FluentDataGrid'))throw new Error('FluentDataGrid missing');if(!edit.includes('EditForm'))throw new Error('EditForm missing');if(!edit.includes('DataAnnotationsValidator'))throw new Error('DataAnnotationsValidator missing');const dir='src/TimeTracker.Web/Pages/Clients';for(const fn of f.readdirSync(dir).filter(x=>x.endsWith('.razor.cs'))){const t=f.readFileSync(p.join(dir,fn),'utf8');if(/DbContext|EntityFrameworkCore|TimeTracker\.Api\./.test(t))throw new Error('layer violation in '+fn)}if(!f.readFileSync('src/TimeTracker.Web/Pages/Clients/ClientEdit.razor.cs','utf8').includes('ClientFormModel'))throw new Error('ClientFormModel binding missing');console.log('OK')"
```

---

### Slice 5: Projects + Time Entries + Invoices pages [P] [depends: Slice 2] [scope: src/TimeTracker.Web/Pages/Projects/**, src/TimeTracker.Web/Pages/TimeEntries/**, src/TimeTracker.Web/Pages/Invoices/**]

**Goal**: Parallel implementation of the remaining three domain areas, using the patterns established in Slice 4.

**Tasks**:
1. `Pages/Projects/ProjectsList.razor` + `ProjectEdit.razor` (and code-behinds) — same shape as Clients but with project fields (Name, Description, ClientId via `<FluentSelect>`, IsActive). Project edit form binds to `ProjectFormModel`.
2. `Pages/TimeEntries/TimeEntriesList.razor` + `TimeEntryCreate.razor` (and code-behinds) — list filterable by ProjectId; create form has Date (`<FluentDatePicker>`), Hours, Description, IsBillable. Bound to `TimeEntryFormModel`.
3. `Pages/Invoices/InvoicesList.razor` (and code-behind) — read-only list with status badges; no edit page this phase.

**Validation Gate**:
```bash
dotnet build src/TimeTracker.Web/TimeTracker.Web.csproj
node -e "const f=require('fs'),p=require('path');['src/TimeTracker.Web/Pages/Projects/ProjectsList.razor','src/TimeTracker.Web/Pages/Projects/ProjectEdit.razor','src/TimeTracker.Web/Pages/TimeEntries/TimeEntriesList.razor','src/TimeTracker.Web/Pages/TimeEntries/TimeEntryCreate.razor','src/TimeTracker.Web/Pages/Invoices/InvoicesList.razor'].forEach(x=>f.statSync(x));if(!f.readFileSync('src/TimeTracker.Web/Pages/Projects/ProjectEdit.razor.cs','utf8').includes('ProjectFormModel'))throw new Error('ProjectFormModel missing');if(!f.readFileSync('src/TimeTracker.Web/Pages/TimeEntries/TimeEntryCreate.razor.cs','utf8').includes('TimeEntryFormModel'))throw new Error('TimeEntryFormModel missing');for(const d of ['src/TimeTracker.Web/Pages/Projects','src/TimeTracker.Web/Pages/TimeEntries','src/TimeTracker.Web/Pages/Invoices'])for(const fn of f.readdirSync(d).filter(x=>x.endsWith('.razor.cs'))){const t=f.readFileSync(p.join(d,fn),'utf8');if(/DbContext|EntityFrameworkCore|TimeTracker\.Api\./.test(t))throw new Error('layer violation in '+d+'/'+fn)}console.log('OK')"
```

---

### Slice 6: bUnit test project + page tests [depends: Slice 3, Slice 4, Slice 5] [scope: tests/TimeTracker.Web.Tests/**, TimeTracker.slnx]

**Goal**: bUnit test project that proves every page renders the three branches (loading / data / error) correctly. Plus a typed-client serialization smoke test.

**Tasks**:
1. Create `tests/TimeTracker.Web.Tests/TimeTracker.Web.Tests.csproj`:
   - PackageRef: `bunit` (latest 1.x), `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `NSubstitute`
   - ProjectRef: `TimeTracker.Web`, `TimeTracker.Web.Client`
2. Create `Pages/DashboardTests.cs` with three tests:
   - `Renders_loading_state_initially` — service returns never-completing task; assert `<FluentProgressRing>` rendered
   - `Renders_kpis_when_summary_loads` — service returns sample `DashboardSummaryDto`; assert KPI values present in markup
   - `Renders_error_message_when_service_throws` — service throws `HttpRequestException`; assert error MessageBar rendered
3. Create `Pages/ClientsListTests.cs` — same three branches.
4. Create `Pages/ClientEditTests.cs` — at least: form renders required-validation message when Name is empty and submit attempted; submit button disabled when `_submitting`.
5. Create `Pages/TimeEntryCreateTests.cs` — validation message when Hours is 0; happy path posts to `ITimeEntriesApi.CreateAsync`.
6. Create `Client/ClientsApiTests.cs` — uses `Microsoft.AspNetCore.TestHost` or `HttpMessageHandler` mock to verify request/response serialization for one method.
7. Add the test project to `TimeTracker.slnx`.

**Validation Gate**:
```bash
dotnet build tests/TimeTracker.Web.Tests/TimeTracker.Web.Tests.csproj
dotnet test tests/TimeTracker.Web.Tests/TimeTracker.Web.Tests.csproj --verbosity quiet --logger "console;verbosity=minimal"
node -e "['tests/TimeTracker.Web.Tests/Pages/DashboardTests.cs','tests/TimeTracker.Web.Tests/Pages/ClientsListTests.cs','tests/TimeTracker.Web.Tests/Pages/ClientEditTests.cs','tests/TimeTracker.Web.Tests/Pages/TimeEntryCreateTests.cs','tests/TimeTracker.Web.Tests/Client/ClientsApiTests.cs'].forEach(p=>require('fs').statSync(p));console.log('OK')"
```

**Stop Condition**: If any test fails → STOP, do not proceed to Slice 7.

---

### Slice 7: Solution build + smoke test + docs [depends: Slice 6] [scope: TimeTracker.slnx, README.md, docs/plans/DEPLOYMENT-ROADMAP.md]

**Goal**: Whole-solution green build, smoke-test the Web app starts cleanly, refresh README and roadmap.

**Tasks**:
1. Run `dotnet build TimeTracker.slnx` and ensure 0 errors, 0 warnings.
2. Run `dotnet test TimeTracker.slnx` and ensure all tests pass (existing `TimeTracker.Tests` + new `TimeTracker.Web.Tests`).
3. Update `README.md` Quick Start section to add:
   - `dotnet run --project src/TimeTracker.Web` line, with `Browse to http://localhost:5100`
   - Brief note that the Web app talks to the API at `http://localhost:5000` and starting both is required for a full demo
   - `src/` layout diagram (Api / Core / Web / Web.Client)
4. Update `docs/plans/DEPLOYMENT-ROADMAP.md` Phase 6 entry: status ✅ Complete with link to this plan.
5. Append a brief "Phase 6: Web UI" line to README's "Phases" section if one exists.

**Validation Gate**:
```bash
dotnet build TimeTracker.slnx
dotnet test TimeTracker.slnx --verbosity quiet --logger "console;verbosity=minimal"
node -e "const f=require('fs');const r=f.readFileSync('README.md','utf8');if(!r.includes('TimeTracker.Web'))throw new Error('README missing TimeTracker.Web');if(!r.includes('5100'))throw new Error('README missing port 5100');if(!f.readFileSync('docs/plans/DEPLOYMENT-ROADMAP.md','utf8').includes('Phase 6'))throw new Error('roadmap missing Phase 6');console.log('OK')"
```

---

## Definition of Done
- [ ] All 7 slices pass their validation gates
- [ ] `dotnet build TimeTracker.slnx` produces 0 errors and 0 warnings
- [ ] `dotnet test TimeTracker.slnx` reports 0 failures across both test projects
- [ ] `dotnet run --project src/TimeTracker.Web` starts the app (manual smoke test post-execution)
- [ ] No `.razor` or `.razor.cs` file imports `Microsoft.EntityFrameworkCore`, references `TimeTrackerDbContext`, or imports any `TimeTracker.Api.*` namespace
- [ ] No `static` fields used for component state
- [ ] Every page has `<PageTitle>` and propagates `CancellationToken` through async lifecycle methods
- [ ] README + DEPLOYMENT-ROADMAP updated
- [ ] No TODO / FIXME / `throw new NotImplementedException()` markers in new code

---

## Cross-References
- Layering rules: `.github/instructions/blazor-fluent-ui.instructions.md`
- Reviewer agent (run before merging): `.github/agents/blazor-reviewer.agent.md`
- Architecture-first principles: `.github/instructions/architecture-principles.instructions.md`
- REST API contract: `src/TimeTracker.Api/Controllers/*.cs` (read-only reference; not modifiable in this phase)
