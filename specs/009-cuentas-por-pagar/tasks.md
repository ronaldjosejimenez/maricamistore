# Tasks: Módulo Cuentas por Pagar (CxP)

**Input**: Design documents from `specs/009-cuentas-por-pagar/`

**Prerequisites**: plan.md ✅, spec.md ✅, data-model.md ✅, contracts/cxp-handlers.md ✅, research.md ✅

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on concurrent tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Exact file paths per task

---

## Phase 2: Foundational — Data Layer

**Purpose**: New DB tables + modified Order entity. MUST be complete before any user story work.

**⚠️ CRITICAL**: All user story phases depend on this phase.

- [x] T001 Add `ActualShippingAmountToCR` property (decimal, default 0) to `MariCamiStore/Model/Order.cs`
- [x] T002 [P] Create `MariCamiStore/Model/PeriodControl.cs` with fields: Id (Guid), OrganizationId (Guid), TransactionMonth (int), TransactionYear (int), ExchangeRate (decimal), PagosRealizados (decimal, default 0), EnCuenta (decimal, default 0), IsClosed (bool, default false), CreatedAt (DateTime) — per data-model.md
- [x] T003 [P] Create `MariCamiStore/Model/CxPEntry.cs` with fields: Id (Guid), PeriodControlId (Guid), CurrencyId (Guid), Amount (decimal), Reference (string), Type (string), OrderId (Guid?, nullable), CreatedAt (DateTime) — per data-model.md
- [x] T004 [P] Create `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/PeriodControlEntityTypeConfiguration.cs` — UNIQUE index on (OrganizationId, TransactionMonth, TransactionYear), decimal(18,4) for ExchangeRate, decimal(18,2) for monetary fields
- [x] T005 [P] Create `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CxPEntryEntityTypeConfiguration.cs` — FK to PeriodControl (CASCADE), FK to Currency (RESTRICT), FK to Order (SET NULL), string(500) for Reference, string(30) for Type, decimal(18,2) for Amount
- [x] T006 Modify `MariCamiStore/Infrastructure/Persistance/MariCamiStoreContext.cs`: add `DbSet<PeriodControl> PeriodControls`, `DbSet<CxPEntry> CxPEntries`, call `builder.ApplyConfiguration(new PeriodControlEntityTypeConfiguration())` and `CxPEntryEntityTypeConfiguration()`, add `builder.Entity<PeriodControl>().HasQueryFilter(p => p.OrganizationId == currentOrganizationService.OrganizationId)`
- [x] T007 Generate EF migration from `MariCamiStore/` project directory: `dotnet ef migrations add AddCxPModule` then `dotnet ef database update` — verify migration creates PeriodControls table, CxPEntries table, adds ActualShippingAmountToCR column to Orders

**Checkpoint**: Database schema ready — `PeriodControls` and `CxPEntries` tables exist, `Orders.ActualShippingAmountToCR` column exists.

---

## Phase 3: User Story 1 — Ver panel financiero (Priority: P1) 🎯 MVP

**Goal**: El usuario puede navegar a `/CxP` y ver el panel de control con todos los indicadores del período abierto (o el formulario de inicialización si no existe período).

**Independent Test**: Navegar a `/CxP` con un período abierto con al menos una entrada CxP y verificar que los 9 indicadores se muestran con valores calculados correctamente.

### Implementation

