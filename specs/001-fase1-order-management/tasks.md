# Tasks: Fase 1 â€” Sales & Order Management System

**Input**: Design documents from `specs/001-fase1-order-management/`

**Prerequisites**: plan.md âœ“, spec.md âœ“, research.md âœ“, data-model.md âœ“, contracts/ âœ“

**Tests**: Not requested â€” no test tasks included.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1â€“US6)

---

## Phase 1: Setup

**Purpose**: Verify baseline and confirm the existing project is runnable before adding new code.

- [x] T001 Verify project builds and runs: open `MariCamiStore/MariCamiStore.sln`, build solution, confirm app starts without errors

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Multi-tenant infrastructure that ALL user stories depend on. No user story work can begin until this phase is complete.

**âš ï¸ CRITICAL**: Complete in order â€” GQF depends on ICurrentOrganizationService; migration depends on EF config.

- [x] T002 Create `MariCamiStore/Services/ICurrentOrganizationService.cs` â€” interface with `Guid OrganizationId { get; }` property
- [x] T003 Create `MariCamiStore/Services/CurrentOrganizationService.cs` â€” reads `OrganizationId` from `IHttpContextAccessor` session key `"ActiveOrganizationId"`; returns `Guid.Empty` if not set
- [x] T004 Add `IHttpContextAccessor` and session services to `MariCamiStore/Program.cs` â€” add `builder.Services.AddHttpContextAccessor()`, `builder.Services.AddSession()`, and `app.UseSession()` before `app.UseRouting()`
- [x] T005 Create `MariCamiStore/Model/OrderStatusHistory.cs` â€” fields: `Id` (Guid), `OrderId` (Guid FK), `FromStatus` (string), `ToStatus` (string), `TransitionDate` (DateTime), `Notes` (string?), `Justification` (string?), `CreatedAt` (DateTime)
- [x] T006 Create `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderStatusHistoryEntityTypeConfiguration.cs` â€” table `dbo.OrderStatusHistory`, PK on Id, FK to Orders, MaxLength on status fields (20), Notes (500), Justification (1000)
- [x] T007 Update `MariCamiStore/Infrastructure/Persistance/MariCamiStoreContext.cs` â€” add constructor parameter `ICurrentOrganizationService currentOrgService`, add `DbSet<OrderStatusHistory> OrderStatusHistories`, apply `OrderStatusHistoryEntityTypeConfiguration`, add Global Query Filters on `Order`, `OrderItem`, `Transaction` using `currentOrgService.OrganizationId`
- [x] T008 Register `ICurrentOrganizationService` (scoped) in `MariCamiStore/Extensions/ApplicationExtensions.cs` and update `MariCamiStoreContext` registration to inject `ICurrentOrganizationService`
- [x] T009 Add EF Core migration `AddOrderStatusHistory` â€” run `dotnet ef migrations add AddOrderStatusHistory` from `MariCamiStore/` directory; verify generated migration creates `OrderStatusHistory` table

**Checkpoint**: Build succeeds, migration applied, app runs with session and GQF active.

---

## Phase 3: User Story 1 â€” Organization Context Selection (Priority: P1) ðŸŽ¯ MVP

**Goal**: Operator can select an organization from the navbar and all subsequent data is scoped to it.

**Independent Test**: Load app â†’ select org from dropdown â†’ navigate to any catalog â†’ confirm only that org's data appears; switch org â†’ data changes.

- [x] T010 Create `MariCamiStore/Pages/Organizations/SetActive.cshtml.cs` (or add handler `OnPostSetActiveAsync` to existing Organizations page) â€” receives `organizationId` (Guid), sets `HttpContext.Session["ActiveOrganizationId"]`, returns `JsonResult({ success: true })`
- [x] T011 Update `MariCamiStore/Pages/Shared/_Layout.cshtml` â€” add organization selector dropdown in navbar: fetch all organizations on page load, highlight active org, wire AJAX POST to SetActive handler on change; add `<div id="loading-overlay">` full-screen spinner that shows/hides on jQuery `ajaxStart`/`ajaxStop` global events
- [x] T012 Create `MariCamiStore/wwwroot/js/layout.js` â€” org selector AJAX call (POST to set active), page reload on success, global spinner logic using `$(document).ajaxStart` / `$(document).ajaxStop`
- [x] T013 Add `<script src="/js/layout.js">` reference and `@Html.AntiForgeryToken()` to `MariCamiStore/Pages/Shared/_Layout.cshtml`

