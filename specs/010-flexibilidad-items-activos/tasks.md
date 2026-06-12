# Tasks: Flexibilidad de Ítems en Órdenes Activas

**Input**: Design documents from `specs/010-flexibilidad-items-activos/`

**Feature branch**: `010-flexibilidad-items-activos`

**Available docs**: spec.md · plan.md · research.md · data-model.md · contracts/items-reassign-handlers.md

**Tests**: No automated tests in this project. Manual verification per plan.md FR checklist.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no pending dependencies)
- **[Story]**: User story this task belongs to (US1–US4)
- All file paths are relative to `MariCamiStore/`

---

## Phase 1: Setup — Migration Scaffold

**Purpose**: Generate the EF migration before any code changes, so the migration file exists for editing.

- [X] T001 Generate EF migration by running `dotnet ef migrations add AddCustomerIsGeneric --project MariCamiStore --startup-project MariCamiStore` from repo root; verify the migration file is created in `MariCamiStore/Infrastructure/Persistance/Migrations/`

---

## Phase 2: Foundational — Data Model & Service Layer

**Purpose**: Model, config, migration, and service changes that ALL user stories depend on. Must be complete before any frontend work.

**⚠️ CRITICAL**: No user story UI work can begin until this phase is complete.

- [X] T002 Add `public bool IsGeneric { get; set; } = false;` to `Model/Customer.cs` (after the `Email` property)
- [X] T003 Add `builder.Property(c => c.IsGeneric).IsRequired().HasDefaultValue(false);` to `Infrastructure/Persistance/EntityConfigurations/CustomerEntityTypeConfiguration.cs` (after the Email config block)
- [X] T004 Edit the generated migration's `Up()` method in `Infrastructure/Persistance/Migrations/<timestamp>_AddCustomerIsGeneric.cs`: keep the auto-generated `AddColumn<bool>` call and add `migrationBuilder.Sql(...)` with the IF NOT EXISTS / UPDATE idempotent SQL from `specs/010-flexibilidad-items-activos/data-model.md §4`; ensure `Down()` only drops the column
- [X] T005 Update `SaldoReportRow` record in `Services/IPaymentService.cs`: change to `public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric);`
- [X] T006 Add `Task<(bool Success, string? Error)> ReasignarItemAsync(Guid itemId, Guid newCustomerId, decimal newAgreedPriceInLocal);` to `Services/IOrderService.cs`
- [X] T007 Implement `ReasignarItemAsync` in `Services/OrderService.cs` following the full logic in `specs/010-flexibilidad-items-activos/plan.md §Phase 2.2` (guard on order status, 3 transaction cases, AgreedPriceInLocal update, partial recalc of TotalAgreedPriceInLocal and EstimatedProfitInLocal only)
- [X] T008 Update `GetSaldosReportAsync()` in `Services/PaymentService.cs`: (a) change `.Where(r => r.Balance > 0)` to `.Where(r => r.Balance != 0)`; (b) change the customer lookup to a tuple dictionary that includes `IsGeneric`; (c) pass `IsGeneric` to each `SaldoReportRow` constructor call (follow plan.md §Phase 2.3)

**Checkpoint**: Build must compile cleanly. Run `dotnet build MariCamiStore` to verify no errors before proceeding.

---

## Phase 3: User Story 1 — Crear ítem con inventario especulativo (P1) 🎯 MVP

**Goal**: "Sin Cliente" can be assigned to any item, appears in the item modal dropdown in alphabetical order, is excluded from the payment form dropdown, and shows with "(Especulativo)" label in Saldos after an order is activated.

**Independent Test**: Create an item assigned to "Sin Cliente", activate the order, navigate to Payments/Index — "Sin Cliente" must appear in the saldos table with "(Especulativo)" label and cannot be selected in the payment registration customer dropdown.

