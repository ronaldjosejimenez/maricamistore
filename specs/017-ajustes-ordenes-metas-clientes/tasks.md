# Tasks: Ajustes en Órdenes, Metas y Clientes

**Input**: Design documents from `specs/017-ajustes-ordenes-metas-clientes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-handlers.md, quickstart.md

**Tests**: No automated test project exists and the spec does not request tests. Verification is `dotnet build` plus [quickstart.md](quickstart.md).

**Organization**: one phase per user story. The three stories are independent and touch disjoint files.

## Phase 1: Setup

- [X] T001 Confirm the solution builds before changes: run `dotnet build` in `MariCamiStore/` and note any pre-existing warnings

## Phase 2: Foundational

None. The stories share no blocking prerequisites.

---

## Phase 3: User Story 1 - Flujo de entrega simplificado e ítems consultables (P1) 🎯 MVP

**Goal**: remove the Delivered status (code + data), relabel "Enviar" as "Iniciar entrega", and add a read-only "Ver" for items outside Pending.

**Independent Test**: quickstart §1.

- [X] T002 [US1] In `MariCamiStore/Model/OrderStatus.cs` delete the `Delivered` static field and remove it from `List()`. Keep XML doc comment style, and remove any now-unused references.
- [X] T003 [US1] In `MariCamiStore/Services/OrderService.cs`:
  - `ValidateTransition`: make the allowed map `Active → [Delivering, Voided]`, `Delivering → [Completed, Voided]`, `Completed → []` and drop the `Delivered` entry. Keep the existing message `Transición 'X' → 'Y' no permitida.`
  - Receipt check (~L169): allow only `OrderStatus.Delivering`, with the message `"Solo se puede marcar recepción en órdenes en estado Entregando."`
  - `ReasignarItemAsync` allowed list: `Active, Delivering, Completed`.
- [X] T004 [P] [US1] In `MariCamiStore/Pages/Orders/Index.cshtml.cs` update the next-status map (~L90): `"Delivering" => ["Completed", "Voided"]` and remove the `"Delivered"` entry.
- [X] T005 [P] [US1] In `MariCamiStore/wwwroot/js/pages/orders/index.js` (~L85) set the labels to `{ Active: 'Activar', Delivering: 'Iniciar entrega', Completed: 'Completar', Voided: 'Anular' }`.
- [X] T006 [US1] Create the data-only EF migration `RemoveDeliveredOrderStatus` in `MariCamiStore/Infrastructure/Persistance/Migrations/`. Generate it with `dotnet ef migrations add RemoveDeliveredOrderStatus` from `MariCamiStore/`; the model snapshot must be unchanged.
  - `Up`: the `migrationBuilder.Sql` from plan.md (INSERT into `dbo.OrderStatusHistory` with Notes `N'Migración automática: el estado Entregada fue eliminado (spec 017).'`, then UPDATE `dbo.Orders` Delivered → Completed with `UpdatedAt = SYSUTCDATETIME()`).
  - `Down`: empty, with a comment saying it is an irreversible data fix.
  - For the production run, generate an idempotent script with `dotnet ef migrations script --idempotent` (see the deploy-order note in plan.md).
- [X] T007 [US1] In `MariCamiStore/wwwroot/js/pages/orders/items.js`:
  - (a) L199: `var isDelivering = orderStatus === 'Delivering';`
  - (b) History rendering (~L704): map `fromStatus`/`toStatus` through a `statusLabels` map `{ Pending:'Pendiente', Active:'Activa', Delivering:'Entregando', Delivered:'Entregada', Completed:'Completada', Voided:'Anulada' }`, falling back to the raw key.
- [X] T008 [US1] In `MariCamiStore/wwwroot/js/pages/orders/items.js`:
  - Add `openViewItem(item)`. It calls `openEditItem(item)`, sets `#item-modal-title` to "Ver Ítem", disables every `input, select, textarea` inside `#itemModal .modal-body` (including select2 `#item-customer` via `.prop('disabled', true)` and `#item-product-type` again after its async load), hides `#btn-save-item`, and changes the Cancel button text to "Cerrar". In `MariCamiStore/Pages/Orders/Items.cshtml` (~L310) add `id="btn-cancel-item"` to the item modal's Cancel button and target it by that id.
  - On `#itemModal` `hidden.bs.modal`, restore the enabled state (respecting the product-type "no types" case), Save visibility, and the "Cancelar" text.
  - In row rendering (~L275), when `!isPending` add `<button class="btn btn-xs btn-info mr-1">Ver</button>` calling `openViewItem(item)` before "Ver Img". Keep "Ver Img" and "Reasignar" unchanged.
- [X] T009 [US1] Grep the repo (`*.cs`, `*.cshtml`, `*.js`, excluding Migrations) for remaining `Delivered` references to order status (not `OrderItemStatus`, not `CxPEntryType.AutoDelivered`), fix any leftovers, and run `dotnet build`.

**Checkpoint**: US1 is complete and verifiable via quickstart §1.

---

## Phase 4: User Story 2 - % de Comisión en Metas (P2)

**Goal**: commission rate, amount and color in Mes Actual and Histórico, computed server-side.

**Independent Test**: quickstart §2.

