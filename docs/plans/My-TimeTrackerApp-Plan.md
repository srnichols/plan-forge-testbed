# My TimeTracker App — Plan

> This is my raw write-up of everything I want the app to do. It is **not** hardened yet.
> I run pforge **Step 1 (pre-flight)** and **Step 2 (harden)** on this doc to turn it into the
> per-phase, multi-slice execution plans (`Phase-1-*-PLAN.md` … `Phase-6-*-PLAN.md`).
>
> **Status**: Draft (pre-harden) — expect rough edges, open questions, and `[NEEDS CLARIFICATION]` markers.
> **Pipeline**: this is the **S1 "Draft Plan"** input → Harden → hardened phase plans.

---

## What I'm building

A small time-tracking + billing app called **TimeTracker**. Consultants log hours against client
projects, and the app turns those hours into invoices. There's a REST API for everything and a web
UI on top of it. Stack is .NET 10 / C# / ASP.NET Core, with a Blazor front-end. Database is
Postgres normally, but it should still run with no database for demos (in-memory is fine).

I want to build this in phases so each chunk is testable on its own. Roughly:

1. Clients
2. Projects
3. Invoicing
4. Time entry reporting
5. Dashboard summary
6. Web UI

Below is everything I can think of for each. Some of it is half-formed — that's what the harden
step is for.

---

## Phase 1 — Clients

Basic building block. A client is who we bill.

- CRUD for clients: create, read, update, "delete".
- A client has at least: name, email, an hourly rate, and an active/inactive flag.
- Don't hard-delete — just mark inactive so old invoices still make sense.
- Validate the obvious stuff: name required, email looks like an email, rate can't be negative.
- Should have unit tests for the service and some kind of API test.
- `[NEEDS CLARIFICATION]` do we need unique emails per client? I think yes but not sure.

## Phase 2 — Projects

Work happens on projects, and projects belong to a client.

- CRUD for projects, each linked to a client.
- A project has a name, optional description, the client it belongs to, and active/inactive.
- Be able to list projects filtered by client.
- Same soft-delete idea as clients.
- No duplicate project names under the same client.
- Tests like Phase 1.
- The controller should stay thin — actual logic lives in a service. (I keep seeing DbContext get
  jammed straight into controllers and I don't want that here.)

## Phase 3 — Invoicing (the meaty one)

Turn logged hours into a proper invoice with line items and money math.

- Generate an invoice for a client over a date range.
- Invoice has line items (one per project), a subtotal, discount, tax, and a grand total.
- Rate tiers: normal hours at the client's rate, but overtime (more than 8h in a day) and weekend
  work bill at 1.5x. `[NEEDS CLARIFICATION]` is weekend stacked on top of overtime or just whichever
  is higher? Need to decide.
- Volume discounts: more hours = bigger discount. Something like 40h→5%, 80h→10%, 160h→15%.
- Tax per client, some clients are tax-exempt. Default maybe 0%? The demo data uses 8.5%.
- Money math has to be exact — use banker's rounding so totals don't drift by a cent.
- Invoice lifecycle: Draft → Issued → Paid, and Void from Draft/Issued with a reason. Can't pay a
  draft, can't un-void, that sort of thing.
- Don't let someone generate two invoices for the same client + overlapping dates.
- Endpoints: generate, get one, list by client, issue, pay, void.
- Lots of tests — every rate tier, every discount bracket, tax, the rounding edge cases, bad
  transitions, empty period, inactive client.
- **Demo seed data**: when running locally I want the DB pre-loaded with a couple of clients,
  their projects, a week of time entries, and a generated invoice each, so the UI isn't empty on
  first run. Only in dev — production should start empty. This can only really exist once invoices
  exist, so it lands here in Phase 3, not earlier.

## Phase 4 — Time entry reporting

Reporting/rollups on the hours themselves (separate from invoicing).

- Some kind of reporting endpoints over time entries — totals by project, by client, billable vs
  non-billable, maybe by date range.
- `[NEEDS CLARIFICATION]` exact report shapes are fuzzy. At minimum: hours per project and
  billable/non-billable split. The rest can be decided during harden.
- Basic CRUD for time entries themselves if it's not already covered (project, date, hours,
  description, billable flag). Hours should be sane (e.g. 0 < hours ≤ 24).

## Phase 5 — Dashboard summary

One endpoint that gives the "at a glance" numbers so the UI doesn't have to make 5 calls.

- `GET /api/dashboard` returns aggregate counts/totals: total clients, total projects, total time
  entries, total hours, billable vs non-billable hours, total invoices, outstanding invoice total.
- Only count active clients/projects.
- Outstanding = invoices that are Draft or Issued (not Paid, not Void).
- Empty database should return all zeros, not blow up.
- Same layering rule: controller → service → data. Service behind an interface.

## Phase 6 — Web UI

Front-end on top of the API. This is where I most want pforge to prove it can do UI without making
a mess (no DbContext in .razor files, real separation, accessible, tested).

- Blazor app that talks to the API through a typed HttpClient SDK — not raw HttpClient calls
  scattered through pages.
- Pages: Dashboard, Clients (list + edit), Projects (list + edit), Time Entries (list + create),
  Invoices (list + detail + generate + status actions).
- Split markup and code — `.razor` for layout, `.razor.cs` code-behind. No logic buried in markup.
- The invoice detail page should show line items, the subtotal/tax/total breakdown, the status, and
  the right action buttons for the current status (Issue / Mark Paid / Void-with-reason). Paid and
  Void are dead ends.
- On the invoice detail page, show the **client's name**, not just a client id number — I got
  burned by this, an invoice that says "Client #2" is useless to a human.
- A generate page: pick a client + date range, create the draft, then drop me on the detail page.
- Use Fluent UI components. Make dropdowns actually work — I hit a thing where a select's options
  got clipped because the layout wasn't full height. The page/layout needs a proper full-height
  CSS chain so overlays aren't cut off.
- bUnit tests for the pages.
- `[NEEDS CLARIFICATION]` auth? For now no login — everything is open. Note it so we don't pretend
  it's secure.

---

## Cross-cutting things I care about

- **Architecture**: strict Controller → Service → data access. Controllers are HTTP-only. No
  business logic or DbContext in controllers or razor pages. Services sit behind interfaces so they
  can be unit tested and swapped.
- **Async everywhere** with CancellationToken on the async methods.
- **Tests**: business logic gets unit tests; APIs get integration tests; UI gets bUnit tests. I'd
  rather TDD the tricky money logic in Phase 3.
- **No surprise dependencies** — keep the NuGet footprint small.
- **Runs without Docker** for demos (in-memory DB fallback), but Postgres when a connection string
  is set.

## Open questions to resolve during harden

- `[NEEDS CLARIFICATION]` Unique client email — enforce it or not?
- `[NEEDS CLARIFICATION]` Overtime vs weekend rate stacking rule.
- `[NEEDS CLARIFICATION]` Default tax rate (0%? per-client only?).
- `[NEEDS CLARIFICATION]` Exact Phase 4 report shapes.
- `[NEEDS CLARIFICATION]` Auth — confirm it's intentionally out of scope for now.

---

## What I expect after hardening

Running Step 1 + Step 2 on this should give me six hardened plans — one per phase — each with a
scope contract (in/out/forbidden), numbered execution slices, explicit file lists, and a validation
gate per slice. Those hardened plans are the things that actually get executed. This doc is just the
starting point.
