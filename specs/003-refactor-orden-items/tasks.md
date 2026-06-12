# Tasks: Refactor Orden e Ítems

**Input**: Design documents from `specs/003-refactor-orden-items/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: No test tasks — out of scope per plan.md.

**Organization**: Tasks agrupadas por capa de implementación, con etiquetas de user story para trazabilidad. Las capas reflejan las dependencias técnicas entre archivos.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Puede correr en paralelo (archivos distintos, sin dependencias incompletas)
- **[Story]**: User story a la que pertenece la tarea (US1–US7 del spec.md)

---

## Phase 1: Setup

No se requiere inicialización de proyecto — el proyecto .NET 10 Razor Pages está completamente operativo. Pasar directamente a la Fase Fundacional.

---

## Phase 2: Foundational — Migración de DB (US7 — P0 Bloqueante)

**Purpose**: La migración de DB es el único prerequisito que bloquea TODAS las demás tareas. Ningún cambio de código puede compilarse sin los cambios al modelo, y el modelo no puede cambiarse antes de tener la migración lista.

**⚠️ Completar antes de cualquier otra fase**

- [x] T001 [US7] Modify `MariCamiStore/Model/Order.cs`: remove property `RequestedAt`, add `public decimal TotalAgreedPriceInLocal { get; set; }`
- [x] T002 [P] [US7] Modify `MariCamiStore/Model/OrderItem.cs`: rename property `ListPriceTax` → `ListPriceTaxWithTax`
- [x] T003 [P] [US7] Modify `MariCamiStore/Infrastructure/Persistance/EntityTypeConfigurations/OrderEntityTypeConfiguration.cs`: remove `RequestedAt` configuration; add `builder.Property(o => o.TotalAgreedPriceInLocal).HasColumnType("decimal(18,2)").HasDefaultValue(0m)`
- [x] T004 [P] [US7] Modify `MariCamiStore/Infrastructure/Persistance/EntityTypeConfigurations/OrderItemEntityTypeConfiguration.cs`: rename property reference `ListPriceTax` → `ListPriceTaxWithTax`
- [x] T005 [US7] Add EF Core migration from repo root: `dotnet ef migrations add RefactorOrderItems --project MariCamiStore` — verify the generated migration uses `RenameColumn(table: "OrderItems", name: "ListPriceTax", newName: "ListPriceTaxWithTax")`, `DropColumn(table: "Orders", name: "RequestedAt")`, and `AddColumn<decimal>(table: "Orders", name: "TotalAgreedPriceInLocal", defaultValue: 0m)`
- [x] T006 [US7] Apply migration: `dotnet ef database update --project MariCamiStore` — verify 0 errors

**Checkpoint**: Modelo actualizado y DB sincronizada. El proyecto debe compilar sin errores después de T001–T004 + T005–T006.

---

## Phase 3: Services — Cálculo Suite + Filtro Moneda (US6, US5)

**Purpose**: Actualizar la capa de servicios para soportar el nuevo campo `TotalAgreedPriceInLocal` y agregar el método de filtrado de ProductTypes por moneda.

**Prerequisito**: Phase 2 completa.

- [x] T007 [US6] Modify `MariCamiStore/Services/IOrderService.cs`: add `decimal TotalAgreedPriceInLocal` to `OrderTotalsDto` record
- [x] T008 [US6] Modify `MariCamiStore/Services/OrderService.cs`: update `UpdateOrderTotalsAsync` to persist `TotalAgreedPriceInLocal`; update all references from `ListPriceTax` → `ListPriceTaxWithTax`; remove any references to `RequestedAt`
- [x] T009 [P] [US5] Modify `MariCamiStore/Services/ICatalogService.cs`: add method signature `Task<List<ProductType>> GetProductTypesByCurrencyAsync(Guid currencyId)`
- [x] T010 [P] [US5] Modify `MariCamiStore/Services/CatalogService.cs`: implement `GetProductTypesByCurrencyAsync` — query `ProductTypes.Where(pt => pt.CurrencyId == currencyId).OrderBy(pt => pt.Name).ToListAsync()`

**Checkpoint**: Services compilando. `OrderTotalsDto` con campo nuevo. `GetProductTypesByCurrencyAsync` disponible.

---

## Phase 4: Items Page Model — Handlers (US1, US3, US5, US6)

**Purpose**: Actualizar los handlers existentes y agregar los nuevos en `Items.cshtml.cs`. Toda la página se modifica en una sola pasada.

**Prerequisito**: Phase 3 completa.

- [x] T011 [US6] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: update `OnGetLoadAsync` — project `listPriceTaxWithTax` (renamed field), add `HasImage = item.ProductImage != null` to each result item, do NOT include binary `ProductImage` in load response
- [x] T012 [US3] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: update `OnPostInsertAsync` — change parameter from `OrderItem` to a new `OrderItemDto` record that includes `string? ProductImageBase64`; decode base64 → `byte[]`; validate `byte[].Length <= 2097152` (2 MB) returning `{ error: "La imagen supera el límite de 2 MB." }` if exceeded; map dto fields to new `OrderItem` instance including `ProductLink`, `ProductSourceCode`, `ProductImage`
- [x] T013 [US3] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: update `OnPostUpdateAsync` — same `OrderItemDto` as T012 + `Id` field; if `ProductImageBase64` is null, preserve existing image; if empty string, clear image; update `ListPriceTaxWithTax` (renamed)
- [x] T014 [US5] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: add `OnGetProductTypesByCurrencyAsync(Guid currencyId)` handler — calls `ICatalogService.GetProductTypesByCurrencyAsync(currencyId)`, returns `[{ id, name }]`
- [x] T015 [US3] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: add `OnGetItemImageAsync(Guid itemId)` handler — load item, verify it has `ProductImage`, detect content type from first bytes (JPEG: FF D8, PNG: 89 50, GIF: 47 49, else `image/jpeg`), return `File(item.ProductImage, contentType)`; return `NotFound()` if item has no image
- [x] T016 [US2] Modify `MariCamiStore/Pages/Orders/Items.cshtml.cs`: add `OrderHeaderDto` record (`OrderId`, `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`); add `OnPostUpdateOrderAsync([FromBody] OrderHeaderDto dto)` handler — verify order exists and is Pending, call `IOrderService.UpdateOrderAsync` with the 4 editable fields, return `{ success: true }` or error

**Checkpoint**: `Items.cshtml.cs` compilando con los nuevos/actualizados handlers. Build sin errores.

---

## Phase 5: Orders Index — Creación Solo 4 Campos (US2)

**Purpose**: Aplicar los cambios menores en la pantalla de lista de órdenes para que el formulario de creación solo muestre los 4 campos requeridos.

**Prerequisito**: Phase 2 completa (modelo sin RequestedAt). Puede correr en paralelo con Phase 4.

- [x] T017 [P] [US2] Modify `MariCamiStore/Pages/Orders/Index.cshtml`: add CSS class `edit-only-field` to the `form-group` divs containing `#order-shipping-intern` and `#order-discount`
- [x] T018 [US2] [US33] Modify `MariCamiStore/Pages/Orders/Index.cshtml.cs`: in `OnGetLoadAsync`, add `CurrencyId` and `ItemCount` (count of OrderItems for this order via a subquery or `.Include()`) to each order in the projection; remove any references to `RequestedAt`
- [x] T019 [P] [US2] Modify `MariCamiStore/wwwroot/js/pages/orders/index.js`: in `openNewOrder()` add `$('.edit-only-field').hide()` after clearing form; in `openEditOrder(item)` add `$('.edit-only-field').show()` before populating form
- [x] T039 [US33] Modify `MariCamiStore/Pages/Orders/Index.cshtml`: add `#order-currency` select field inside the order modal (visible always, not `edit-only-field`); add a `form-group` with label "Moneda" containing an empty `<select id="order-currency">` that will be populated dynamically
- [x] T040 [US33] Modify `MariCamiStore/wwwroot/js/pages/orders/index.js`: on `$(function() {...})` init, load currencies via `$.get('/Currencies?handler=Load')` and store as `var currencyItems = [...]`; populate `#order-currency` with `<option value="{id}">{abbreviation}</option>` for each; in `openNewOrder()`: load config via `?handler=Configuration` (already done) and set `#order-currency` to the value returned as `currencyId` from the config response (update `OnGetConfigurationAsync` to return `currencyId` if not already); in `openEditOrder(item)`: set `#order-currency` to `item.currencyId`; disable `#order-currency` if `item.itemCount > 0 && item.status !== 'Pending'` (read-only when has items AND not Pending); ensure `currencyId: $('#order-currency').val()` is included in the POST body of both `?handler=Create` and `?handler=Update`
- [x] T041 [US33] Modify `MariCamiStore/Pages/Orders/Index.cshtml.cs`: update `OnGetConfigurationAsync` to return `currencyId: configuration.OrderCurrencyIdDefault` alongside `exchangeRate` and `taxPercentage`