- [x] T008 [P] [US1] Create `MariCamiStore/Services/ICxPService.cs` with interface definition + all DTOs: `CxPPeriodIndicatorsDto` (add `bool ExchangeRateWarning` field for zero-rate edge case), `CxPCurrencyBalance`, `CxPEntryDto`, `CreateManualCxPEntryRequest`, `UpdatePeriodFieldsRequest`, `InitPeriodRequest`, `DeleteEntryRequest` — per contracts/cxp-handlers.md and data-model.md
- [x] T009 [US1] Create `MariCamiStore/Services/CxPService.cs` skeleton with constructor injecting `MariCamiStoreContext`, `ICurrentOrganizationService`, `IPaymentService`, `ILogger<CxPService>`; implement `GetOpenPeriodAsync()` (returns PeriodControl with IsClosed=false for current org) and `InitializePeriodAsync(month, year, exchangeRate)` (validates no open period exists, creates new PeriodControl)
- [x] T010 [US1] Implement `CxPService.GetPeriodIndicatorsAsync(periodId)` in `MariCamiStore/Services/CxPService.cs`: load period + entries + currencies; calculate PorPagarPorMoneda (group by CurrencyId), PorPagarEnColones (non-local × ExchangeRate + local direct), SaldosPorCobrar (paymentService.GetSaldosReportAsync().Sum), ShippingCRPendientesDeAplicar (Active orders SUM(ShippingAmountToCR) × ExchangeRate), DeudaAPagar, PendienteDeRecoger, Posicion; if `period.ExchangeRate == 0`, set all conversion-dependent fields to 0 and include `exchangeRateWarning: true` in the DTO so the UI can show a visible warning (spec edge case)
- [x] T011 [US1] Implement `CxPService.GetEntriesByPeriodAsync(periodId)` in `MariCamiStore/Services/CxPService.cs`: query CxPEntries joined with Currency, map to `CxPEntryDto` list (include CurrencyName and Sign); the raw `Type` field is returned as-is (e.g., "AutoActiva") — display label mapping is handled in T015
- [x] T012 [US1] Register `ICxPService → CxPService` as `Scoped` in `MariCamiStore/Extensions/ApplicationExtensions.cs`
- [x] T013 [US1] Create `MariCamiStore/Pages/CxP/Index.cshtml.cs` inheriting `OrganizationPageModel`; inject `ICxPService`, `ICatalogService`, `ICurrentOrganizationService`; implement `OnGetAsync()` (loads currencies + local currency sign for view), `OnGetPeriodAsync()` (returns `JsonResult` from `GetPeriodIndicatorsAsync` or `{noPeriod:true}` if no open period), `OnGetEntriesAsync()` (returns `JsonResult` from `GetEntriesByPeriodAsync`), `OnPostInitPeriodAsync([FromBody] InitPeriodRequest)` (validates month 1–12, year ≥ 2020, exchangeRate > 0, calls `InitializePeriodAsync`)
- [x] T014 [US1] Create `MariCamiStore/Pages/CxP/Index.cshtml`: (a) **empty-state section** `#cxp-init-section` with form fields Mes / Año / Tipo de Cambio + "Inicializar" button, hidden by default; (b) **panel card** `#cxp-panel` with display fields for all 9 indicators, Posición displayed in `<h3 class="font-weight-bold" id="posicion-value">`, editable inputs `#tc-input`, `#pagos-input`, `#en-cuenta-input` with "Guardar" button, "Cerrar Mes" button; (c) **tables container** `#cxp-tables-container`; include `<script src="~/js/pages/cxp/index.js" asp-append-version="true"></script>`
- [x] T015 [US1] Create `MariCamiStore/wwwroot/js/pages/cxp/index.js`: `loadPeriod()` calls `GET ?handler=Period`, if `noPeriod` shows `#cxp-init-section` and hides `#cxp-panel`, otherwise populates all indicator values using `formatAmount()` from utilities.js; if response contains `exchangeRateWarning: true`, display a visible Bootstrap alert in the panel "Tipo de cambio es 0 — los indicadores de conversión muestran 0"; `loadEntries()` calls `GET ?handler=Entries`, renders one Bootstrap card per currency with entries table + subtotal; apply type display label mapping in table rows: `"AutoActiva" → "Auto-Activa"`, `"AutoDelivered" → "Auto-Entregada"`, `"SaldoAnterior" → "Saldo Anterior"`, `"Manual"` stays "Manual" (FR-016); init form submit calls `POST ?handler=InitPeriod` then `loadPeriod()` + `loadEntries()`; call both on `$(document).ready`

**Checkpoint**: Navigating to `/CxP` shows either the init form (empty DB) or the financial panel with all 9 indicators populated.

