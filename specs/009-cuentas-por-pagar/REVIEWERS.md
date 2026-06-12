# Review Guide: Módulo Cuentas por Pagar (CxP)

**Generated**: 2026-06-10 | **Spec**: [spec.md](spec.md)

## Why This Change

The store currently has no consolidated view of what it owes to suppliers. The operator manually tracks accounts payable across currencies, estimated vs. real shipping costs to CR, and net financial position — a process that is error-prone and disconnected from the order workflow. When an order is activated or delivered, there is no automatic record of the supplier obligation, so the operator must cross-reference orders and payments manually to arrive at a financial position. This module replaces that manual process with an automated, period-based tracking system.

## What Changes

A new **Cuentas por Pagar** module is added to the application. It introduces two new database tables (`PeriodControl`, `CxPEntry`), a new field on `Order` (`ActualShippingAmountToCR`), and a full-stack feature: `ICxPService` with all financial indicator logic, a new Razor Page at `/CxP/Index`, and automatic CxP entry creation hooked into the existing order transition flow. The operator gains a financial panel showing 9 indicators (including the key **Posición** metric), per-currency entry tables, editable period fields (exchange rate, payments made, funds on-hand), and a month-close operation that carries forward the outstanding balance to the next period. There are no breaking changes to existing pages: `TransitionOrderDto` is extended with an optional parameter that defaults to null for all existing callers.

## How It Works

The implementation follows the project's established layered pattern:

**Data layer** (`Phase A`): Two new EF Core entities — `PeriodControl` (one per org per month, carries exchange rate and manual fields) and `CxPEntry` (individual AP line items with type, currency, amount, and optional order reference). `PeriodControl` gets the project's standard `OrganizationId` global query filter; `CxPEntry` inherits org isolation by always being accessed through its parent period. A single EF migration (`AddCxPModule`) handles all schema changes including the new `Orders.ActualShippingAmountToCR` column.

**Service layer** (`Phase B`): `ICxPService` / `CxPService` is registered as `Scoped` and handles all business logic: period lifecycle (init, close, rollover), all 9 indicator calculations (including calling `IPaymentService.GetSaldosReportAsync()` for client balances and querying Active orders for pending shipping), entry CRUD, and the `ExchangeRate = 0` warning flag. It intentionally does NOT depend on `IOrderService` to avoid circular dependencies.

**Order integration** (`Phase C + D`): `ICxPService` is injected into `OrderService`. In `TransitionOrderAsync`, after the existing status-change save: activation triggers `CreateAutoEntryAsync(..., "AutoActiva")` with the supplier total; delivery captures the real shipping amount from the dialog, sets `order.ActualShippingAmountToCR`, and triggers `CreateAutoEntryAsync(..., "AutoDelivered")`. If no open period exists, the order transition completes successfully and a warning is logged — AP creation is a soft side-effect, not a gate.

**UI** (`Phase E + F`): A single Razor Page (`Pages/CxP/Index.cshtml` + `.cs`) with AJAX handlers following the project's `OnGet{X}Async` / `OnPost{X}Async` pattern. The JS (`wwwroot/js/pages/cxp/index.js`) calls `loadPeriod()` and `loadEntries()` on page load and after every write. A new sidebar section `menuCxP` is added to `_Layout.cshtml`.

**Period close** (`Phase F`): Closing marks the period `IsClosed=true`, reads `Configuration.ExchangeRate` for the new period default, creates the next `PeriodControl`, and posts a `SaldoAnterior` entry in the local currency (`Configuration.LocalCurrencyId`) equal to the final "Deuda a Pagar". Closed periods are immutable but preserved indefinitely.

## When It Applies

**Applies when**:
- An organization is active (session established) and navigates to `/CxP`
- An order is transitioned to Active or Delivered status (auto-entry hook fires)
- A `PeriodControl` record with `IsClosed = false` exists for the organization

**Does not apply when**:
- The operator wants to view historical closed periods — UI for browsing past periods is out of scope in this version (data is preserved in DB)
- An order is voided/cancelled — no automatic reversal of AutoActiva entries; operator deletes manually if needed
- The org has no open period during an order transition — the transition still completes; CxP entry is skipped silently with a log warning
- Multi-currency conversion for "Saldos por Cobrar" — all client balances are assumed to already be in Colones (system assumption)

## Key Decisions