- [X] T009 [US1] Locate the customer list AJAX endpoint used by the item add/edit modal in `Pages/Orders/Items.cshtml` (search for customer dropdown data load in `.cshtml` or `.cshtml.cs`). Confirm "Sin Cliente" (`IsGeneric = true`) is included (no filter needed here); if any `IsGeneric` filter exists, remove it for the items modal endpoint.
- [X] T010 [US1] Locate the customer list AJAX endpoint used by the payment registration form in `Pages/Payments/Index.cshtml`. Add `.Where(c => !c.IsGeneric)` filter to that endpoint so "Sin Cliente" is excluded from the payment dropdown (FR-003).
- [X] T011 [US1] Update saldos table JS in `Pages/Payments/Index.cshtml` to read the new `isGeneric` field from each row and append `" (Especulativo)"` to the customer name cell when `row.isGeneric === true` (FR-018).

**Checkpoint**: US1 fully testable — Sin Cliente available in item modal, excluded from payment form, labeled in saldos.

---

## Phase 4: User Stories 2 & 3 — Reasignación de ítem y ajuste de precio (P2/P3)

**Goal**: A "Reasignar" button appears on each item row in Active/Delivering/Delivered/Completed orders. Clicking it opens a modal pre-filled with the current customer and price. On confirm, the system creates the appropriate transactions and recalculates order totals. US3 (price-only adjustment for same customer) is handled by the same modal and handler — no separate UI needed.

**Independent Test**: Take an active-order item assigned to Client A. Click "Reasignar", select Client B at a new price, confirm. Verify: (1) Transactions table shows a Void to A and a Charge to B; (2) Item row updates to Client B + new price; (3) Order totals (TotalAgreedPriceInLocal, EstimatedProfitInLocal) updated; (4) TotalToPayToSupplier unchanged.

- [X] T012 [US2] Add `public record ReasignarRequest(Guid ItemId, Guid NewCustomerId, decimal NewAgreedPriceInLocal);` and `OnPostReasignarAsync` handler to `Pages/Orders/Items.cshtml.cs` (follow contract in `specs/010-flexibilidad-items-activos/contracts/items-reassign-handlers.md`)
- [X] T013 [US2] Add a "Reasignar" button to each item row in `Pages/Orders/Items.cshtml`: button is visible only when `Order.Status` ∈ {Active, Delivering, Delivered, Completed}; include `data-item-id`, `data-customer-id`, `data-customer-name`, `data-price` attributes (follow plan.md §Phase 4.1)
- [X] T014 [US2] Add reassignment modal HTML to `Pages/Orders/Items.cshtml`: modal with customer `<select>` (`id="reasignarCustomerId"`), price `<input>` (`id="reasignarPrecio"`), price=0 warning div, error div, Confirmar/Cancelar buttons (follow plan.md §Phase 4.2)
- [X] T015 [P] [US2] Add JS in `Pages/Orders/Items.cshtml`: customer dropdown loader (`loadCustomersForReasignar`), button click handler that opens the modal pre-filled with the item's current customer and price (follow plan.md §Phase 4.4 JS block)
- [X] T016 [P] [US2] Add JS confirm handler in `Pages/Orders/Items.cshtml`: POST to `?handler=Reasignar` with antiforgery token, show price=0 warning (FR-015), handle `success: true` (close modal + `loadItems()`) and `success: false` (show error in modal) (follow plan.md §Phase 4.4 confirm JS block)

**Checkpoint**: US2 and US3 fully testable — client reassignment and price-only adjustments both work through the modal.

---

## Phase 5: User Story 4 — Ver saldos negativos / créditos a favor (P4)

**Goal**: The Saldos table shows customers with negative balances labeled "Crédito a favor" with visual differentiation (badge/color). Balance = 0 customers are excluded. Backend filter already handles this after Phase 2 (T008).

**Independent Test**: Create a scenario where Client A has a Charge + Payment that nets to a negative balance (or trigger a reassignment with prior Payment). Navigate to Payments/Index — Client A must appear in the saldos table with "Crédito a favor" badge and a different row color.