---

## Phase 4: User Story 2 — Registrar entrada manual (Priority: P2)

**Goal**: El usuario puede crear y eliminar entradas CxP manuales en el período abierto, y las tablas + indicadores se actualizan inmediatamente.

**Independent Test**: Hacer clic en "+ Agregar entrada", ingresar referencia + moneda + monto, confirmar, y verificar que la entrada aparece en la tabla de la moneda seleccionada y el panel recalcula "Por pagar".

### Implementation

- [x] T016 [P] [US2] Implement `CxPService.CreateManualEntryAsync(periodId, req)` in `MariCamiStore/Services/CxPService.cs`: validate period is open, currencyId exists, amount > 0, reference not empty; create `CxPEntry` with Type="Manual"
- [x] T017 [P] [US2] Implement `CxPService.DeleteEntryAsync(entryId)` in `MariCamiStore/Services/CxPService.cs`: load entry, validate its PeriodControl has IsClosed=false, delete entry
- [x] T018 [US2] Add `OnPostAddEntryAsync([FromBody] CreateManualCxPEntryRequest)` and `OnPostDeleteEntryAsync([FromBody] DeleteEntryRequest)` handlers to `MariCamiStore/Pages/CxP/Index.cshtml.cs`
- [x] T019 [US2] Add to `MariCamiStore/Pages/CxP/Index.cshtml`: (a) "+ Agregar entrada" button visible only when period is open; (b) modal `#modal-add-entry` with fields Referencia (text, required), Moneda (select from catalog), Monto (number > 0); (c) delete icon button on each entry row (data-entry-id attribute)
- [x] T020 [US2] Add to `MariCamiStore/wwwroot/js/pages/cxp/index.js`: `addEntry()` function (POST ?handler=AddEntry, re-calls loadPeriod + loadEntries on success); `deleteEntry(entryId)` function (POST ?handler=DeleteEntry, re-calls loadPeriod + loadEntries on success); wire up submit event on add-entry modal form and click event on delete buttons using event delegation

**Checkpoint**: User can add manual entries and delete any entry; tables and panel indicators update without page refresh.

---

## Phase 5: User Story 3 — Entradas automáticas (Priority: P3)

**Goal**: Al activar una orden se crea automáticamente una entrada CxP (proveedor); al entregarla se crea la entrada de shipping real (capturado en el diálogo).

**Independent Test**: Activar una orden con período abierto y verificar que `/CxP` muestra automáticamente una entrada AutoActiva con el monto correcto al proveedor en la moneda de la orden.

### Implementation

- [x] T021 [US3] Extend `TransitionOrderDto` record in `MariCamiStore/Services/IOrderService.cs`: add `decimal? ActualShippingAmountToCR = null` as last parameter (all existing callers remain compatible)
- [x] T022 [US3] Inject `ICxPService cxpService` and `ILogger<OrderService> logger` into `OrderService` constructor in `MariCamiStore/Services/OrderService.cs`
- [x] T023 [US3] Implement `CxPService.CreateAutoEntryAsync(periodId, orderId, currencyId, amount, reference, type)` (internal helper) in `MariCamiStore/Services/CxPService.cs`: creates CxPEntry with given type and OrderId set
- [x] T024 [US3] Modify `OrderService.TransitionOrderAsync` in `MariCamiStore/Services/OrderService.cs` — after existing `context.SaveChangesAsync()`: (a) if `toStatus == Active`: get open period → if exists call `cxpService.CreateAutoEntryAsync(period.Id, order.Id, order.CurrencyId, order.TotalToPayToSupplier, order.NameOfOrder, "AutoActiva")`; if no period log warning and continue; (b) if `toStatus == Delivered`: set `order.ActualShippingAmountToCR = dto.ActualShippingAmountToCR ?? order.ShippingAmountToCR`, save order, get open period → if exists call `cxpService.CreateAutoEntryAsync(period.Id, order.Id, order.CurrencyId, order.ActualShippingAmountToCR, order.NameOfOrder, "AutoDelivered")`
- [x] T025 [US3] Add "Shipping real a CR" input field to the Delivered transition modal in `MariCamiStore/Pages/Orders/Index.cshtml` (note: modal is on Index, not Items): label "Shipping real a CR", input `id="actual-shipping-amount"`, pre-populated from JS with current order's `ShippingAmountToCR` value (decimal input, ≥ 0)
- [x] T026 [US3] Modify `MariCamiStore/wwwroot/js/pages/orders/index.js`: when toStatus === 'Delivered', show `#actual-shipping-group` pre-populated with `item.shippingAmountToCR`; include `actualShippingAmountToCR: parseFloat(...)` in the transition POST request body