1. **Hook point: `OrderService.TransitionOrderAsync`** — Auto-entries are created inside the existing order service after its `SaveChangesAsync()`, mirroring how `Transaction` records (customer charges) are already created there. Alternative (domain events / mediator) was rejected as over-engineered for this project scale.

2. **Dependency direction: `IOrderService → ICxPService` (not the reverse)** — Avoids circular dependency. `CxPService` reads Active orders directly from `DbContext` for the "Shipping CR Pendientes" indicator rather than going through `IOrderService`.

3. **`CxPEntry` has no `OrganizationId` / global query filter** — Entries are always accessed through `PeriodControl`, which already has the org filter. Adding a redundant `OrganizationId` to `CxPEntry` would add noise without safety benefit.

4. **`ActualShippingAmountToCR` is non-nullable (default 0)** — Zero is semantically unambiguous for an order that hasn't been delivered. Nullable would create an unnecessary "unknown vs. zero" distinction.

5. **`TransitionOrderDto` extended backward-compatibly** — The new `decimal? ActualShippingAmountToCR = null` parameter is appended at the end of the positional record. All existing callers remain valid without changes.

6. **Local currency identity via `Configuration.LocalCurrencyId`** — Uses the same field already leveraged by `OrderService.BuildTransaction()`. No brittle name-string lookups.

7. **"One open period" invariant enforced in service, not DB** — A unique DB constraint on `(OrganizationId, Month, Year)` prevents duplicate periods, but the "at most one `IsClosed = false`" invariant is enforced in `InitializePeriodAsync` and `ClosePeriodAsync`. A DB partial unique index was considered but rejected as unnecessary complexity at this scale.

## Areas Needing Attention

- **Error handling for auto-entries in `TransitionOrderAsync`**: The spec requires that if `CxPService.CreateAutoEntryAsync` throws, the order transition still completes. The implementation must catch the exception, log it, and swallow it. Reviewers should verify this is actually try/caught rather than simply "no open period → skip" (the two failure modes need separate handling).

- **`ExchangeRate = 0` warning path**: Added during plan review (spec edge case). The DTO includes `bool ExchangeRateWarning` and the JS renders a Bootstrap alert. This is a new field not in the original data-model.md contracts — ensure the serialized JSON response matches what `loadPeriod()` expects.

- **Period rollover month boundary**: `ClosePeriodAsync` must handle month=12 → month=1, year+1. This is straightforward but easy to miss in implementation. Reviewers should confirm the month arithmetic.

- **`GetSaldosReportAsync()` scope**: The "Saldos por Cobrar" indicator calls the existing payment service. That query returns all client balances for the org — its scope may include balances from multiple currencies (though the assumption is they're all in Colones). If the payment service ever returns multi-currency balances, this indicator will silently sum mixed units.

## Open Questions

No open questions identified. All 4 clarification questions from the spec session (2026-06-10) were resolved:
- Deletion scope: all entry types deletable from open period
- First period initialization: inline form on `/CxP` page
- Reactivation guard: not needed (orders are unidirectional)
- Exchange rate source: `Configuration.ExchangeRate` field

---

## Review Checklist

- [ ] Key decisions are justified
- [ ] `TransitionOrderAsync` auto-entry failure is caught and swallowed (order transition never blocked by CxP errors)
- [ ] `ExchangeRate = 0` path returns `ExchangeRateWarning: true` and the JS renders the alert
- [ ] Month rollover (Dec → Jan) is handled correctly in `ClosePeriodAsync`
- [ ] `PeriodControl` global query filter wired correctly in `MariCamiStoreContext`
- [ ] No `OrganizationId` filter on `CxPEntry` (by design — access is always through period)
- [ ] `ActualShippingAmountToCR` column migration is non-nullable with `DEFAULT 0`
- [ ] Scope matches the stated boundaries (no historical period browsing UI, no auto-reversal on void)
- [ ] Success criteria SC-001 (< 5s panel) and SC-004 (< 2s field save) verified manually post-implementation

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
<!-- Each phase section follows the template below -->

<!--
## Phase N: [Phase Name] (YYYY-MM-DD)

### What Changed

[Summary of files and functionality added/modified]

### Spec Compliance

[Which requirements this phase addresses, compliance score]

### Focus Areas for Review

[Where the reviewer should concentrate]

### AI Assumptions

[Decisions made during implementation that were not in the spec]
-->
