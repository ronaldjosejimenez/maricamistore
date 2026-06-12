# Review Guide: Fase 1 — Sales & Order Management System

**Generated**: 2026-06-03 | **Spec**: [spec.md](spec.md)

---

## Why This Change

MariCamiStore is a Costa Rican resale business that purchases products from international suppliers (Amazon, Shein, etc.) and sells them locally in colones. Currently there is no system to manage purchase orders, track deliveries, calculate margins, or monitor customer accounts receivable — all of this is done manually. This Phase 1 build delivers a complete operational and financial tracking system from scratch on top of an existing ASP.NET Core 10 project that already has domain models defined but no working UI or business logic.

---

## What Changes

The system gains full multi-tenant operational capability: operators can select an active organization and manage all catalogs (currencies, product types, suppliers, customers, configuration), create and manage purchase orders with real-time margin calculations, advance orders through a delivery lifecycle while the system automatically posts financial ledger entries, register client payments, and view outstanding accounts receivable. The only existing working functionality (a partial currencies list) is completed; all other screens are new. No breaking changes to the existing data model — one new entity (`OrderStatusHistory`) is added via EF migration.

---

## How It Works

The implementation follows a layered approach on ASP.NET Core 10 Razor Pages with jsGrid for AJAX CRUDs:

**Multi-tenancy**: A scoped `ICurrentOrganizationService` reads the active `OrganizationId` from the HTTP session and is injected into `MariCamiStoreContext`, which applies EF Core Global Query Filters on `Order`, `OrderItem`, and `Transaction`. `Customer` is global (no filter). A navbar dropdown sets the session org and reloads the page.

**Catalogs**: Five catalog pages (Configuration, Currency, ProductType, Supplier, Customer) use a consistent jsGrid handler pattern — four named Razor Page handlers (`OnGetLoadAsync`, `OnPostInsertAsync`, `OnPostUpdateAsync`, `OnPostDeleteAsync`) serve as the AJAX backend.

**Order lifecycle**: The Order Dashboard lists orders; items are managed in a deep-dive editor. When an operator changes any price field, client-side JavaScript recalculates all derived totals instantly without a server round-trip. On status transition, `OrderService.TransitionOrderAsync` validates the state machine, writes an `OrderStatusHistory` record, and auto-generates `Charge` or `Void` ledger transactions per item — all in one atomic database write.

**Financial ledger**: `Transaction` records are immutable (no update/delete endpoints exist). Balances are computed purely from transaction aggregates: `Sum(Charge) - Sum(Payment) - Sum(Void)` per customer. Order status has no bearing on balances.

**Reports**: Payment Registry lets operators register customer payments (each creating a `Payment` transaction). The Saldos (Accounts Receivable) report shows customers with net balance > 0, rendered server-side.

---

## When It Applies

**Applies when**:
- An active organization is selected in the session (transactional screens require this)
- The operator is managing purchase orders for an organization
- Clients owe money and the operator needs to track or receive payments

**Does not apply when**:
- No organization is selected — transactional pages (Orders, Payments, Reports) redirect with an error; catalog pages remain accessible
- Authentication or authorization is needed — Phase 1 has no auth (all users have full access)
- Azure deployment or CI/CD is involved — out of scope for Phase 1
- Automated tests are needed — out of scope for Phase 1
- A per-customer transaction detail report is needed — recognized future need, not built in Phase 1

---

## Key Decisions

1. **Razor Pages + jsGrid AJAX** (not MVC Controllers): The existing project already uses Razor Pages with jsGrid partially implemented. Staying consistent avoids a rewrite and keeps all page logic co-located. Named handlers (`?handler=Load`) serve as the AJAX API for jsGrid.

2. **`ICurrentOrganizationService` for Global Query Filters**: Rather than filtering in every service method or coupling the DbContext directly to `IHttpContextAccessor`, a scoped service is injected into the DbContext. This keeps the persistence layer decoupled from HTTP while enabling EF's GQF to filter automatically.

3. **Client-side recalculation (no server round-trip)**: Order item totals recalculate instantly in JavaScript on every field change. The server trusts the submitted values (all fields are intentionally user-editable by design). The server only enforces `ShippingAmountToCR = Sum(EstimateShipping)` on save to prevent stale aggregates.