**Checkpoint**: Activating an order creates an AutoActiva entry in `/CxP`; marking Delivered with a custom shipping amount creates an AutoDelivered entry.

---

## Phase 6: User Story 4 — Cerrar mes (Priority: P4)

**Goal**: El usuario puede cerrar el período actual; el sistema bloquea las entradas del período cerrado, crea el nuevo período, y registra el "Saldo anterior".

**Independent Test**: Ejecutar el cierre de un mes con entradas y verificar que: (a) el período queda bloqueado (no se puede agregar entradas), (b) el nuevo mes aparece con una entrada "Saldo Anterior" en Colones igual al valor de "Deuda a Pagar" del período cerrado.

### Implementation

- [x] T027 [US4] Implement `CxPService.ClosePeriodAsync(periodId)` in `MariCamiStore/Services/CxPService.cs`: validate period exists and is open; calculate `DeudaAPagar = PorPagarEnColones - PagosRealizados` (via GetPeriodIndicatorsAsync or inline); set `period.IsClosed = true`; determine next month/year (month=12 → month=1, year+1); load `Configuration` for ExchangeRate and LocalCurrencyId; create new `PeriodControl` for next month; create `CxPEntry` in new period: Type="SaldoAnterior", Amount=DeudaAPagar, CurrencyId=LocalCurrencyId, Reference="Saldo anterior"
- [x] T028 [US4] Add `OnPostClosePeriodAsync()` handler to `MariCamiStore/Pages/CxP/Index.cshtml.cs`
- [x] T029 [US4] Add "Cerrar Mes" button (disabled when period is closed) and confirmation modal `#modal-close-period` to `MariCamiStore/Pages/CxP/Index.cshtml`
- [x] T030 [US4] Add `closePeriod()` function to `MariCamiStore/wwwroot/js/pages/cxp/index.js`: POST `?handler=ClosePeriod`, on success reload the full page (window.location.reload()) to reflect new active period

**Checkpoint**: Closing a month locks it (add/delete buttons disappear or are disabled), new month appears with Saldo Anterior entry.

---

## Phase 7: User Story 5 — Editar campos manuales (Priority: P5)

**Goal**: El usuario puede actualizar Tipo de Cambio, Pagos Realizados y En Cuenta del período abierto; los indicadores dependientes se recalculan al guardar.

**Independent Test**: Modificar el Tipo de Cambio y hacer clic en "Guardar"; verificar que "Por pagar en Colones", "Deuda a Pagar" y "Posición" se actualizan con el nuevo valor.

### Implementation

- [x] T031 [US5] Implement `CxPService.UpdatePeriodFieldsAsync(periodId, req)` in `MariCamiStore/Services/CxPService.cs`: validate period is open, `req.ExchangeRate ≥ 0`, `req.PagosRealizados ≥ 0`, `req.EnCuenta ≥ 0`; update PeriodControl fields and save
- [x] T032 [US5] Add `OnPostUpdatePeriodAsync([FromBody] UpdatePeriodFieldsRequest)` handler to `MariCamiStore/Pages/CxP/Index.cshtml.cs`
- [x] T033 [US5] Enable editable inputs for Tipo de Cambio (`#tc-input`), Pagos Realizados (`#pagos-input`), En Cuenta (`#en-cuenta-input`) in `MariCamiStore/Pages/CxP/Index.cshtml` — disabled/read-only when period is closed
- [x] T034 [US5] Add `savePeriodFields()` function to `MariCamiStore/wwwroot/js/pages/cxp/index.js`: read values from inputs, POST `?handler=UpdatePeriod`, on success call `loadPeriod()` to refresh all indicator values

