# Implementation Plan: Filtro de Organización en Payments

**Branch**: `015-filtro-organizacion-pagos` | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/015-filtro-organizacion-pagos/spec.md`

## Summary

Add an Organization filter combo to the Payments page ("Todas" first, then all organizations, defaulting to the session's current org). Extend `PaymentService`/`IPaymentService` so balances and payment registration can target an **explicit** organization id instead of always using `ICurrentOrganizationService` (the session org). "Todas" is represented as `null` (no explicit org): balances stay global (current `IgnoreQueryFilters()` behavior, unchanged) and the register-payment action is disabled client-side and rejected server-side. No schema changes.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery)

**Primary Dependencies**: EF Core 10 (SQL Server), AdminLTE 3 / Bootstrap 4

**Storage**: No schema change. `Transaction.OrganizationId` already exists; queries gain an explicit-org variant alongside the existing session-scoped and `IgnoreQueryFilters()` paths.

**Testing**: No automated test project; `dotnet build` + manual [quickstart.md](quickstart.md)

**Target Platform**: Azure App Service; no migration needed for this feature

**Project Type**: Single web application (`MariCamiStore/`)

**Constraints**: Session's active organization (`ICurrentOrganizationService`) MUST NOT be mutated by this filter (FR-004); server MUST reject a payment without an explicit organization even if called directly (FR-013)

**Scale/Scope**: Tens of organizations; single page (`Payments/Index`)

## Constitution Check

`.specify/memory/constitution.md` is the unfilled template — no ratified principles. Gate: **PASS**. Post-design: **PASS** (no new projects/dependencies; extends existing service/page pattern used by CxP and Orders).

## Project Structure

### Documentation (this feature)

```text
specs/015-filtro-organizacion-pagos/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/payments-handlers.md
└── tasks.md
```

### Source Code

```text
MariCamiStore/
├── Services/
│   ├── IPaymentService.cs   [MODIFY] add Guid? organizationId param to balance/register methods
│   └── PaymentService.cs    [MODIFY] explicit-org query paths; reuse existing IgnoreQueryFilters pattern
├── Pages/Payments/
│   ├── Index.cshtml.cs      [MODIFY] handlers take organizationId; expose organizations + session org id to the page
│   └── Index.cshtml         [MODIFY] Organization combo above "Registrar Pago"; disabled button + legend
└── wwwroot/js/pages/payments/
    └── index.js             [MODIFY] read/send organizationId; toggle button+legend; abort stale requests on filter change