- [X] T010 [P] [US2] Create `MariCamiStore/Services/SalesGoalCommission.cs`: a static class with `For(decimal basePercentage)` returning `(decimal Rate, string Status)`: `< 85 → (0,"red")`, `< 86 → (2.5m,"yellow")`, `<= 110 → (5,"green")`, else `(6,"green-plus")`. Add `Amount(decimal actual, decimal rate)` returning `Math.Max(0, Math.Round(actual * rate / 100m, 2, MidpointRounding.AwayFromZero))`. Use XML doc comments like the other services.
- [X] T011 [US2] In `MariCamiStore/Services/ISalesGoalService.cs` append `decimal CommissionRate, decimal CommissionAmount, string CommissionStatus` to the `GoalDto` and `HistoryRowDto` records.
- [X] T012 [US2] In `MariCamiStore/Services/SalesGoalService.cs`:
  - Extract `private static decimal TodayPercentage(SalesGoal goal, <today type used in BuildDto> today)` from `BuildDto` (~L209-212) and call it from both `BuildDto` and `GetHistoryAsync`.
  - When building `GoalDto`, set the base = `todayPercentage` if `isCurrent`, else `goal.CompliancePercentage`, and fill the commission fields via `SalesGoalCommission`.
  - In `GetHistoryAsync`, keep the projection, then for the row matching the current Costa Rica year/month load that goal's days and compute today %. Set the commission fields for every row (current → today %, others → `CompliancePercentage`, amount from `ActualAmount`).
  - Fix every other `new GoalDto(`/`new HistoryRowDto(` call site so it compiles.
- [X] T013 [P] [US2] In `MariCamiStore/Pages/SalesGoals/Current.cshtml`:
  - Change the summary row to four `col-md-3` info-boxes.
  - Add a box with `<span class="info-box-text">% de Comisión</span>` and `<span class="info-box-number" id="commission-total" style="font-size:1.6rem;">0%</span> <small id="commission-amount"></small>`.
  - Add the CSS `.goal-highlight { box-shadow: 0 0 0 3px #155724, 0 0 12px rgba(40,167,69,.8); }`.
- [X] T014 [US2] In `MariCamiStore/wwwroot/js/pages/sales-goals/current.js` render the commission next to `#today-total`:
  - Text `fmtPct(goal.commissionRate)` and `'(' + sign + ' ' + fmt(goal.commissionAmount) + ')'`.
  - On the closest `.info-box`, remove `goal-red goal-yellow goal-green goal-highlight bg-light`, then add the class for `commissionStatus` (`red → goal-red`, `yellow → goal-yellow`, `green → goal-green`, `green-plus → goal-green goal-highlight`).
- [X] T015 [P] [US2] In `MariCamiStore/Pages/SalesGoals/History.cshtml` add `<th class="text-right">% Comisión</th>` after "% Cumplimiento" in `#history-table`, and add the same `.goal-highlight` rule as Current.cshtml for the highlighted state.
- [X] T016 [US2] In `MariCamiStore/wwwroot/js/pages/sales-goals/history.js` (~L25) add a cell after compliance containing a badge with `fmtPct(r.commissionRate)` (`badge-danger`/`badge-warning`/`badge-success`; `green-plus` adds the highlight class) followed by `<small> (sign amount)</small>`, using the existing `fmt` helper.

**Checkpoint**: US2 is complete and verifiable via quickstart §2.

---

## Phase 5: User Story 3 - Buscador de Clientes (P3)

**Goal**: a working global search and column filters in the customers grid.

**Independent Test**: quickstart §3.

- [X] T017 [P] [US3] In `MariCamiStore/Pages/Customers/Index.cshtml` add above `#jsGrid`: `<input id="customer-search" type="search" class="form-control mb-2" placeholder="Buscar por apodo, nombre, teléfono, email o dirección" autocomplete="off" />`.
- [X] T018 [US3] In `MariCamiStore/wwwroot/js/pages/customers/index.js`:
  - Add `var customerCache = null;`, a `norm(s)` helper (NFD, strip `̀-ͯ`, lowercase, trim), and the `SEARCH_FIELDS = ['nickName','name','phoneNumber','email','address']` constant.
  - `loadData(filter)` returns a Deferred: it fetches `?handler=Load` only when the cache is null, then filters so every non-empty column filter (from `filter`) is contained in its field and the global term matches at least one search field.
  - `#customer-search` `input` is debounced 200 ms and triggers `$('#jsGrid').jsGrid('search')`.
  - Add `onItemInserted`/`onItemUpdated`/`onItemDeleted` handlers that set `customerCache = null` and call `jsGrid('search')`.
  - Set `noDataContent: 'No se encontraron clientes'`.

**Checkpoint**: US3 is complete and verifiable via quickstart §3.

---

## Phase 6: Polish & Cross-Cutting

- [X] T019 Run `dotnet build` in `MariCamiStore/` with no new warnings or errors.
- [X] T020 Walk through `specs/017-ajustes-ordenes-metas-clientes/quickstart.md` statically: cross-check each step against the code and fix gaps.

## Dependencies & Execution Order

- T001 → all stories.
- **US1**: T002 → T003 and T009; T004 and T005 run in parallel with T003; T006 is independent of the code tasks; T007 → T008 (same file).
- **US2**: T010 and T011 → T012; T013 → T014; T015 → T016. T013 and T015 can run in parallel with T010–T012.
- **US3**: T017 and T018 can run in parallel (different files).
- The stories are mutually independent; US1, US2 and US3 can proceed in parallel.
- T019 and T020 come after all stories.

## Parallel Example

```text
US1: T004 (Index.cshtml.cs) | T005 (index.js) | T006 (migration)
US2: T010 (SalesGoalCommission.cs) | T013 (Current.cshtml) | T015 (History.cshtml)
US3: T017 (Customers/Index.cshtml) | T018 (customers/index.js)
```

## Implementation Strategy

MVP = US1 (the only data-affecting change). Then US2, then US3. Each checkpoint can be validated independently, and all three ship in one PR.