**Checkpoint**: All 5 user stories complete — the CxP module is fully functional.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Navigation wiring and final integration.

- [x] T035 Add new sidebar section `menuCxP` to `MariCamiStore/Pages/Shared/_Layout.cshtml` after the Finance section: `<li class="nav-item has-treeview">` with icon `fas fa-file-invoice-dollar`, title "Cuentas por Pagar", child link `asp-page="/CxP/Index"` with `data-menu-group="menuCxP"` and label "Control CxP"
- [x] T036 Verified `formatMoney(amount, sign)` (correct function name from utilities.js) is used consistently throughout `MariCamiStore/wwwroot/js/pages/cxp/index.js`
- [ ] T037 Smoke-test the full flow: init period → add manual entry → activate order (AutoActiva) → deliver order (AutoDelivered) → edit TC → close month → verify new period with SaldoAnterior — navigate to `/CxP` at each step to confirm indicators

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately. BLOCKS all US phases.
- **US1 (Phase 3)**: Depends on Phase 2 completion.
- **US2 (Phase 4)**: Depends on Phase 3 (needs CxPService + page skeleton).
- **US3 (Phase 5)**: Depends on Phase 2 (models must exist); T023 depends on CxPService from Phase 3.
- **US4 (Phase 6)**: Depends on Phase 3 (CxPService + page).
- **US5 (Phase 7)**: Depends on Phase 3 (CxPService + page).
- **Polish (Phase 8)**: Depends on all previous phases.

### User Story Dependencies

- **US1 (P1)**: Directly after Phase 2 — no other story dependency.
- **US2 (P2)**: After US1 (needs page skeleton + CxPService base).
- **US3 (P3)**: After Phase 2 (models) + needs CxPService.CreateAutoEntryAsync from Phase 3.
- **US4 (P4)**: After US1 (needs page + CxPService base).
- **US5 (P5)**: After US1 (needs page + CxPService base).

### Within Each Phase

- [P]-marked tasks can run simultaneously (different files).
- T006 depends on T002–T005 (needs models before context).
- T007 depends on T006 (needs DbSets registered before migration).
- T009–T011 can run after T008 (interface created).
- T013–T015 can run after T012 (service registered).

### Parallel Opportunities (Phase 2)

```
T001, T002, T003, T004, T005 → all parallel (different files)
       ↓
      T006 (needs T002–T005)
       ↓
      T007 (migration)
```

### Parallel Opportunities (Phase 3)

```
T008 (interface) → T009, T010, T011 (all need interface, can interleave)
T012 (registration) → T013, T014, T015 (need service registered)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (T001–T007)
2. Complete Phase 3: User Story 1 (T008–T015)
3. **STOP and VALIDATE**: Navigate to `/CxP`, initialize first period, verify panel shows indicators
4. Deliver/demo if ready

### Incremental Delivery

1. Phase 2 → Foundation (DB ready)
2. Phase 3 → Panel visible → MVP
3. Phase 4 → Manual entries
4. Phase 5 → Auto entries from orders
5. Phase 6 → Month close
6. Phase 7 → Editable fields
7. Phase 8 → Navigation + polish

---

## Notes

- **No test tasks** — no automated tests in this project
- All AJAX POST handlers require `@Html.AntiForgeryToken()` (already in `_Layout.cshtml`)
- `formatAmount(amount, sign)` helper is available from `utilities.js` — use it for all CxP monetary values
- `LocalCurrencyId` is available from `Configuration` — same pattern used in `OrderService.BuildTransaction`
- Periods use `OrganizationId` global query filter — no manual org filtering needed in service queries
- The `[P]` marker means the task operates on a different file from its siblings — they can be sent to parallel agents safely
