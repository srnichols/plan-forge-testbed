I approached this slice as the plan prescribed — verification-first rather than rewriting from scratch. All twenty-one files were already scaffolded at HEAD, so the real work was a contract alignment pass comparing each client SDK file against its server controller counterpart.

The most important finding was a routing bug in TimeEntriesApi. The client used hyphenated URLs ("api/time-entries") but the ASP.NET Core controller uses `[Route("api/[controller]")]` which resolves to "api/timeentries" without the hyphen. This would have caused 404s at runtime. All five URL references were corrected.

The second fix was adding the `sealed` modifier to all five API implementation classes to match the contract specification for primary-constructor-based service types.

I deliberately chose not to add navigation properties (like `Projects` on `ClientDto` or `Client` on `InvoiceDto`) even though the server domain models include them. System.Text.Json deserialization silently ignores unmapped properties, so adding them would be cosmetic. If a future slice needs those properties for UI rendering, they can be added then with proper typing.

Future slices should watch for the BillingController and ReportsController — these exist server-side but have no client SDK counterparts yet. Also, ProjectsApi exists client-side but has no matching controller — the server likely serves projects through nested routes on clients. These gaps may need resolution in later UI slices.

Key files a later slice will touch: `ServiceCollectionExtensions.cs` when adding new API pairs, and the `Models/` directory when the Blazor UI needs richer DTOs.