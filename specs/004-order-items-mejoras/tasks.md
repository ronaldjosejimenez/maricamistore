# Tasks: Order Items — Mejoras de UI y Modelo

**Input**: Design documents from `specs/004-order-items-mejoras/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/items-handlers.md ✓

**Tests**: No aplica — sin pruebas automatizadas según política del proyecto.

**Organization**: Tareas agrupadas por User Story. Fases A→E mapean al plan.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Paralelizable (archivo diferente, sin dependencia de tarea incompleta)
- **[Story]**: User Story del spec.md (US1–US7)
- Paths explícitos en cada tarea

---

## Phase 1: Setup

_(No aplica — proyecto existente con infraestructura completa)_

---

## Phase 2: Foundational — Migración IsReceived (Phase A del plan) ⚠️

**Propósito**: Agregar `IsReceived` a `OrderItems`. Prerequisito bloqueante para US5 (checklist de recepción).

**⚠️ CRÍTICO**: US5 no puede implementarse hasta que esta fase esté completa.

- [X] T001 [US1] Agregar propiedad `public bool IsReceived { get; set; }` al modelo en `MariCamiStore/Model/OrderItem.cs`
- [X] T002 [US1] Agregar configuración EF `builder.Property(oi => oi.IsReceived).IsRequired().HasDefaultValue(false);` en `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderItemEntityTypeConfiguration.cs`
- [X] T003 [US1] Generar migración EF: `dotnet ef migrations add AddIsReceivedToOrderItem --project MariCamiStore` desde el directorio raíz del repo
- [X] T004 [US1] Aplicar migración: `dotnet ef database update --project MariCamiStore`

**Checkpoint**: Migración aplicada. Todos los registros existentes tienen `IsReceived = false`. Compilación sin errores.

---

## Phase 3: US2 + US3 — Backend Grid Agrupado + Ordenamiento (Phase B del plan)

**Goal**: El endpoint `?handler=Load` retorna `CustomerDisplayName` e `IsReceived`, pre-ordenado por nombre de cliente ASC → CreatedAt DESC.

**Independent Test**: Llamar `GET ?handler=Load&orderId={id}` y verificar que la respuesta JSON incluye `customerDisplayName` en cada ítem y que están ordenados correctamente.

- [X] T005 [P] [US2] Agregar record `OrderItemWithCustomerDto` al archivo `MariCamiStore/Pages/Orders/Items.cshtml.cs` (campos: Id, OrderId, CustomerId, CustomerDisplayName, ProductDescription, ProductLink, ProductSourceCode, HasImage, ProductTypeId, ListPrice, ListPriceTaxWithTax, RealPrice, EstimateShipping, ServiceFeeInLocal, AgreedPriceInLocal, IsReceived, CreatedAt, UpdatedAt)
- [X] T006 [P] [US2] Agregar firma `Task<List<OrderItemWithCustomerDto>> GetOrderItemsWithCustomerAsync(Guid orderId);` a la interfaz `MariCamiStore/Services/IOrderService.cs`
- [X] T007 [US2] Implementar `GetOrderItemsWithCustomerAsync` en `MariCamiStore/Services/OrderService.cs`: JOIN con tabla `Customers` via EF (Include o proyección), campo `CustomerDisplayName = customer.NickName != "" ? customer.NickName : (customer.Name ?? customer.Id.ToString())`, ordenado `.OrderBy(x => x.CustomerDisplayName).ThenByDescending(x => x.CreatedAt)`, retornar lista de `OrderItemWithCustomerDto`
- [X] T008 [US2] Actualizar handler `OnGetLoadAsync` en `MariCamiStore/Pages/Orders/Items.cshtml.cs` para usar `GetOrderItemsWithCustomerAsync` e incluir `CustomerDisplayName` e `IsReceived` en la respuesta JSON proyectada

**Checkpoint**: `GET ?handler=Load` retorna `customerDisplayName` e `isReceived`. Ítems ordenados por nombre de cliente A→Z, luego fecha más reciente primero.

---

## Phase 4: US5 — Backend ToggleReceived (Phase B del plan, continuación)

**Goal**: Nuevo endpoint `POST ?handler=ToggleReceived` persiste `IsReceived` correctamente validando el estado de la orden.

**Independent Test**: Con orden en estado `Delivering`, POST `{ itemId, isReceived: true }` → respuesta `{ success: true }` y `IsReceived` actualizado en DB.

- [X] T009 [P] [US5] Agregar firma `Task<(bool Success, string? Error)> ToggleIsReceivedAsync(Guid itemId, bool isReceived);` a `MariCamiStore/Services/IOrderService.cs`
- [X] T010 [US5] Implementar `ToggleIsReceivedAsync` en `MariCamiStore/Services/OrderService.cs`: cargar ítem + su orden, validar que orden esté en `Delivering` o `Delivered`, actualizar `item.IsReceived` y `item.UpdatedAt`, `SaveChangesAsync()`
- [X] T011 [US5] Agregar record `ToggleReceivedRequest(Guid ItemId, bool IsReceived)` y handler `OnPostToggleReceivedAsync([FromBody] ToggleReceivedRequest request)` en `MariCamiStore/Pages/Orders/Items.cshtml.cs`

**Checkpoint**: POST `?handler=ToggleReceived` con `{ itemId, isReceived }` actualiza DB. Retorna `{ success: false, error: "..." }` si la orden no está en Delivering/Delivered.

---

## Phase 5: US2 + US3 + US6 — Frontend Tabla Custom + Agrupación + Contador (Phase C del plan)

**Goal**: Grilla reemplazada con tabla HTML agrupada por cliente, subtotales, total general, contador actualizado.

**Independent Test**: Abrir pantalla Items de una orden con ítems de 2+ clientes → tabla agrupada visible con grupos, subtotales por cliente y total general al pie. Badge muestra conteo correcto.

- [X] T012 [P] [US2] Agregar `var orderStatus = '@order.Status';` en el bloque `<script>` inicial de `MariCamiStore/Pages/Orders/Items.cshtml` (junto a `isPending`, `exchangeRate`, etc.)
- [X] T013 [P] [US6] En `MariCamiStore/Pages/Orders/Items.cshtml`: reemplazar `<div id="jsGrid"></div>` con `<div id="items-table-container"><div class="text-center text-muted py-3">Cargando...</div></div>`; agregar `<span id="item-count-badge" class="badge badge-primary ml-2">0</span>` al lado del título "Artículos" en el card header
- [X] T014 [US2] Agregar función `renderItemsTable(items)` en `MariCamiStore/wwwroot/js/pages/orders/items.js` según el plan.md Phase C: determinar `isDelivering`, agrupar ítems por `customerDisplayName`, generar `<table>` con filas de encabezado de grupo (`table-dark`), filas de ítem, filas de subtotal (`table-light font-weight-bold`) y fila de total general (`table-info`); actualizar `$('#item-count-badge').text(items.length)`; incluir columna ProductLink como `<a href target="_blank">Ver link</a>` con `min-width:250px`
- [X] T015 [US2] Agregar función `loadItems()` en `MariCamiStore/wwwroot/js/pages/orders/items.js`: `$.get('?handler=Load&orderId=' + orderId, function(data) { renderItemsTable(data || []); });`
- [X] T016 [US2] En `MariCamiStore/wwwroot/js/pages/orders/items.js`: reemplazar el bloque `$('#jsGrid').jsGrid({...})` y su llamada `autoload` con una llamada `loadItems()` al final de `$(function() { ... })`; reemplazar todas las llamadas `$('#jsGrid').jsGrid('loadData')` con `loadItems()` (en los handlers de Insert, Delete y SaveOrder)

**Checkpoint**: Tabla agrupada visible con subtotales. Badge muestra conteo. Agregar y eliminar ítems actualiza tabla y contador. `refreshTotals()` sigue funcionando.

---

## Phase 6: US5 — Frontend Checklist de Recepción (Phase C continuación)

**Goal**: Checkbox por ítem visible solo en Delivering/Delivered. Fila verde al marcar. Persistido.

**Independent Test**: Transicionar orden a Delivering → checkbox aparece en cada fila. Marcar checkbox → fila verde inmediata + persiste al recargar.

- [X] T017 [US5] En la función `renderItemsTable` en `MariCamiStore/wwwroot/js/pages/orders/items.js`: agregar columna condicional de checkbox cuando `isDelivering` es true (`<input type="checkbox" class="is-received-chk" data-item-id="{id}">`, `checked` si `item.isReceived`); aplicar clase `table-success` a la fila `<tr>` cuando `item.isReceived === true`
- [X] T018 [US5] En `MariCamiStore/wwwroot/js/pages/orders/items.js`: agregar handler delegado `$(document).on('change', '.is-received-chk', function() { ... })` que llama `ajaxPost('ToggleReceived', { itemId, isReceived }, ...)`, aplica/quita clase `table-success` en la fila en caso de éxito, y revierte el checkbox en caso de error

**Checkpoint**: Checkbox funcional en órdenes Delivering/Delivered. Invisible en otros estados. Fila verde persiste al recargar.

---

## Phase 7: US4 — Reactive Pricing: RealPrice ← ListPrice (Phase D del plan)

**Goal**: RealPrice sigue automáticamente a ListPrice en nuevo ítem; comportamiento correcto en edición (FR-007, FR-007b, FR-008).

**Independent Test**: Modal nuevo ítem → escribir 25.50 en ListPrice → RealPrice muestra 25.50. Cambiar a 30 → RealPrice actualiza a 30. Editar RealPrice manualmente a 20 → cambiar ListPrice → RealPrice mantiene 20.

- [X] T019 [P] [US4] Agregar `var userEditedRealPrice = false;` en el scope de archivo en `MariCamiStore/wwwroot/js/pages/orders/items.js` (junto a `userEditedAgreed`)
- [X] T020 [US4] En función `openAddItem()` en `MariCamiStore/wwwroot/js/pages/orders/items.js`: agregar `userEditedRealPrice = false;` al inicio
- [X] T021 [US4] En función `openEditItem(item)` en `MariCamiStore/wwwroot/js/pages/orders/items.js`: agregar `userEditedRealPrice = Math.abs((parseFloat(item.realPrice)||0) - (parseFloat(item.listPrice)||0)) > 0.01;`
- [X] T022 [US4] En handler `$('#item-list-price').on('input', ...)` en `MariCamiStore/wwwroot/js/pages/orders/items.js`: reemplazar la condición `if (!$('#item-real-price').val() || parseFloat(...) === 0)` por `if (!userEditedRealPrice) { $('#item-real-price').val(lp.toFixed(2)); }`
- [X] T023 [US4] Agregar nuevo listener en `MariCamiStore/wwwroot/js/pages/orders/items.js`: `$('#item-real-price').on('input', function () { userEditedRealPrice = true; });`

**Checkpoint**: Test de 5 escenarios según US-4 del spec (nuevos ítems, edición con RealPrice==ListPrice, edición con RealPrice≠ListPrice).

---

## Phase 8: US7 — URL Más Ancho en Modal (Phase E del plan)

**Goal**: Campo ProductLink ocupa fila completa en el modal (col-md-12).

**Independent Test**: Abrir modal de ítem → campo Link del Producto ocupa el ancho completo de la columna del modal.

- [X] T024 [US7] En `MariCamiStore/Pages/Orders/Items.cshtml` (modal body): mover el campo `item-product-link` a su propia fila `<div class="row"><div class="col-md-12">...</div></div>`, y mover el campo `item-product-source-code` a una nueva fila independiente `<div class="row"><div class="col-md-6">...</div></div>`

**Checkpoint**: Modal muestra campo Link en fila propia, ancho completo. Código Fuente en fila separada.

---

## Phase 9: Polish & Verificación

- [X] T025 [P] Verificar que compilación del proyecto no tiene errores: `dotnet build MariCamiStore`
- [X] T026 Smoke test manual: cargar pantalla Items de orden con ítems de 2+ clientes → tabla agrupada, subtotales correctos, contador correcto
- [X] T027 Smoke test manual: agregar ítem nuevo → RealPrice sigue a ListPrice; guardar; tabla se actualiza; contador incrementa
- [X] T028 Smoke test manual: transicionar orden a Delivering → checkbox aparece; marcar ítem → fila verde; recargar → fila sigue verde
- [X] T029 [P] Agregar validación backend de NickName en `MariCamiStore/Pages/Customers/Index.cshtml.cs`: en `OnPostInsertAsync` y `OnPostUpdateAsync`, si `string.IsNullOrWhiteSpace(item.NickName)` → retornar `new JsonResult(new { error = "El apodo es requerido." })` antes de llamar al servicio (nota: el frontend ya tiene `validate: 'required'` — esta tarea agrega la defensa en backend)

---

## Dependencies & Execution Order

### Dependencias entre fases

```
Phase 2 (Migration/US1) — Prerequisito bloqueante
    ↓
