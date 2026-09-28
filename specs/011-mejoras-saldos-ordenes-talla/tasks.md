---
description: "Task list for 011-mejoras-saldos-ordenes-talla"
---

# Tasks: Total de Saldos, Filtro de Estados en Órdenes y Talla en Ítems

**Input**: Design documents from `specs/011-mejoras-saldos-ordenes-talla/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/page-handlers.md, quickstart.md

**Tests**: No automated test project exists and tests were not requested. Verification = `dotnet build` + manual scenarios in quickstart.md.

**Paths**: All source paths are relative to repository root; the web project lives in `MariCamiStore/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = Saldos total, US2 = Status checkboxes, US3 = Volver keeps selection, US4 = Talla

---

## Phase 1: Setup

- [ ] T001 Verify baseline builds before changes: run `dotnet build MariCamiStore/MariCamiStore.csproj` and confirm success

---

## Phase 2: Foundational

No shared blocking prerequisites: the three improvements touch disjoint areas. (US3 depends on US2.)

---

## Phase 3: User Story 1 - Saldo neto total (Priority: P1) 🎯 MVP

**Goal**: Footer "Total" row in "Saldos de Clientes" with the net sum of visible rows.

**Independent Test**: quickstart.md §1.

- [ ] T002 [US1] In `MariCamiStore/wwwroot/js/pages/payments/index.js` `renderSaldos`: keep the existing "No hay saldos registrados." branch (no total) when `data` is empty; when `filtered` is empty render a single body row `<tr><td colspan="2" class="text-muted">Ningún cliente coincide con el filtro</td></tr>`; compute `total = Math.round(filtered.reduce((s, r) => s + r.balance, 0) * 100) / 100`; append `<tfoot><tr class="font-weight-bold"><td>Total</td><td class="text-right">` + `(total < 0 ? '−' : '') + formatMoney(Math.abs(total), localCurrencySign)` + `</td></tr></tfoot>` (no badge, no row color). Filter input and post-payment `loadSaldos()` already call `renderSaldos`, so recalculation is automatic (FR-001..FR-005a).

**Checkpoint**: US1 complete and verifiable independently.

---

## Phase 4: User Story 2 - Filtro por casillas de estado (Priority: P2)

**Goal**: Replace the status combo with six checkboxes (default Pendiente + Activa), auto-reload, empty selection = empty list.

**Independent Test**: quickstart.md §2.

- [ ] T003 [P] [US2] Add a static parsing helper in `MariCamiStore/Model/OrderStatus.cs`: `public static IReadOnlyList<string> DefaultFilterKeys => new[] { Pending.Key, Active.Key };` and `public static IReadOnlyList<string> ParseFilter(string? raw, bool present)` implementing data-model.md rules: `!present` → default; present and `string.IsNullOrWhiteSpace(raw)` → empty list; otherwise split on ',' (trim, RemoveEmptyEntries), keep keys that exist in `List()` (ordinal match), distinct, ordered as `List()`; if values were given but none valid → default.
- [ ] T004 [P] [US2] Change `GetOrdersAsync` in `MariCamiStore/Services/IOrderService.cs` and `MariCamiStore/Services/OrderService.cs` to `Task<List<Order>> GetOrdersAsync(IReadOnlyCollection<string> statuses)`: if `statuses.Count == 0` return empty list; otherwise `Where(o => statuses.Contains(o.Status))` ordered by `CreatedAt` desc (remove "empty = all" behavior).
- [ ] T005 [US2] In `MariCamiStore/Pages/Orders/Index.cshtml.cs`: add `public IReadOnlyList<string> SelectedStatuses { get; private set; } = OrderStatus.DefaultFilterKeys;` set in `OnGetAsync` from `OrderStatus.ParseFilter(Request.Query["statuses"], Request.Query.ContainsKey("statuses"))`; change `OnGetLoadAsync(string? statusFilter = "Pending,Active")` to `OnGetLoadAsync(string? statuses)` using `OrderStatus.ParseFilter(statuses, Request.Query.ContainsKey("statuses"))` and the new `GetOrdersAsync` signature (depends on T003, T004).
- [ ] T006 [US2] In `MariCamiStore/Pages/Orders/Index.cshtml`: remove the `<select id="statusFilter">` block and render, in the same header area, one inline checkbox per `OrderStatus.List()` — `<div class="form-check form-check-inline"><input class="form-check-input status-filter" type="checkbox" id="status-@s.Key" value="@s.Key" @(Model.SelectedStatuses.Contains(s.Key) ? "checked" : "")><label class="form-check-label" for="status-@s.Key">@s.Name</label></div>` (add `@using MariCamiStore.Model` if needed) (depends on T005).
- [ ] T007 [US2] In `MariCamiStore/wwwroot/js/pages/orders/index.js`: add `function getSelectedStatuses() { return $('.status-filter:checked').map(function () { return this.value; }).get().join(','); }` and `var pendingLoad = null;`; replace both `loadData` implementations in `loadGrid` with one function that reads `getSelectedStatuses()` at call time, returns `[]` (no AJAX) when empty, otherwise aborts `pendingLoad` if in flight and returns `pendingLoad = $.get('?handler=Load&statuses=' + encodeURIComponent(sel))`; replace `$('#statusFilter').on('change', loadGrid)` with `$(document).on('change', '.status-filter', loadGrid)`; remove all references to `#statusFilter` (FR-008, FR-009, FR-013).

**Checkpoint**: US2 complete; Orders filters by any combination.

---

## Phase 5: User Story 3 - Volver conserva la selección (Priority: P2)

**Goal**: Selection travels Orders → Items → Volver via `statuses` query parameter.

