# Feature Specification: Fase 1 — Sales & Order Management System

**Feature Branch**: `001-fase1-order-management`

**Created**: 2026-06-03

**Status**: Draft

**Input**: Complete Phase 1 implementation of MariCamiStore — a multi-tenant sales and order management platform for a Costa Rican resale business that purchases from international suppliers (Amazon, Shein, etc.) and sells locally in colones.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Organization Context Selection (Priority: P1)

As an operator, I need to select which organization (tenant) I'm working in before performing any action, so that my data is properly isolated from other organizations.

**Why this priority**: Every other feature depends on the active organization being set. Without this, no transactional data can be scoped correctly.

**Independent Test**: Can be tested by loading the application, selecting an organization from the navbar dropdown, navigating to any catalog page, and confirming that only that organization's data appears.

**Acceptance Scenarios**:

1. **Given** the application is open, **When** I look at the top navigation bar, **Then** I see a dropdown listing all available organizations.
2. **Given** I select an organization from the dropdown, **When** I navigate to any data screen, **Then** all records shown belong only to the selected organization.
3. **Given** I switch organizations via the dropdown, **When** I view the same screen again, **Then** the records update to reflect the newly selected organization.
4. **Given** no organization has been selected yet, **When** I access a data screen, **Then** the system prompts me to select an organization or defaults to the first available.

---

### User Story 2 — Catalog Management (Priority: P2)

As an operator, I need to maintain the reference catalogs (currencies, product types, suppliers, customers, and system configuration) so that orders can be created with accurate reference data.

**Why this priority**: Catalogs are prerequisite data for creating orders. They are simpler than orders and must exist first.

**Independent Test**: Can be tested by navigating to each catalog screen, adding a new record, editing it, and deleting it — all without creating any orders.

**Acceptance Scenarios**:

1. **Given** I am on a catalog page (e.g., Currencies), **When** I click "Agregar", **Then** a form appears where I can enter the new record.
2. **Given** I submit a valid new record, **When** the save completes, **Then** the new record appears in the grid without a page reload.
3. **Given** I click "Editar" on an existing record, **When** I change fields and save, **Then** the grid reflects the updated values.
4. **Given** I click "Eliminar" on a record, **When** I confirm deletion, **Then** the record is removed from the grid.
5. **Given** I am on the ProductType catalog, **When** I add a product type, **Then** I can set a fixed `EstimateShipping` amount and a fixed `ServiceFeeInLocal` amount.
6. **Given** I am on the Configuration page, **When** I update the default tax percentage or exchange rate, **Then** new orders will use these updated defaults.

---

### User Story 3 — Order Creation and Management (Priority: P3)

As an operator, I need to create purchase orders, add items to them, and track their progress from pending to completion, so that I can manage my purchasing and client commitments.

**Why this priority**: This is the core business flow. Depends on catalogs existing first.

**Independent Test**: Can be tested end-to-end by creating a new order, adding items, transitioning it to Active, and confirming charges appear in the ledger.

**Acceptance Scenarios**:

1. **Given** I am on the Order Dashboard, **When** I click "Nueva Orden", **Then** a form opens with `ExchangeRate` and `TaxPercentage` pre-filled from system Configuration.
2. **Given** I save a new order, **When** the order is created, **Then** it appears in the dashboard with status `Pending` and all fields are editable.
3. **Given** an order is in `Pending` status, **When** I open the Order Items editor, **Then** I can add, edit, and delete items.
4. **Given** I add an item and select a `ProductType`, **When** the type is selected, **Then** `EstimateShipping` and `ServiceFeeInLocal` auto-fill from the catalog values.
5. **Given** I am editing an item, **When** I change `ListPrice`, **Then** `ListPriceTax`, `AgreedPriceInLocal`, and all Order header totals recalculate in real time on screen.
6. **Given** an order has items, **When** I transition it to `Active`, **Then** a confirmation dialog appears requiring a date (default today) and the system creates one `Charge` transaction per item.
7. **Given** an order is in `Active` or later status, **When** I view the order, **Then** all fields are read-only.
8. **Given** I transition an order to `Voided`, **When** I confirm, **Then** I must provide a justification, and the system creates one `Void` transaction per item.