Phase 3 (Backend Load+Sort)        Phase 4 (Backend ToggleReceived)
    ↓                                   ↓
Phase 5 (Frontend Custom Table)    Phase 6 (Frontend IsReceived)
    ↓                                   ↓
    Phase 7 (Reactive Pricing)    [depende de T017-T018]
    ↓
    Phase 8 (URL Width)
    ↓
    Phase 9 (Polish)
```

Fases 3 y 4 son independientes entre sí (diferentes servicios/handlers).
Fases 5 y 6 comparten el archivo `items.js` → hacerlas secuencialmente.
Fases 7 y 8 son independientes de 5/6 (diferentes secciones de `items.js` y `Items.cshtml`).

### Dependencias internas por fase

**Phase 3**: T007 depende de T005+T006. T008 depende de T007.
**Phase 4**: T010 depende de T009. T011 depende de T010.
**Phase 5**: T014+T015 dependen de T012+T013. T016 depende de T014+T015.
**Phase 6**: T017 depende de T014 (modifica renderItemsTable). T018 depende de T017.
**Phase 7**: T020-T023 dependen de T019.

### Oportunidades de paralelización

```bash
# Phase 2: secuencial (T001→T002→T003→T004)

# Phase 3 + Phase 4 pueden ejecutarse en paralelo entre sí:
Task: "Phase 3 - Backend GetOrderItemsWithCustomer"
Task: "Phase 4 - Backend ToggleReceived"