**Independent Test**: quickstart.md §3. Depends on US2.

- [ ] T008 [US3] In `MariCamiStore/wwwroot/js/pages/orders/index.js` Items link: change href to `'/Orders/Items?orderId=' + item.id + '&statuses=' + encodeURIComponent(getSelectedStatuses())` (depends on T007).
- [ ] T009 [US3] In `MariCamiStore/Pages/Orders/Items.cshtml.cs`: change `OnGetAsync(Guid orderId)` to `OnGetAsync(Guid orderId, string? statuses)`; set `ViewData["ReturnStatuses"] = Request.Query.ContainsKey("statuses") ? (statuses ?? string.Empty) : null;`.
- [ ] T010 [US3] In `MariCamiStore/Pages/Orders/Items.cshtml` line ~18: replace `<a href="/Orders" ...>` "Volver" with an href built as `ViewData["ReturnStatuses"] is string rs ? "/Orders?statuses=" + Uri.EscapeDataString(rs) : "/Orders"`, keeping classes and icon (depends on T009).

**Checkpoint**: Volver restores selection; menu entry uses defaults.

---

## Phase 6: User Story 4 - Talla en ítems (Priority: P3)

**Goal**: New `Size` field on OrderItem, editable only in the create/edit item modal.

**Independent Test**: quickstart.md §4.

- [ ] T011 [P] [US4] Add `public string Size { get; set; } = string.Empty;` with XML doc comment (`/// <summary>Gets or sets the size (talla) of the garment.</summary>`) to `MariCamiStore/Model/OrderItem.cs` after `ProductSourceCode`.
- [ ] T012 [US4] In `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderItemEntityTypeConfiguration.cs` add `builder.Property(oi => oi.Size).IsRequired().HasMaxLength(20).HasDefaultValue(string.Empty);` (depends on T011).
- [ ] T013 [US4] Generate migration from `MariCamiStore/`: `dotnet ef migrations add AddOrderItemSize --output-dir Infrastructure/Persistance/Migrations`; verify the generated `Up` adds `Size nvarchar(20) NOT NULL defaultValue: ""` to `OrderItems` and `Down` drops it; confirm `MariCamiStoreContextModelSnapshot.cs` updated. **Do NOT run `dotnet ef database update`** (connection strings may point to shared/production databases) (depends on T012).
- [ ] T014 [US4] In `MariCamiStore/Services/IOrderService.cs` (or wherever `OrderItemWithCustomerDto` is declared) add `string Size` to the record, and in `MariCamiStore/Services/OrderService.cs` `GetOrderItemsWithCustomerAsync` pass `i.Size` in the matching position (depends on T011).
- [ ] T015 [US4] In `MariCamiStore/Pages/Orders/Items.cshtml.cs`: add `string? Size` to `OrderItemDto` (after `ProductSourceCode`); in `OnPostInsertAsync` and `OnPostUpdateAsync` compute `var size = (dto.Size ?? string.Empty).Trim();` and if `size.Length > 20` return `new JsonResult(new { error = "La talla no puede superar 20 caracteres." })` before saving; assign `Size = size` / `existing.Size = size`; include `Size` in both JSON responses (depends on T011).
- [ ] T016 [US4] In `MariCamiStore/Pages/Orders/Items.cshtml` item modal: add next to "Código Fuente" (same row, `col-md-6`) a form-group `<label>Talla</label><input id="item-size" type="text" class="form-control" maxlength="20" placeholder="Ej: M, 38, 10-12 años" />`.
- [ ] T017 [US4] In `MariCamiStore/wwwroot/js/pages/orders/items.js`: prefill `$('#item-size').val(item.size || '')` where the edit modal sets `#item-product-source-code`; clear it wherever the create modal/form resets fields; send `size: $('#item-size').val() || ''` in the insert/update payload next to `productSourceCode`. Do NOT add a grid column (depends on T015, T016).
- [ ] T018 [US4] Verify `ReasignarItemAsync` in `MariCamiStore/Services/OrderService.cs` does not overwrite `Size` (it should only change customer/price on the tracked entity); fix only if it rebuilds the entity (FR-019).

**Checkpoint**: Talla editable, persisted, trimmed, validated.

---

## Phase 7: Polish & Cross-Cutting

- [ ] T019 Run `dotnet build MariCamiStore/MariCamiStore.csproj` and fix any errors/warnings introduced by this feature.
- [ ] T020 Search for leftover references: `statusFilter` in `MariCamiStore/` (Pages, wwwroot) must return no matches; `GetOrdersAsync(` callers compile against the new signature.
- [ ] T021 Walk through `specs/011-mejoras-saldos-ordenes-talla/quickstart.md` scenarios that can be checked by code reading (DB-dependent steps are left for the user) and note results.

---

## Dependencies & Execution Order

- Setup (T001) → all stories.
- US1 (T002): independent.
- US2: T003 ∥ T004 → T005 → T006; T007 after T005 (uses Load contract).
- US3: depends on US2 (T007) → T008; T009 → T010.
- US4: T011 → T012 → T013; T011 → T014, T015; T016 ∥ T014/T015; T017 after T015+T016; T018 after T011.
- Polish after all stories.

### Parallel Opportunities

- T002 (US1), T003/T004 (US2), T011 (US4) touch different files and can start together.
- Within US4: T014, T015, T016 are in different files once T011 is done.

## Implementation Strategy

1. MVP: US1 (single JS change) — immediately useful.
2. US2 + US3 together (Orders filter end-to-end).
3. US4 (schema change + migration) last; the migration is applied to databases by the user (or at deploy), not during implementation.