---

### User Story 4 — Order Status Lifecycle (Priority: P4)

As an operator, I need to advance orders through a defined sequence of states with a date and notes at each step, so that I can track delivery progress.

**Why this priority**: Depends on orders existing. Delivers business value by tracking fulfillment.

**Independent Test**: Can be tested by creating and activating an order, then advancing it through Delivering → Delivered → Completed, confirming each step records a date in the history.

**Acceptance Scenarios**:

1. **Given** an order is in `Active`, **When** I click "Marcar Delivering", **Then** a dialog asks for an estimated delivery date (default today, editable) and confirmation.
2. **Given** an order is in `Delivering`, **When** I click "Marcar Delivered", **Then** a dialog asks for the actual delivery date and confirmation.
3. **Given** an order is in `Delivered`, **When** I click "Completar", **Then** a dialog asks for a completion date (default today) and confirmation.
4. **Given** any transition, **When** I confirm with a date, **Then** the transition is recorded in the order status history with the date and operator.
5. **Given** I initiate a `Void` from any status except `Pending`, **When** I submit, **Then** the justification field is required — saving without it shows an error.

---

### User Story 5 — Payment Registration (Priority: P5)

As an operator, I need to register payments from clients and see their outstanding balances, so that I can track accounts receivable.

**Why this priority**: Requires orders with charges to exist. Closes the financial loop.

**Independent Test**: Can be tested by selecting a customer with existing charges, entering a payment amount, saving it, and confirming the balance decreases.

**Acceptance Scenarios**:

1. **Given** I am on the Payment Registry screen, **When** I select a customer, **Then** I see their total outstanding balance (global across all organizations) and the balance for the active organization.
2. **Given** I enter a payment amount and save, **When** the save completes, **Then** a `Payment` transaction is created and the displayed balance updates.
3. **Given** I submit without selecting a customer or entering an amount, **When** validation runs, **Then** an error message is shown for each missing required field (Customer and Amount).

---

### User Story 6 — Accounts Receivable Report (Priority: P6)

As an operator, I need to view a list of all clients who owe money, so that I can follow up on outstanding balances.

**Why this priority**: Reporting is the last layer; depends on charges and payments existing.

**Independent Test**: Can be tested by confirming that only customers with a net balance > 0 appear, and that the balance formula (Charges − Payments − Voids) is correct.

**Acceptance Scenarios**:

1. **Given** I open the Saldos (Accounts Receivable) report, **When** it loads, **Then** only customers with a total balance greater than 0 are shown.
2. **Given** a customer has a balance of 0 or less, **When** the report loads, **Then** that customer does not appear.
3. **Given** a customer has charges and partial payments, **When** I view their row, **Then** the balance equals `Sum(Charges) − Sum(Payments) − Sum(Voids)`.

---

### Edge Cases

- What happens when no organization exists yet? System must prevent access to transactional screens until at least one organization exists.
- What happens if an order is voided after some items have been paid? Void transactions are still created for all items regardless.
- What if a user manually edits `ShippingAmountToCR` in an order and then adds another item? The field is recalculated and overwrites the manual value.
- What if `AgreedPriceInLocal` is manually edited? The edited value is saved; the original calculated value is displayed alongside it as reference.
- What if no configuration exists yet? New orders cannot be created until `Configuration` has at least one record with defaults.
- What if an order has no items when transitioning to `Active`? The transition to `Active` is not permitted if the order has zero items — the action button must be disabled and an error message shown indicating that at least one item is required.
- What if a user tries to delete an order that is not in `Pending` status? The delete action is blocked with an error message. Only `Pending` orders can be deleted.
- What if a user tries to delete an order item whose parent order is not in `Pending` status? The delete action is blocked with an error message.

---

## Requirements *(mandatory)*

### Functional Requirements

**Infrastructure & Multi-Tenancy**