**Checkpoint**: Switching org in navbar updates session and filters data correctly.

---

## Phase 4: User Story 2 â€” Catalog Management (Priority: P2)

**Goal**: Full CRUD for Configuration, Currency, ProductType, Supplier, Customer via AJAX jsGrid.

**Independent Test**: Navigate to each catalog page â†’ add record â†’ edit it â†’ delete it â†’ all without page reload.

- [x] T014 Create `MariCamiStore/Services/ICatalogService.cs` â€” declare CRUD methods for all catalogs: `GetCurrenciesAsync`, `CreateCurrencyAsync`, `UpdateCurrencyAsync`, `DeleteCurrencyAsync`; same pattern for Configuration (upsert only), ProductType, Supplier, Customer
- [x] T015 Create `MariCamiStore/Services/CatalogService.cs` â€” implement all ICatalogService methods using `MariCamiStoreContext`; Configuration uses upsert pattern (no delete)
- [x] T016 Register `ICatalogService` (scoped) in `MariCamiStore/Extensions/ApplicationExtensions.cs`
- [x] T017 [P] [US2] Update `MariCamiStore/Pages/Currencies/Index.cshtml.cs` â€” add `OnPostInsertAsync`, `OnPostUpdateAsync`, `OnPostDeleteAsync` handlers using `ICatalogService`; rename existing load handler to `OnGetLoadAsync`
- [x] T018 [P] [US2] Update `MariCamiStore/wwwroot/js/pages/currencies/index.js` â€” complete jsGrid config with `insertItem`, `updateItem`, `deleteItem` pointing to Insert/Update/Delete handlers; add antiforgery token to POST calls
- [x] T019 [P] [US2] Create `MariCamiStore/Pages/Configurations/Index.cshtml` and `Index.cshtml.cs` â€” jsGrid with upsert (no delete); handlers: `OnGetLoadAsync`, `OnPostUpsertAsync`; fields: TaxPercentage, ExchangeRate, LocalCurrencyId (dropdown from Currencies)
- [x] T020 [P] [US2] Create `MariCamiStore/wwwroot/js/pages/configurations/index.js` â€” jsGrid config for single-record upsert pattern
- [x] T021 [P] [US2] Create `MariCamiStore/Pages/ProductTypes/Index.cshtml` and `Index.cshtml.cs` â€” standard CRUD handlers; fields: Name, Description, EstimateShipping, ServiceFeeInLocal, CurrencyId
- [x] T022 [P] [US2] Create `MariCamiStore/wwwroot/js/pages/product-types/index.js` â€” jsGrid config for ProductType CRUD
- [x] T023 [P] [US2] Create `MariCamiStore/Pages/Suppliers/Index.cshtml` and `Index.cshtml.cs` â€” standard CRUD handlers; fields: Name, Website
- [x] T024 [P] [US2] Create `MariCamiStore/wwwroot/js/pages/suppliers/index.js` â€” jsGrid config for Supplier CRUD
- [x] T025 [P] [US2] Update `MariCamiStore/Pages/Customers/Index.cshtml.cs` â€” add full CRUD handlers using `ICatalogService` (no org filter); fields: FirstName, LastName, Email, Phone
- [x] T026 [P] [US2] Create `MariCamiStore/wwwroot/js/pages/customers/index.js` â€” jsGrid config for Customer CRUD
- [x] T027 [US2] Add sidebar navigation links for all catalog pages to `MariCamiStore/Pages/Shared/_Layout.cshtml` (ConfiguraciÃ³n, Monedas, Tipos de Producto, Proveedores, Clientes)

**Checkpoint**: All 5 catalog CRUDs work with AJAX. No page reloads on add/edit/delete.

---

## Phase 5: User Story 3 â€” Order Creation and Management (Priority: P3)

**Goal**: Create orders, add items, see real-time calculated totals, transition order to Active.

**Independent Test**: Create order with supplier â†’ open items editor â†’ add 2 items with different ProductTypes â†’ verify totals recalculate on every field change â†’ transition to Active â†’ confirm Charge transactions created.