- [X] T017 [US4] Update saldos table render JS in `Pages/Payments/Index.cshtml`: (a) render "Crédito a favor" badge (e.g. `badge-success`) for rows where `balance < 0`; (b) render "Saldo pendiente" badge (e.g. `badge-warning`) for rows where `balance > 0`; (c) apply distinct row CSS class (e.g. `table-success`) for negative rows; (d) display absolute value of balance in the amount cell with a leading "−" sign for negative rows (follow plan.md §Phase 5.1)

**Checkpoint**: US4 fully testable — negative saldo rows visible and labeled; "(Especulativo)" label from US1/T011 also visible for Sin Cliente.

---

## Phase 6: Polish & Verification

**Purpose**: Final verification sweep and any edge-case fixes.

- [ ] T018 [P] Manually verify all 18 FRs from `specs/010-flexibilidad-items-activos/plan.md §Verification Checklist` against the running app; fix any issues found before marking this task complete
- [X] T019 [P] Review the full `ReasignarItemAsync` implementation in `Services/OrderService.cs` for edge cases: (a) `newAgreedPriceInLocal = 0` (valid — Void + Charge(0)); (b) reasigning Sin Cliente to Sin Cliente (no-op if same ID); (c) concurrent requests (EF tracks state per context, each request has its own context — no action needed)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 (migration file must exist to edit). **Blocks all UI work.**
- **Phase 3 (US1)**: Depends on Phase 2 (needs `Customer.IsGeneric`, updated `SaldoReportRow`)
- **Phase 4 (US2/US3)**: Depends on Phase 2 (needs `ReasignarItemAsync` in service)
- **Phase 5 (US4)**: Depends on Phase 2 (needs `GetSaldosReportAsync` filter change)
- **Phase 6 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)** → no story dependencies; depends only on foundational phase
- **US2/US3 (P2/P3)** → no story dependencies; depends only on foundational phase
- **US4 (P4)** → no story dependencies; depends only on foundational phase (backend already done in T008)

### Within Phase 4

- T012 (handler) → can start immediately after Phase 2
- T013 (button) and T014 (modal HTML) → can run in parallel after Phase 2
- T015 and T016 (JS) → run after T013+T014 (HTML must exist for JS to reference IDs)

### Parallel Opportunities

- After Phase 2 completes: Phases 3, 4, and 5 can be worked in parallel (they touch different files)
- Within Phase 4: T013 and T014 are parallel; T015 and T016 are parallel (marked [P])
- Phase 6: T018 and T019 are parallel (marked [P])

---

## Parallel Example: Phase 4 (US2/US3)

```text
# After T012 (handler) is done, launch in parallel:
T013: Add "Reasignar" button to Items.cshtml
T014: Add reassignment modal HTML to Items.cshtml

# After T013 + T014 are done, launch in parallel:
T015: Add JS modal-open loader
T016: Add JS confirm-submit handler
```

---

## Implementation Strategy

### MVP (US1 only — Sin Cliente workflow)

1. Complete Phase 1 (T001)
2. Complete Phase 2 (T002–T008)
3. Complete Phase 3 (T009–T011)
4. **VALIDATE**: Create item with Sin Cliente → activate order → confirm Saldos shows Sin Cliente (Especulativo)
5. Ship US1 independently if needed

### Full Delivery Order

1. Phase 1 → Phase 2 (foundation)
2. Phase 3 (US1) → validate Sin Cliente end-to-end
3. Phase 4 (US2+US3) → validate reassignment and price adjustment
4. Phase 5 (US4) → validate negative saldo display
5. Phase 6 (polish + verification checklist)

---

## Notes

- `[P]` tasks = different files or logically independent, no pending dependencies
- No automated tests exist in this project; manual verification is the acceptance gate
- After T004, run `dotnet ef database update` on the local dev DB before testing
- The `ReasignarItemAsync` no-op case (same customer + same price) returns `success: true` — frontend does not reload items in that case (no visible change)
- `TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`, `ShippingAmountToCR` are NEVER touched by any task in this feature