**Checkpoint**: Al crear una nueva orden, solo se ven 4 campos. Al editar, se ven todos.

---

## Phase 6: Items HTML — Modal + In-Place Editing (US1, US2, US3, US4, US5, US6)

**Purpose**: Rediseñar `Orders/Items.cshtml` para soportar el modal de ítems, la edición in-place del header de la orden, el warning de dirty state, y el indicador `TotalAgreedPriceInLocal`.

**Prerequisito**: Phase 4 completa (los handlers deben existir antes de agregar el HTML que los referencia).

- [x] T020 [US1] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: disable jsGrid inline insert/edit — remove `inserting` and `editing` from the jsGrid server-side variables or set them to false; add "Agregar Ítem" button (visible only when `isPending == true`) that will trigger `openAddItem()`; keep Delete button in control column; add Edit button in control column that triggers `openEditItem(item)`; add inline JS variable `var orderCurrencyId = '@(order?.CurrencyId)';` alongside existing inline variables (`orderId`, `isPending`, `exchangeRate`, etc.)
- [x] T021 [US1] [US3] [US4] [US5] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: add `#itemModal` Bootstrap modal with all item fields — `item-id` (hidden), `item-customer` (select), `item-product-description` (text), `item-product-link` (text, optional), `item-product-source-code` (text, optional), `item-product-image` (file input, accept image/*), `item-product-image-preview` (img, initially hidden), `item-product-type` (select), `item-list-price` (number), `item-list-price-tax-with-tax` (number), `item-real-price` (number), `item-estimate-shipping` (number), `item-service-fee-in-local` (number), `item-total` (number, readonly), `item-agreed-price-in-local` (number), `item-modal-error` (div, initially hidden), "Guardar" button `#btn-save-item`, "Cancelar" button
- [x] T022 [US3] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: add `#imagePreviewModal` Bootstrap modal with an `<img id="preview-img">` for displaying saved item images; modal has only a "Cerrar" button
- [x] T023 [US2] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: convert the 4 editable order header fields (`ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`) from read-only display elements to `<input type="number">` fields when `isPending == true` (use `@if (isPending)` Razor condition); add "Guardar Orden" button `#btn-save-order` visible only when `isPending == true`
- [x] T024 [US2] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: add `<div id="order-dirty-warning" class="alert alert-warning" style="display:none">Presionar guardar para recalcular si hay algo pendiente de guardar</div>` above the order header card
- [x] T025 [US6] Modify `MariCamiStore/Pages/Orders/Items.cshtml`: add `TotalAgreedPriceInLocal` display element to the order totals card (label "Precio Acordado Total", value element with id `order-total-agreed`)

**Checkpoint**: Page renders without errors. Modal structure is in place (not yet wired to JS).

---

## Phase 7: Items JS — Lógica Reactiva Completa (US1, US2, US3, US4, US5, US6)

**Purpose**: Actualizar `items.js` para soportar el modal, los cálculos reactivos, la edición in-place, el dirty state, el filtro de ProductTypes, y el upload de imágenes. Todo en una sola pasada sobre el mismo archivo.

**Prerequisito**: Phase 6 completa (los IDs del HTML deben existir antes de referenciarlos en JS).

- [x] T026 [US1] [US6] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: rename all occurrences of `listPriceTax` → `listPriceTaxWithTax` in jsGrid column definitions and data mappings; update `calcItem()` formula — `item.listPriceTaxWithTax = round2(item.listPrice + item.listPrice * taxPercentage / 100)`, `item._total = round2(item.listPriceTaxWithTax * exchangeRate + item.serviceFeeInLocal)`, set `item.agreedPriceInLocal = item._total` only when `!item._agreedManuallyEdited`
- [x] T027 [US6] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: update `recalcOrderHeader(allItems)` — add `totals.totalAgreedPriceInLocal = allItems.reduce(...)` calculation; update DOM element `#order-total-agreed` with the value; add `totalAgreedPriceInLocal` to the object passed to `persistTotals()`
- [x] T028 [US6] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: update `persistTotals(totals)` — include `totalAgreedPriceInLocal` in the POST body to `?handler=UpdateTotals`
- [x] T029 [US1] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: add file-scope variables — `var userEditedAgreed = false; var orderDirty = false; var orderCurrencyId = '@(order?.CurrencyId)';`; add functions `openAddItem()` (clear modal, reset `userEditedAgreed = false`, load ProductTypes by currency, load Customers, show #itemModal) and `openEditItem(item)` (populate modal with item data, set `userEditedAgreed = (item.agreedPriceInLocal !== item._total)`, show #itemModal)
- [x] T030 [US4] [US5] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: in modal, add reactive event handlers — `#item-list-price` input: recalculate `listPriceTaxWithTax` and `_total`; if `!userEditedAgreed`, update `#item-agreed-price-in-local`; if `#item-real-price` is empty, set it equal to `listPrice`; `#item-list-price-tax-with-tax` input: recalculate `_total` and `agreedPriceInLocal` (if `!userEditedAgreed`); `#item-service-fee-in-local` input: recalculate `_total` and `agreedPriceInLocal` (if `!userEditedAgreed`); `#item-agreed-price-in-local` input: set `userEditedAgreed = true`
- [x] T031 [US5] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: in `openAddItem()` after loading ProductTypes via `?handler=ProductTypesByCurrency`, populate `#item-product-type` select options; if the returned list is empty, disable `#item-product-type` and append a `<option disabled>No hay tipos disponibles para esta moneda</option>` option; add `#item-product-type` change handler — call `$.get('?handler=ProductType&id=' + val)`, on success set `#item-service-fee-in-local` and `#item-estimate-shipping` from response; trigger recalculation of `_total` and `agreedPriceInLocal`
- [x] T032 [US3] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: add `#item-product-image` change handler — read file, validate size `<= 2097152` bytes; if valid, show `#item-product-image-preview` with FileReader DataURL; store base64 string in `var selectedImageBase64`; if invalid, clear input, hide preview, show error "La imagen supera el límite de 2 MB."
- [x] T033 [US1] [US3] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: add `#btn-save-item` click handler — build payload including all item fields + `productImageBase64: selectedImageBase64 || null`; POST to `?handler=Insert` if `#item-id` is empty, else `?handler=Update`; on success close modal, refresh jsGrid, call `refreshTotals()`; on error show `#item-modal-error` with `response.error` if present, otherwise "Error al guardar el ítem. Por favor intente de nuevo." (never expose raw exception details)
- [x] T034 [US2] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: add `change` event listener on order header input fields (`#order-exchange-rate`, `#order-tax`, `#order-shipping-intern`, `#order-discount`) — on any change set `orderDirty = true` and show `#order-dirty-warning`; add `#btn-save-order` click handler — POST to `?handler=UpdateOrder` with the 4 field values; on success set `orderDirty = false`, hide `#order-dirty-warning`, update local `exchangeRate` and `taxPercentage` variables, trigger Trigger A (recalculate all items and order totals via `refreshTotals()`)
- [x] T035 [US3] Modify `MariCamiStore/wwwroot/js/pages/orders/items.js`: update jsGrid column definitions — add `productSourceCode` text column; add `Total` computed column using `itemTemplate: function(val, item) { return round2(item.listPriceTaxWithTax * exchangeRate + item.serviceFeeInLocal).toFixed(2); }`; add image button column — `itemTemplate: function(val, item) { return item.hasImage ? '<button class="btn btn-xs btn-info btn-preview-image" data-id="' + item.id + '">Ver</button>' : ''; }` — wire button click to show `#imagePreviewModal` with `<img src="?handler=ItemImage&itemId=' + id + '">`

**Checkpoint**: La app funciona end-to-end — modal de ítems operativo, cálculos reactivos, totales de orden actualizados, dirty state del header funcional.

---

## Phase 8: Polish & Cross-Cutting

**Purpose**: Verificación de compilación, revisión de errores de manejo, y validación del smoke test.

- [x] T036 [P] Run `dotnet build MariCamiStore/MariCamiStore.csproj` — verify 0 errors (warnings existentes sobre nullability son pre-existentes y aceptables)
- [x] T037 [P] Review `Items.cshtml.cs` for exception handling — `OnGetItemImageAsync` should catch exceptions and return `StatusCode(500)`; `OnPostUpdateOrderAsync` should validate non-zero `ExchangeRate`
- [x] T038 [P] Verify `orderCurrencyId` JS variable is correctly injected from Razor (`@order?.CurrencyId`) and used in `openAddItem()` ProductTypes filter call

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 2 (Foundational)**: No dependencias — comenzar inmediatamente
- **Phase 3 (Services)**: Requiere Phase 2 (modelo actualizado)
- **Phase 4 (Items.cshtml.cs)**: Requiere Phase 3 (servicios actualizados)
- **Phase 5 (Index changes)**: Requiere Phase 2 — puede correr en paralelo con Phase 3 y 4
- **Phase 6 (Items HTML)**: Requiere Phase 4 (handlers deben existir)
- **Phase 7 (Items JS)**: Requiere Phase 6 (IDs del HTML deben existir)
- **Phase 8 (Polish)**: Requiere todas las fases anteriores

### User Story Coverage

- **US7 (P0)**: Phase 2 (T001–T006)
- **US6 (P1)**: Phase 3 (T007–T008), Phase 4 (T011), Phase 6 (T025), Phase 7 (T026–T028)
- **US1 (P1)**: Phase 4 (T011–T013), Phase 6 (T020–T021), Phase 7 (T029, T033, T035)
- **US2 (P1)**: Phase 4 (T016), Phase 5 (T017–T019), Phase 6 (T023–T024), Phase 7 (T034)
- **US3 (P1)**: Phase 4 (T012–T013, T015), Phase 6 (T021–T022), Phase 7 (T032, T033)
- **US4 (P1)**: Phase 7 (T026, T030)
- **US5 (P2)**: Phase 3 (T009–T010), Phase 4 (T014), Phase 7 (T031, T035)
- **US33 = FR-033/FR-034 (P1)**: Phase 5 (T018, T039, T040, T041)

### Dentro de cada fase (orden secuencial requerido)

- T001/T002 pueden correr en paralelo entre sí (archivos distintos)
- T003/T004 pueden correr en paralelo entre sí (archivos distintos), pero requieren T001/T002
- T005 requiere T001–T004
- T006 requiere T005
- T009/T010 pueden correr en paralelo entre sí
- T017/T018/T019 pueden correr en paralelo entre sí (archivos distintos)
- T036/T037/T038 pueden correr en paralelo entre sí

### Parallel Opportunities

```text
Phase 2:
  T001 ‖ T002 (Model changes — different files)
  T003 ‖ T004 (Config changes — different files)

Phase 3:
  T009 ‖ T010 (ICatalogService + CatalogService)

Phase 5:
  T017 ‖ T018 ‖ T019 (Index.cshtml, Index.cshtml.cs, index.js — different files)

Phase 8:
  T036 ‖ T037 ‖ T038 (review tasks — no dependencies between them)
```

---

## Implementation Strategy

### MVP First (US7 + US1 — Migración + Modal básico)

1. Completar Phase 2 (T001–T006) — migración
2. Completar Phase 3 (T007–T010) — services
3. Completar Phase 4 (T011–T016) — handlers
4. Completar Phase 6 (T020–T021) — HTML del modal básico (sin imagen)
5. Completar Phase 7 parcial (T026–T029, T033) — JS modal básico con cálculos
6. **STOP y VALIDAR**: modal de ítems funcionando, cálculos básicos correctos

### Incremental Delivery (Todas las US)

1. Phase 2 → Foundation DB ready
2. Phase 3 + 4 → Services y handlers ready
3. Phase 5 → Index changes (paralelo con 3/4)
4. Phase 6 → HTML completo
5. Phase 7 → JS completo
6. Phase 8 → Polish + smoke test

---

## Notes

- [P] tasks = archivos distintos, sin dependencias compartidas — seguro paralelizar
- Modificar `Items.cshtml.cs` en una sola pasada (T011–T016 secuencialmente en el mismo archivo)
- Modificar `items.js` en una sola pasada (T026–T035 secuencialmente en el mismo archivo)
- Modificar `Items.cshtml` en una sola pasada (T020–T025 secuencialmente en el mismo archivo)
- La migración (T005–T006) DEBE aplicarse antes de que la app pueda correr
- Verificar que `select2` esté disponible en `_Layout.cshtml` durante T021 — si no, implementar filtro JS simple sobre el `<select>` de `#item-customer`
- La variable JS `orderCurrencyId` debe exponerse desde Razor en `Items.cshtml` como variable inline