- [ ] T028 Create `MariCamiStore/Services/IOrderService.cs` â€” declare: `GetOrdersAsync(statusFilter)`, `GetOrderAsync(id)`, `CreateOrderAsync(dto)`, `UpdateOrderAsync(dto)`, `GetOrderItemsAsync(orderId)`, `CreateOrderItemAsync(dto)`, `UpdateOrderItemAsync(dto)`, `DeleteOrderItemAsync(id)`, `UpdateOrderTotalsAsync(orderId, totalsDto)`, `GetProductTypeValuesAsync(productTypeId)`, `TransitionOrderAsync(dto)`
- [ ] T029 Create `MariCamiStore/Services/OrderService.cs` â€” implement `GetOrdersAsync`, `GetOrderAsync`, `CreateOrderAsync` (pre-fill ExchangeRate/TaxPercentage from active Configuration), `UpdateOrderAsync`; new orders default to `Status = OrderStatus.Pending.Key` and `CreatedAt = DateTime.UtcNow`
- [ ] T030 Register `IOrderService` (scoped) in `MariCamiStore/Extensions/ApplicationExtensions.cs`
- [ ] T031 Create `MariCamiStore/Pages/Orders/Index.cshtml` and `Index.cshtml.cs` â€” handlers: `OnGetAsync` (page), `OnGetLoadAsync(statusFilter)` (jsGrid load), `OnPostCreateAsync` (new order modal), `OnPostUpdateAsync` (edit order fields); order list columns: Name, Supplier, Status, TotalOfTheOrder, EstimatedProfitInLocal, CreatedAt
- [ ] T032 Create `MariCamiStore/wwwroot/js/pages/orders/index.js` â€” jsGrid for order list, "Nueva Orden" modal with form, status-aware action buttons per row (only show valid transitions)
- [ ] T033 Add `GetOrderItemsAsync`, `CreateOrderItemAsync`, `UpdateOrderItemAsync`, `DeleteOrderItemAsync`, `UpdateOrderTotalsAsync`, `GetProductTypeValuesAsync` to `MariCamiStore/Services/OrderService.cs`
- [ ] T034 Create `MariCamiStore/Pages/Orders/Items.cshtml` and `Items.cshtml.cs` â€” page receives `orderId` query param; handlers: `OnGetAsync` (page + order header data), `OnGetLoadAsync` (items jsGrid), `OnGetProductTypeAsync(id)` (auto-fill values), `OnPostInsertAsync`, `OnPostUpdateAsync`, `OnPostDeleteAsync`, `OnPostUpdateTotalsAsync`; CRUD enabled only when `Order.Status == Pending`
- [ ] T035 Create `MariCamiStore/wwwroot/js/pages/orders/items.js` â€” jsGrid for items; on `ProductTypeId` field change: AJAX call to ProductType handler â†’ auto-fill EstimateShipping and ServiceFeeInLocal; real-time recalculation on every numeric input: ListPriceTax, AgreedPriceInLocal, all Order header totals; call `UpdateTotals` handler after each item save/delete
- [ ] T036 Add Orders link to sidebar in `MariCamiStore/Pages/Shared/_Layout.cshtml`

**Checkpoint**: Order created, items added, totals recalculate in real time, order visible in dashboard.

---

## Phase 6: User Story 4 â€” Order Status Lifecycle (Priority: P4)

**Goal**: Advance orders through states with date + notes; Void requires justification; each transition logged in history; ledger transactions auto-created.

**Independent Test**: Activate order â†’ confirm Charge transactions created per item; advance to Delivering/Delivered/Completed with dates â†’ verify each step in OrderStatusHistory; Void from Active without justification â†’ blocked with error; Void with justification â†’ Void transactions created.