```

**Structure Decision**: Single existing web project; in-place modifications to the Payments vertical slice only. No new files besides docs.

## Design Decisions

### D1 — "Todas" representation (resolves review finding #3)

- Client sends `organizationId: null` (JS: `""` → `null` before serializing) when "Todas" is selected, a real GUID string otherwise. Never `Guid.Empty` as a sentinel (an actual `Organization.Id` could theoretically collide, however unlikely; `null` is unambiguous and idiomatic for a nullable `Guid?`).
- Handlers/service signatures use `Guid? organizationId`. `null` = "Todas" (global scope for reads; rejected for the register action per FR-013).

### D2 — Service changes (`IPaymentService` / `PaymentService`)

- `GetSaldosReportAsync(Guid? organizationId)`:
  - `organizationId == null` → unchanged: `IgnoreQueryFilters()`, no org predicate (global, FR-007).
  - `organizationId.HasValue` → `IgnoreQueryFilters().Where(t => t.OrganizationId == organizationId.Value)` (FR-005). Same grouping/shape as today; only the transaction source set changes.
- `GetCustomerBalanceAsync(Guid customerId, Guid? organizationId)`:
  - `GlobalBalance`: unchanged (`CalcBalanceAsync(customerId, ignoreOrgFilter: true)`, all orgs, FR-006).
  - `OrgBalance`: when `organizationId` has a value, compute with an explicit-org variant of `CalcBalanceAsync` (`IgnoreQueryFilters().Where(t => t.CustomerId == customerId && t.OrganizationId == organizationId.Value)`) instead of relying on the EF query filter (which is pinned to the *session* org, per review finding #2). When `organizationId == null` ("Todas"), `OrgBalance` MUST equal `GlobalBalance` (FR-008) — reuse the same all-orgs sum.
  - `CalcBalanceAsync` gets a third private overload/parameter `Guid? explicitOrganizationId` so the three cases (session org via EF filter — no longer used by this page but kept for any other caller, all orgs, explicit org) share one small helper.
- `RegisterPaymentAsync(Guid customerId, decimal amount, Guid? organizationId)`:
  - `organizationId == null` → reject: `(null, "Seleccione una organización específica para registrar el pago.")` (FR-013). No transaction created.
  - `organizationId.HasValue` → create the `Transaction` with `OrganizationId = organizationId.Value` instead of `currentOrg.OrganizationId` (FR-010). Everything else (currency from Configuration, CR timezone date, description) unchanged.
  - Return type becomes `(bool Success, string? Error, CustomerBalanceDto? Balance)` so the page can distinguish "validation error" from "customer not found" (today both paths return differently-shaped results; keep the customer-not-found message as-is: "Cliente no encontrado.").

### D3 — Page handlers (`Pages/Payments/Index.cshtml.cs`)

- `OnGetAsync`: also load `await organizationService.GetOrganizationsAsync()` into `ViewData["Organizations"]` and expose the session's current org id via `ViewData["CurrentOrganizationId"] = currentOrg.OrganizationId` for the default-selected `<option>` (FR-003). Inject `IOrganizationService` (already registered in DI).
- `OnGetBalanceAsync(Guid customerId, Guid? organizationId)` → passes `organizationId` through.
- `OnGetSaldosAsync(Guid? organizationId)` → passes `organizationId` through.
- `OnPostRegisterPaymentAsync([FromBody] PaymentRequest request)`: `PaymentRequest` gains `Guid? OrganizationId`; if `null`, short-circuit with the FR-013 message before calling the service (defense in depth, service also checks) — same "double validation" pattern already used for CxP (client `min`, handler check, service check).

### D4 — Page markup (`Pages/Payments/Index.cshtml`)

- New `<div class="form-group">` above the "Registrar Pago" card: `<select id="payment-org-filter" class="form-control">` with `<option value="">Todas</option>` first, then `@foreach` over `ViewData["Organizations"]` rendering `<option value="@o.Id" selected="@(o.Id == currentOrgId)">@o.Name</option>` (server-rendered default selection, no extra round trip — same approach as `OrderStatus` checkboxes in Orders/Index).
- Below/near `#btn-register-payment`, a `#payment-org-required-hint` `<small class="text-muted">` with the FR-011 legend text, hidden by default (shown via JS when filter = "Todas").

### D5 — JS (`wwwroot/js/pages/payments/index.js`)

- `function currentOrgFilter() { var v = $('#payment-org-filter').val(); return v ? v : null; }`.
- `updateRegisterButtonState()`: toggles `#btn-register-payment` `disabled` and `#payment-org-required-hint` visibility based on `currentOrgFilter() === null` (FR-011/FR-012/FR-014). Called on load and on filter `change`.
- Stale-request protection (resolves review finding #4): keep a single in-flight `saldosRequest`/`balanceRequest` jqXHR per concern; `.abort()` the previous one before issuing a new one on filter `change`, mirroring the pattern already used in `wwwroot/js/pages/cxp/index.js` (`closePreviewRequest.abort()`). `onError`/`.fail()` ignores `statusText === 'abort'`.
- `loadSaldos()` and the balance `$.get` add `organizationId=` (encoded, omitted/empty when "Todas") to the query string; `registerPayment` payload adds `organizationId: currentOrgFilter()`.
- On `#payment-org-filter` `change`: `updateRegisterButtonState()`, reload saldos, and — if a customer is already selected (`#payment-customer` has a value) — reload the balance card (FR-009, Story 2 scenario 4).

## Complexity Tracking

None; this stays within the existing Payments vertical slice and reuses established patterns (`IgnoreQueryFilters` + explicit predicate, abort-stale-request).