4. **`OrderStatusHistory` table instead of date fields on `Order`**: Storing one row per transition (with date, notes, and justification) is cleaner than adding `ActiveAt`, `DeliveringAt`, `DeliveredAt`, `CompletedAt`, `VoidedAt` nullable fields. Provides a full audit trail.

5. **Atomic transaction automation**: All side effects of a status transition (history record + ledger entries) happen in one `SaveChangesAsync()` call in `OrderService`. No domain events or separate service calls — keeps it simple and consistent for Phase 1 scale.

6. **Delete restricted to Pending status**: Both orders and order items can only be deleted when the order is in `Pending` status. `OrderService.DeleteOrderAsync` and `DeleteOrderItemAsync` enforce this server-side and return a user-facing error if the status is not Pending. Deleting an order cascades to its items. The rationale: once an order is activated, it generates financial transactions; allowing deletion would create orphaned ledger entries.

7. **Order status ≠ payment status**: The order lifecycle (Pending → Active → Delivering → Delivered → Completed) tracks fulfillment only. `Completed` is a manual operational marker and does not imply the customer has paid. Balances are derived exclusively from `Transaction` records.

---

## Areas Needing Attention

- **GQF with `Guid.Empty`**: When no org is selected, `ICurrentOrganizationService.OrganizationId` returns `Guid.Empty`. The GQF `o.OrganizationId == Guid.Empty` returns zero results — safe, but could mask a missing guard. T054 adds an explicit redirect for transactional pages; reviewers should verify this is sufficient.

- **`IgnoreQueryFilters()` in Payment/Saldos queries**: `GetCustomerBalanceAsync` uses `IgnoreQueryFilters()` for the global balance calculation. This bypasses the org filter intentionally (balances are customer-global). Reviewers should confirm this is the desired behavior and that it doesn't inadvertently expose cross-org data in other query paths.

- **Session-based org in a single-user context**: The session approach works for Phase 1's single-user assumption. If multiple browser tabs are open with different orgs selected, session state is shared (last write wins). Not a concern for Phase 1 but worth noting.

- **ProductType EstimateShipping currency**: `ProductType` has a `CurrencyId` field, but `Order.ShippingAmountToCR` is described as "to Costa Rica" (implying colones). If EstimateShipping is stored in USD and ToCR means colones, there may be a currency conversion gap in the formula. The spec treats them as the same denomination — worth confirming with the business owner.

---

## Open Questions

- Should `ShippingAmountToCR` in the Order header be auto-recalculated on the server every time an item is saved, or only when the full order is saved? The spec says "on any item change" — the current design calls `UpdateTotalsAsync` after each item save. Verify this is the expected UX.
- The `Transaction.TransactionDate` should use Costa Rica local time (UTC-6). The implementation needs to ensure `DateTime.Now` is correctly offset, especially if the server runs in UTC. Consider using `TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central America Standard Time")`.

---

## Review Checklist

- [ ] Key decisions are justified
- [ ] Global Query Filters cover all tenant-scoped entities and exclude global entities
- [ ] `OrderStatusHistory` is created atomically with status transitions
- [ ] `Charge` / `Void` transactions are created atomically with status transitions
- [ ] No `Update` or `Delete` endpoints exist for `Transaction` (immutability enforced)
- [ ] Payment transactions have `SourceId = null` (not linked to a specific item)
- [ ] Saldos balance formula is global (ignores org filter)
- [ ] `AgreedPriceInLocal` override shows original formula value as reference
- [ ] Order fields are read-only for non-Pending statuses
- [ ] Voided transition requires non-empty justification (server-side validation)
- [ ] Active transition is blocked if the order has zero items
- [ ] Delete order is blocked server-side unless status is Pending
- [ ] Delete order item is blocked server-side unless parent order status is Pending
- [ ] Deleting an order cascades deletion of all its items
- [ ] Scope matches the stated boundaries (no auth, no Azure deployment)
- [ ] Success criteria are achievable with the described implementation

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