- [ ] T037 Add `TransitionOrderAsync(orderId, toStatus, transitionDate, notes, justification)` to `MariCamiStore/Services/OrderService.cs` â€” validate state machine (Pendingâ†’Activeâ†’Deliveringâ†’Deliveredâ†’Completed, Voided from any non-Pending); validate at least 1 item for Active transition; validate non-empty justification for Voided; create `OrderStatusHistory` record; if Active: create one `Charge` Transaction per item (Amount=AgreedPriceInLocal, CustomerId=item.CustomerId, SourceId=item.Id, OrganizationId=order.OrganizationId, Description="Cargo â€“ {ProductDescription} â€“ Orden {NameOfOrder}"); if Voided (non-Pending): create one `Void` Transaction per item with Description="AnulaciÃ³n â€“ {ProductDescription} â€“ Orden {NameOfOrder}"; all in single `SaveChangesAsync()` call; **NOTE: do NOT expose any Update or Delete endpoints for Transaction â€” immutability is enforced by absence of mutation handlers**
- [ ] T038 Add `OnPostTransitionAsync` handler to `MariCamiStore/Pages/Orders/Index.cshtml.cs` â€” receives `orderId`, `toStatus`, `transitionDate`, `notes`, `justification`; calls `OrderService.TransitionOrderAsync`; returns `JsonResult({ success, newStatus, newStatusLabel, error })`
- [ ] T039 Update `MariCamiStore/wwwroot/js/pages/orders/index.js` â€” add transition modal: date input (default today), optional notes textarea, conditional justification textarea (required + visible only for Voided); wire transition buttons to modal â†’ POST to Transition handler â†’ refresh grid row on success; show server error messages inline
- [ ] T040 Add `OnGetHistoryAsync(orderId)` handler to `MariCamiStore/Pages/Orders/Items.cshtml.cs` â€” returns list of `OrderStatusHistory` records for the order
- [ ] T041 Add order status history panel to `MariCamiStore/Pages/Orders/Items.cshtml` â€” read-only timeline showing each transition: date, fromâ†’to status, notes, justification; loaded via AJAX on page load

**Checkpoint**: Full order lifecycle works. Transitions logged. Charge/Void transactions auto-created atomically.

---

## Phase 7: User Story 5 â€” Payment Registration (Priority: P5)

**Goal**: Register client payments; see outstanding balance globally and by active org.

**Independent Test**: Select customer with existing Charge transactions â†’ verify balance shown correctly â†’ register payment â†’ balance decreases â†’ register again â†’ balance continues decreasing.

- [ ] T042 Create `MariCamiStore/Services/IPaymentService.cs` â€” declare: `GetCustomerBalanceAsync(customerId)` returns `{ GlobalBalance, OrgBalance }`, `RegisterPaymentAsync(customerId, amount)` returns updated balances
- [ ] T043 Create `MariCamiStore/Services/PaymentService.cs` â€” `GetCustomerBalanceAsync`: query Transactions grouped by CustomerId, calc `Sum(Charge) - Sum(Payment) - Sum(Void)`; global = ignore GQF (use `IgnoreQueryFilters()`), org = use GQF normally; `RegisterPaymentAsync`: validate amount > 0 and customerId not empty, create `Transaction { Type=Payment, Amount=amount, CustomerId=customerId, OrganizationId=activeOrg, SourceId=null, CurrencyId=localCurrencyId, Description="Pago â€“ {CustomerFullName}", TransactionDate=DateTime.Now (CR local time) }`, save, return updated balances
- [ ] T044 Register `IPaymentService` (scoped) in `MariCamiStore/Extensions/ApplicationExtensions.cs`
- [ ] T045 Create `MariCamiStore/Pages/Payments/Index.cshtml` and `Index.cshtml.cs` â€” handlers: `OnGetAsync` (page), `OnGetBalanceAsync(customerId)` (returns balance DTO as JSON), `OnPostRegisterPaymentAsync` (registers payment, returns updated balance); customer selector dropdown populated from CatalogService
- [ ] T046 Create `MariCamiStore/wwwroot/js/pages/payments/index.js` â€” customer dropdown with AJAX balance load on selection; amount input; submit handler â†’ POST â†’ update displayed balances on success; show validation errors
- [ ] T047 Add Pagos link to sidebar in `MariCamiStore/Pages/Shared/_Layout.cshtml`

**Checkpoint**: Payment registered, balances update immediately, Transaction record created.

---

## Phase 8: User Story 6 â€” Accounts Receivable Report (Priority: P6)

**Goal**: View all clients with outstanding balance > 0 with net amount owed.

**Independent Test**: Confirm only customers with Charges exceeding Payments+Voids appear; verify math on at least 3 customers with mixed transaction histories.

