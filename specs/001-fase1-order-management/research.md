# Research: Fase 1 — Sales & Order Management System

**Date**: 2026-06-03

---

## Decision 1: Multi-Tenancy Pattern

**Decision**: `ICurrentOrganizationService` (scoped) injected into `MariCamiStoreContext` via constructor; reads `OrganizationId` from `IHttpContextAccessor` session.

**Rationale**: EF Core Global Query Filters require the filter value at DbContext construction time. A scoped service that reads from the HTTP session is the standard pattern — it keeps the DbContext decoupled from HTTP primitives while allowing the filter to be resolved per-request. Using `IHttpContextAccessor` directly in the DbContext would couple the persistence layer to ASP.NET Core.

**Alternatives considered**:
- Direct `IHttpContextAccessor` in DbContext — rejected: couples persistence to HTTP
- Manual `.Where(x => x.OrganizationId == orgId)` in every service — rejected: error-prone, easy to forget

**Implementation note**: Register `ICurrentOrganizationService` as Scoped. Register `IHttpContextAccessor` via `AddHttpContextAccessor()`. The service returns `Guid.Empty` when no org is selected; the GQF with `Guid.Empty` returns zero results (safe default).

---

## Decision 2: Razor Pages AJAX Handler Pattern

**Decision**: Named Razor Page handlers (`OnGetLoadAsync`, `OnPostInsertAsync`, `OnPostUpdateAsync`, `OnPostDeleteAsync`) serve as the AJAX endpoints for jsGrid.

**Rationale**: The existing `Currencies/Index` page already uses this pattern (`OnGetCurrenciesAsync`). Staying consistent with the existing codebase. No separate API controllers needed. jsGrid maps its four operations (loadData, insertItem, updateItem, deleteItem) to GET/POST calls with `?handler=` query string.

**Alternatives considered**:
- Separate API Controllers — rejected: adds project complexity, inconsistent with existing code
- Minimal API endpoints — rejected: not consistent with existing Razor Pages architecture

**jsGrid URL pattern**:
```
loadData:   GET  /PageRoute?handler=Load
insertItem: POST /PageRoute?handler=Insert  (+ AntiForgeryToken)
updateItem: POST /PageRoute?handler=Update  (+ AntiForgeryToken)
deleteItem: POST /PageRoute?handler=Delete  (+ AntiForgeryToken)
```

---

## Decision 3: Transaction Automation — Where to Put the Logic

**Decision**: Transaction automation lives in `OrderService.TransitionOrderAsync()`. The method validates the transition, writes `OrderStatusHistory`, and inserts Transaction records — all in one `SaveChangesAsync()` call.

**Rationale**: Keeping all side effects of a status transition in one atomic service method ensures consistency. If the transaction insert fails, the status change is also rolled back. Domain events would be cleaner architecturally but add significant complexity for a Phase 1 build.

**Alternatives considered**:
- Domain events (MediatR) — rejected: adds dependency and ceremony not needed at this scale
- Separate `TransactionService` called from Razor Page — rejected: no atomicity guarantee

---

## Decision 4: Real-Time Recalculation Strategy

**Decision**: Pure client-side JavaScript recalculation on every `input` event in the OrderItems editor. Server receives the final computed values on save.

**Rationale**: Eliminates server round-trips and perceived latency. The formulas are simple arithmetic; no risk of client/server divergence since the server trusts the submitted values (all fields are user-editable by design). Server recalculates `ShippingAmountToCR` = Sum(EstimateShipping) on save to prevent stale totals.

**Formulas implemented in JS**:
```js
ListPriceTax = ListPrice * (TaxPercentage / 100)
AgreedPriceInLocal = (ListPrice + ListPriceTax) * ExchangeRate + ServiceFeeInLocal
// Order header (aggregated from all items in memory):
TotalWithoutTaxes = Sum(RealPrice)
TaxesAmount = Sum(ListPriceTax)
ShippingAmountToCR = Sum(EstimateShipping)
TotalToPayToSupplier = TotalWithoutTaxes + TaxesAmount + ShippingAmountIntern - DiscountAmount
TotalOfTheOrder = TotalToPayToSupplier + ShippingAmountToCR
EstimatedProfitInLocal = Sum(AgreedPriceInLocal) - TotalOfTheOrder * ExchangeRate
```

---

## Decision 5: OrderStatusHistory vs. Fields on Order

**Decision**: New `OrderStatusHistory` entity — one row per transition — instead of storing dates as fields on the `Order` entity.

**Rationale**: The spec requires recording a history of transitions with date, notes, and justification per step. Storing these as fields on `Order` would require nullable date fields for each status (RequestedAt, DeliveringAt, DeliveredAt, CompletedAt, VoidedAt) and a Justification field — messy schema. A history table is cleaner, extensible, and gives a full audit trail.

**Schema**: `Id`, `OrderId` (FK), `FromStatus`, `ToStatus`, `TransitionDate`, `Notes` (nullable), `Justification` (nullable, required only for Voided), `CreatedAt`

---

## Decision 6: Session Setup

**Decision**: Use ASP.NET Core built-in session (`AddSession` / `UseSession`). Store `OrganizationId` as a string (GUID) in session key `"ActiveOrganizationId"`.

**Rationale**: No authentication system means no user identity. Session is the simplest way to persist the active organization across requests. The session is in-memory by default (acceptable for Phase 1 / single-server dev).

**Note for future**: If deployed to Azure Container Apps with multiple instances, session will need to be backed by distributed cache (Redis). Out of scope for Phase 1.