- **FR-001**: The system MUST provide a global organization selector in the main navigation bar that persists the selected `OrganizationId` in the user session for the duration of the browser session.
- **FR-002**: The system MUST automatically filter all transactional data (orders, order items, transactions) by the active organization stored in the session.
- **FR-003**: Customer records MUST be shared globally across all organizations (not filtered by organization).
- **FR-004**: The UI MUST display a full-screen loading overlay during all asynchronous server requests to prevent duplicate submissions.

**Catalog CRUDs**

- **FR-005**: The system MUST provide full CRUD operations (create, read, update, delete) via AJAX grid for: `Configuration`, `Currency`, `ProductType`, `Supplier`, and `Customer`.
- **FR-006**: `ProductType` records MUST include a fixed `EstimateShipping` amount and a fixed `ServiceFeeInLocal` amount that are copied to order items when the type is selected.
- **FR-007**: `Configuration` MUST store a default `TaxPercentage`, default `ExchangeRate`, and `LocalCurrencyId` that pre-populate new orders.

**Order Management**

- **FR-008**: The Order Dashboard MUST display orders filtered by `Pending` and `Active` status by default, with the ability to view other statuses.
- **FR-009**: New orders MUST pre-populate `ExchangeRate` and `TaxPercentage` from the active `Configuration` record.
- **FR-010**: Order fields MUST be editable only when the order status is `Pending`. All other statuses result in read-only fields.
- **FR-010b**: Orders MUST only be deletable when in `Pending` status. Attempting to delete an order in any other status MUST return an error message. Deleting an order also deletes all its items.
- **FR-011**: The Order Items editor MUST be accessible only for orders in `Pending` status and MUST support full CRUD via AJAX.
- **FR-011b**: Order items MUST only be deletable when the parent order is in `Pending` status. Attempting to delete an item from a non-Pending order MUST return an error message.
- **FR-012**: When a `ProductType` is selected on an order item, the system MUST auto-fill `EstimateShipping` and `ServiceFeeInLocal` from the catalog.
- **FR-013**: The system MUST recalculate the following in real time on the client as item fields change:
  - `ListPriceTax = ListPrice × TaxPercentage`
  - `AgreedPriceInLocal = (ListPrice + ListPriceTax) × ExchangeRate + ServiceFeeInLocal`
  - Order header: `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `ShippingAmountToCR`, `TotalOfTheOrder`, `EstimatedProfitInLocal`
- **FR-014**: All calculated fields MUST remain user-editable, with the original formula result shown as a reference label when a manual override is applied to `AgreedPriceInLocal`.
- **FR-015**: `ShippingAmountToCR` MUST be recalculated as `Sum(EstimateShipping)` from all items whenever any item changes, overwriting any prior manual edit.

**Order Status Lifecycle**

- **FR-016**: The order status machine MUST follow: `Pending → Active → Delivering → Delivered → Completed`. Voiding is permitted from any status except `Pending`.
- **FR-017**: Every status transition MUST present a confirmation dialog with a `TransitionDate` field (default = today, editable by the user).
- **FR-018**: Status transitions to `Voided` MUST require a non-empty justification text; all other transitions allow optional notes.
- **FR-019**: Every status transition MUST be recorded in an `OrderStatusHistory` record containing: order reference, previous status, new status, transition date, notes/justification, and creation timestamp.

**Transaction Automation (Ledger)**

- **FR-020**: When an order transitions to `Active`, the system MUST automatically create one `Charge` transaction per order item, with `Amount = AgreedPriceInLocal` and linked to the `OrderItem`.
- **FR-021**: When an order transitions to `Voided` (from any status other than `Pending`), the system MUST automatically create one `Void` transaction per order item, with `Amount = AgreedPriceInLocal`.
- **FR-022**: Transactions MUST be immutable once created — they cannot be edited or deleted by users.
- **FR-023**: Each transaction MUST record: organization, source item (if applicable), customer, type (Charge/Void/Payment), description (auto-generated from type and source, e.g., "Cargo – [Product Description] – Orden [OrderName]" or "Pago – [Customer Name]"), date (Costa Rica local time), status, and currency.

**Payment Registry**

- **FR-024**: The Payment Registry screen MUST allow the operator to select a customer and enter a payment amount.
- **FR-025**: Upon saving a payment, the system MUST create a `Payment` transaction for the customer. Payment transactions are linked to the customer only; `SourceId` is null (they are not linked to a specific order item).
- **FR-026**: The Payment Registry MUST display the selected customer's total outstanding balance globally and broken down by the active organization.

**Accounts Receivable Report**

- **FR-027**: The Saldos report MUST list only customers whose net balance is greater than zero.
- **FR-028**: Customer balance MUST be calculated as: `Sum(Charge amounts) − Sum(Payment amounts) − Sum(Void amounts)` across all transactions for that customer.
- **FR-029**: The report MUST display at minimum: customer full name and total amount owed.

### Key Entities

- **Organization**: Tenant unit. All transactional data (except Customers) is scoped to an organization.
- **Configuration**: System-wide defaults per organization — tax percentage, exchange rate, local currency.
- **Currency**: Reference catalog of currencies used in pricing and orders.
- **ProductType**: Reference catalog of product categories with fixed `EstimateShipping` and `ServiceFeeInLocal` amounts.
- **Supplier**: Reference catalog of vendors or websites where purchases are made.
- **Customer**: Client registry, shared globally across all organizations.
- **Order**: Purchase order header scoped to an organization. Has a status, financial totals, and belongs to a supplier.
- **OrderItem**: Line item within an order. Links a customer, product type, and pricing details.
- **OrderStatusHistory**: Immutable record of each order status transition, including date, notes, and justification.
- **Transaction**: Immutable financial ledger entry. Types: Charge, Void, Payment. Auto-generated by system events.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator can create a new order, add items, and transition it to Active in under 5 minutes from a fresh start with catalogs already populated.
- **SC-002**: Switching between organizations in the navbar immediately filters all visible data to the selected organization — no stale cross-organization data visible.
- **SC-003**: All CRUD operations on catalogs complete without a full page reload; data updates are visible in the grid within 2 seconds.
- **SC-004**: Order item totals and header totals recalculate and display on screen within 500 milliseconds of a field change, with no visible loading delay.
- **SC-005**: Every order status transition generates the correct ledger transactions — zero manual correction needed after transitioning 100 test orders through their full lifecycle.
- **SC-006**: The Saldos report accurately reflects the net balance for every customer at any point in time, with no reconciliation errors when verified against raw transaction records.
- **SC-007**: The system prevents data leakage between organizations — an operator in Organization A cannot see or access Organization B's orders, items, or transactions.

---

## Assumptions

- **Order status and payment are independent.** The order lifecycle (`Pending → Active → Delivering → Delivered → Completed`) tracks fulfillment and delivery only. Transitioning to `Completed` does NOT imply the customer has paid — it is a manual operational marker. An order can be `Completed` with an outstanding customer balance, and that is valid by design.
- **Customer balances are derived exclusively from Transaction records** (Charges, Payments, Voids). Order status has no bearing on balance calculations. The Saldos report and Payment Registry operate entirely from the Transaction ledger, independent of order status.
- A detailed per-customer transaction breakdown report (showing which specific charges and payments make up a balance) is a recognized future need but is **out of scope for Phase 1**. Phase 1 delivers only the aggregate net balance per customer.
- No authentication or authorization is required for Phase 1 — all users have full access.
- A single `Configuration` record per organization is expected; the system uses the most recently updated one as the active default.
- All monetary amounts are stored as decimal values with sufficient precision for Costa Rican colones (CRC) and USD.
- The UI language is Spanish; all source code, variable names, database fields, and logs are in English.
- AdminLTE 3.0.5 with Bootstrap and jQuery is already integrated in the project layout.
- jsGrid is used for all CRUD grid interactions and is already present in the project.
- The project already has all domain entity models defined in C# and initial EF Core migrations applied.
- A new `OrderStatusHistory` entity must be added to the domain model and database schema as part of this implementation.
- The `ICurrentOrganizationService` approach is used to provide `OrganizationId` context to the database layer for global query filters — it reads from the HTTP session.
- Azure deployment and CI/CD are out of scope for Phase 1.
- Automated tests are out of scope for Phase 1.