- [ ] T048 Add `GetSaldosReportAsync()` to `MariCamiStore/Services/PaymentService.cs` (or `IPaymentService`) â€” query groups all Transactions by CustomerId, calculates `Sum(Charge) - Sum(Payment) - Sum(Void)`, filters balance > 0, joins Customer name, returns ordered by balance descending; uses `IgnoreQueryFilters()` for global balance (balances are customer-global, not org-scoped)
- [ ] T049 Create `MariCamiStore/Pages/Reports/Saldos.cshtml` and `Saldos.cshtml.cs` â€” `OnGetAsync` calls `GetSaldosReportAsync()` and renders server-side HTML table with columns: Nombre Completo, Total Adeudado (formatted in local currency); no AJAX needed
- [ ] T050 Add Saldos link to sidebar in `MariCamiStore/Pages/Shared/_Layout.cshtml`

**Checkpoint**: Saldos report shows correct customers and correct balances.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements spanning multiple user stories.

- [ ] T051 [P] Add `[ValidateAntiForgeryToken]` attribute to all POST handlers across all Razor Pages (`Orders/Index`, `Orders/Items`, `Payments/Index`, all catalog pages) â€” verify jsGrid JS sends the token in POST requests
- [ ] T052 [P] Verify GQF isolation â€” manually test that switching organizations shows different data for Orders, Transactions; confirm Customers, Currencies, ProductTypes, Suppliers are visible from any org
- [ ] T053 [P] Add `CreatedAt = DateTime.UtcNow` and `UpdatedAt = DateTime.UtcNow` defaults in all Create/Update service methods that are missing them
- [ ] T054 Guard against missing active organization â€” add redirect or error message when `ICurrentOrganizationService.OrganizationId == Guid.Empty` and user tries to access transactional pages (Orders, Payments, Reports/Saldos); global catalogs (Currencies, ProductTypes, Suppliers, Customers, Configurations) remain accessible without an active org

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 â€” **BLOCKS all user stories**
- **Phase 3 (US1)**: Depends on Phase 2
- **Phase 4 (US2)**: Depends on Phase 2; can run in parallel with Phase 3
- **Phase 5 (US3)**: Depends on Phase 2 + Phase 4 (needs catalog data: Suppliers, ProductTypes, Customers, Configuration)
- **Phase 6 (US4)**: Depends on Phase 5 (needs orders with items)
- **Phase 7 (US5)**: Depends on Phase 6 (needs Charge transactions from active orders)
- **Phase 8 (US6)**: Depends on Phase 7 (needs payments to show non-trivial balances)
- **Phase 9 (Polish)**: Depends on all previous phases

### Parallel Opportunities Within Phases

**Phase 4 (Catalogs)**: T017â€“T026 can all run in parallel (different pages/files):
```
Parallel: T017+T018 (Currencies) | T019+T020 (Configurations) | T021+T022 (ProductTypes) | T023+T024 (Suppliers) | T025+T026 (Customers)
```

**Phase 5 (Orders)**: T033â€“T035 depend on T029â€“T032; T036 is independent:
```
Sequential: T028 â†’ T029+T030 â†’ T031+T032 â†’ T033 â†’ T034+T035
Parallel with T036 (sidebar link)
```

---

## Implementation Strategy

### MVP (Phase 1 + 2 + 3)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational) â€” critical blocker
3. Complete Phase 3 (US1: Org Selector)
4. **STOP and VALIDATE**: App runs, org switching filters data
5. Continue to Phase 4+ incrementally

### Incremental Delivery

1. Phases 1â€“3 â†’ Organization switching works âœ“
2. Phase 4 â†’ All catalogs have data entry âœ“
3. Phase 5 â†’ Orders can be created and items added âœ“
4. Phase 6 â†’ Full order lifecycle + ledger âœ“
5. Phase 7 â†’ Payments registered âœ“
6. Phase 8 â†’ Saldos report live âœ“
7. Phase 9 â†’ Security + edge cases hardened âœ“

---

## Task Summary

| Phase | Tasks | Parallelizable |
|-------|-------|----------------|
| Phase 1: Setup | 1 | 0 |
| Phase 2: Foundational | 8 | 0 |
| Phase 3: US1 Org Selector | 4 | 0 |
| Phase 4: US2 Catalogs | 11 | 10 |
| Phase 5: US3 Orders | 9 | 2 |
| Phase 6: US4 Status Lifecycle | 5 | 1 |
| Phase 7: US5 Payments | 6 | 1 |
| Phase 8: US6 Saldos | 3 | 0 |
| Phase 9: Polish | 4 | 4 |
| **Total** | **51** | **18** |