# Dentro de Phase 3, T005 y T006 son paralelos:
Task: "T005 - Add OrderItemWithCustomerDto"
Task: "T006 - Add IOrderService signature"
# Luego T007, luego T008

# Dentro de Phase 5:
Task: "T012 - Add orderStatus var"    # paralelo
Task: "T013 - Update HTML structure"  # paralelo
# Luego T014 → T015 → T016

# Phase 7 puede hacerse en paralelo con Phase 5/6 (diferente sección del JS)
# Phase 8 puede hacerse en paralelo con Phase 5/6/7 (solo HTML del modal)
```

---

## Implementation Strategy

### MVP (Phase 2 + Phase 3 + Phase 5 solamente)

1. Completar Phase 2 (migración)
2. Completar Phase 3 (backend con customerDisplayName)
3. Completar Phase 5 (tabla custom agrupada)
4. **VALIDAR**: Grid agrupado visible con subtotales y contador
5. Continuar con Phases 4+6 (checklist), 7 (reactive pricing), 8 (URL width)

### Entrega incremental sugerida

1. Phases 2+3+5 → Grid agrupado por cliente ✓
2. Phase 7 → Reactive pricing ✓
3. Phases 4+6 → Checklist de recepción ✓
4. Phase 8 → URL width ✓
5. Phase 9 → Smoke tests ✓

---

## Notas

- **[P]**: diferentes archivos o secciones independientes — pueden ejecutarse en paralelo
- El campo `OrderItemWithCustomerDto` (T005) se define en `Items.cshtml.cs` para mantener coherencia con los demás DTOs de la página
- `renderItemsTable` reemplaza toda la funcionalidad de render de jsGrid; `refreshTotals()` se mantiene sin cambios
- Si `dotnet ef migrations` no está disponible, usar: `dotnet tool install --global dotnet-ef`
- Total: **28 tareas** (T001–T028)
