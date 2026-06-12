# Implementation Plan: Refactor Orden e Ítems

**Branch**: `003-refactor-orden-items` | **Date**: 2026-06-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-refactor-orden-items/spec.md`

---

## Summary

Refactoring mayor del sistema de Orden e Ítems: reemplazar la edición inline del jsGrid por un modal dedicado, habilitar edición in-place del header de la orden, agregar campos `ProductLink`/`ProductSourceCode`/`ProductImage` al modal, renombrar `ListPriceTax` → `ListPriceTaxWithTax`, eliminar `RequestedAt`, agregar `TotalAgreedPriceInLocal`, implementar cálculos reactivos en JS, filtrar ProductTypes por moneda de la orden, y ejecutar la Order Calculation Suite tras cada cambio de ítem.

No se requieren cambios en autenticación, autorización, ni navegación.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10

**Primary Dependencies**: ASP.NET Core 10 Razor Pages, Entity Framework Core 10, SQL Server, NLog, jsGrid 1.5.3, jQuery 3.x, Bootstrap 4 / AdminLTE 3

**Storage**: SQL Server via EF Core — 1 nueva migración (rename columna, remove campo, add campo)

**Testing**: Out of scope

**Target Platform**: Windows (development) / Linux container (future)

**Project Type**: Web application (Razor Pages + AJAX / JSON handlers)

**Performance Goals**: Cálculos JS < 500ms; recálculo de totales < 2s; carga de grilla < 2s

**Constraints**: UI en español; código en inglés; todo acceso a DB en la capa de Services; sin stack traces expuestos al usuario

**Scale/Scope**: Small-business — misma escala que Fase 1

---

## Constitution Check

Proyecto no tiene constitución definida. Regla arquitectural del spec FR-001 (services layer only): se cumple. Todos los cambios a la DB pasan por `IOrderService`/`ICatalogService`.

No hay violaciones de gate.

---

## Project Structure

### Documentation (this feature)

```text
specs/003-refactor-orden-items/
├── plan.md              ← este archivo
├── research.md          ← decisiones técnicas
├── data-model.md        ← cambios al modelo y DB
├── contracts/
│   ├── items-handlers.md
│   └── order-index-handlers.md
└── tasks.md             ← generado por /speckit-tasks
```

### Source Code (archivos modificados)

```text
MariCamiStore/
│
├── Infrastructure/Persistance/
│   ├── Migrations/                          ← NUEVA migración
│   └── MariCamiStoreContext.cs              (sin cambio — query filters ya correctos)
│
├── Model/
│   ├── Order.cs                             (modify — remove RequestedAt, add TotalAgreedPriceInLocal)
│   └── OrderItem.cs                         (modify — rename ListPriceTax → ListPriceTaxWithTax)
│
├── Infrastructure/Persistance/
│   └── EntityTypeConfigurations/
│       ├── OrderEntityTypeConfiguration.cs  (modify — reflect model changes)
│       └── OrderItemEntityTypeConfiguration.cs (modify — rename column mapping)
│
├── Services/
│   ├── IOrderService.cs                     (modify — update OrderTotalsDto, add new method signatures)
│   └── OrderService.cs                      (modify — implement new methods, update DTO, rename field refs)
│
├── Pages/Orders/
│   ├── Items.cshtml.cs                      (modify — new handlers, update existing)
│   └── Items.cshtml                         (modify — modal, in-place editing, image preview)
│
├── Pages/Orders/
│   ├── Index.cshtml                         (modify — hide/show fields on create vs edit)
│   └── Index.cshtml.cs                      (modify — remove RequestedAt refs)
│
└── wwwroot/js/pages/orders/
    ├── items.js                             (modify — modal, new calcs, filters, image, dirty state)
    └── index.js                             (modify — hide extra fields on create)
```

---

## Implementation Layers

### Layer 0 — DB Migration (Prerequisito)

**Files**: `Migrations/`, `Order.cs`, `OrderItem.cs`, entity type configurations

1. **Modificar `Order.cs`**:
   - Remover propiedad `RequestedAt`
   - Agregar propiedad `TotalAgreedPriceInLocal` (decimal)

2. **Modificar `OrderItem.cs`**:
   - Renombrar `ListPriceTax` → `ListPriceTaxWithTax`

3. **Modificar `OrderEntityTypeConfiguration.cs`**:
   - Remover configuración de `RequestedAt`
   - Agregar: `builder.Property(o => o.TotalAgreedPriceInLocal).HasColumnType("decimal(18,2)")`

4. **Modificar `OrderItemEntityTypeConfiguration.cs`**:
   - Cambiar referencia de `ListPriceTax` → `ListPriceTaxWithTax`

5. **Agregar migración EF Core**:
   ```
   dotnet ef migrations add RefactorOrderItems --project MariCamiStore
   ```
   La migración debe:
   - `RenameColumn(table: "OrderItems", name: "ListPriceTax", newName: "ListPriceTaxWithTax")`
   - `DropColumn(table: "Orders", name: "RequestedAt")`
   - `AddColumn<decimal>(table: "Orders", name: "TotalAgreedPriceInLocal", defaultValue: 0m)`

6. **Aplicar migración**: `dotnet ef database update`

---

### Layer 1 — Services (Backend Logic)

**Files**: `IOrderService.cs`, `OrderService.cs`

1. **Actualizar `OrderTotalsDto`** — agregar `decimal TotalAgreedPriceInLocal`

2. **Actualizar `UpdateOrderTotalsAsync`** — persistir el nuevo campo

3. **Actualizar todas las referencias a `ListPriceTax`** → `ListPriceTaxWithTax` en el servicio

4. **Remover referencias a `RequestedAt`** (si las hay en `CreateOrderAsync` u otros métodos)

5. **Agregar a `ICatalogService`**:
   ```csharp
   Task<List<ProductType>> GetProductTypesByCurrencyAsync(Guid currencyId);
   ```

6. **Implementar en `CatalogService`**:
   ```csharp
   return await context.ProductTypes
       .Where(pt => pt.CurrencyId == currencyId)
       .OrderBy(pt => pt.Name)
       .ToListAsync();
   ```

---

### Layer 2 — Items Page Model (Handlers)

**File**: `Pages/Orders/Items.cshtml.cs`

1. **Actualizar `OnGetLoadAsync`**:
   - Proyectar `listPriceTaxWithTax` (renamed)
   - Agregar `hasImage = item.ProductImage != null` al resultado
   - NO incluir `productImage` binario en la respuesta

2. **Actualizar `OnPostInsertAsync` / `OnPostUpdateAsync`**:
   - Aceptar `OrderItemDto` con nuevos campos: `productLink`, `productSourceCode`, `productImageBase64` (string? nullable)
   - Decodificar base64 a `byte[]` si no es null
   - Validar `byte[].Length <= 2097152` (2 MB) — retornar error si supera
   - Para Update: si `productImageBase64` es `null`, no sobreescribir imagen existente; si es `""`, limpiar imagen

3. **Agregar `OnGetProductTypesByCurrencyAsync(Guid currencyId)`**:
   - Llama `ICatalogService.GetProductTypesByCurrencyAsync(currencyId)`
   - Retorna `[{ id, name }]`

4. **Agregar `OnGetItemImageAsync(Guid itemId)`**:
   - Obtener ítem, verificar que tenga imagen
   - Retornar `File(item.ProductImage, "image/jpeg")` (o detectar tipo desde header de bytes)
   - Retornar 404 si no existe o no tiene imagen

5. **Agregar `OnPostUpdateOrderAsync([FromBody] OrderHeaderDto dto)`**:
   - Validar que la orden existe y es Pending
   - Actualizar `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount` vía `IOrderService.UpdateOrderAsync`
   - Retornar `{ success: true }` o error

---

### Layer 3 — Orders Index Page (Cambios: creación 4 campos + moneda editable)

**Files**: `Pages/Orders/Index.cshtml`, `Pages/Orders/Index.cshtml.cs`, `wwwroot/js/pages/orders/index.js`

1. **`Index.cshtml`**:
   - Agregar clase `edit-only-field` a los form-groups de `#order-shipping-intern` y `#order-discount`
   - Agregar campo `#order-currency` (select) al modal de orden, siempre visible

2. **`Index.cshtml.cs`**:
   - Remover referencias a `RequestedAt`
   - En `OnGetLoadAsync`: agregar `CurrencyId` y `ItemCount` a la proyección de cada orden
   - En `OnGetConfigurationAsync`: agregar `currencyId: configuration.OrderCurrencyIdDefault` al resultado

3. **`wwwroot/js/pages/orders/index.js`**:
   - En `$(function(){})`: cargar currencies vía `/Currencies?handler=Load`, poblar `#order-currency`
   - En `openNewOrder()`: `$('.edit-only-field').hide()`; setear `#order-currency` al valor de config
   - En `openEditOrder(item)`: `$('.edit-only-field').show()`; setear `#order-currency = item.currencyId`; deshabilitar `#order-currency` si `item.itemCount > 0 && item.status !== 'Pending'`
   - En save (Create/Update): incluir `currencyId: $('#order-currency').val()` en el POST body

**FR-033/FR-034 implementation**: `CurrencyId` editable cuando: nuevaOrden (crear) O (status == Pending). Read-only cuando: itemCount > 0 Y status != Pending.

---

### Layer 4 — Items Page HTML (Modal + In-Place Editing)

**File**: `Pages/Orders/Items.cshtml`

1. **Reemplazar edición inline del jsGrid**:
   - El jsGrid pasa a modo display/delete solo (`editing: false, inserting: false`)
   - Se mantiene el control column con botones "Editar" y "Eliminar"
   - Agregar botón "Agregar Ítem" visible solo cuando `isPending == true`

2. **Agregar `#itemModal`** (Bootstrap modal, scrollable):
   ```html
   <!-- Campos del modal: -->
   <!-- item-id (hidden) -->
   <!-- item-customer (select con búsqueda) -->
   <!-- item-product-description (text) -->
   <!-- item-product-link (text, opcional) -->
   <!-- item-product-source-code (text, opcional) -->
   <!-- item-product-image (file input, .jpg/.png/.gif/.webp, max 2MB) -->
   <!-- item-product-image-preview (img, oculto hasta selección) -->
   <!-- item-product-type (select, filtrado por moneda) -->
   <!-- item-list-price (number) -->
   <!-- item-list-price-tax-with-tax (number, calculado) -->
   <!-- item-real-price (number) -->
   <!-- item-estimate-shipping (number) -->
   <!-- item-service-fee-in-local (number) -->
   <!-- item-total (number, readonly, calculado) -->
   <!-- item-agreed-price-in-local (number) -->
   <!-- item-modal-error (div, oculto) -->
   ```

3. **Agregar `#imagePreviewModal`** (Bootstrap modal) para previsualización de imagen de ítems guardados.

4. **Edición in-place del header de la orden**:
   - Los 4 campos editables (`ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`) se muestran como `<input>` cuando `isPending == true`
   - Cuando `isPending == false`, se muestran como texto (sin cambio en comportamiento actual)
   - Agregar botón "Guardar Orden" (visible solo cuando `isPending == true`)
   - Agregar `#order-dirty-warning` (`alert alert-warning`, oculto por defecto): "Presionar guardar para recalcular si hay algo pendiente de guardar"

5. **Agregar `TotalAgreedPriceInLocal`** al card de totales de la orden.

---

### Layer 5 — Items.js (Lógica JS principal)

**File**: `wwwroot/js/pages/orders/items.js`

1. **Renombrar `listPriceTax` → `listPriceTaxWithTax`** en todas las referencias JS (columna jsGrid, calcItem, mapeos).

2. **Actualizar `calcItem(item)`**:
   - `item.listPriceTaxWithTax = round2(item.listPrice + item.listPrice * taxPercentage / 100)`
   - `item._total = round2(item.listPriceTaxWithTax * exchangeRate + item.serviceFeeInLocal)`
   - `item.agreedPriceInLocal`: si `!item._agreedManuallyEdited`, setear a `item._total`

3. **Actualizar `recalcOrderHeader(allItems)`**:
   - Agregar: `totals.totalAgreedPriceInLocal = sum of agreedPriceInLocal`
   - Agregar display del nuevo campo en el DOM

4. **Actualizar `persistTotals(totals)`**:
   - Incluir `totalAgreedPriceInLocal` en el POST body

5. **Agregar variables de scope de archivo**:
   ```js
   var userEditedAgreed = false;
   var orderDirty = false;
   ```

6. **Modal de ítem — apertura**:
   - `openAddItem()`: limpiar modal, resetear `userEditedAgreed = false`, cargar ProductTypes filtrados por moneda (`?handler=ProductTypesByCurrency&currencyId={orderCurrencyId}`)
   - `openEditItem(item)`: poblar modal con datos del ítem, setear `userEditedAgreed` a `true` si tiene `agreedPriceInLocal != Total`

7. **Modal de ítem — eventos reactivos**:
   - `#item-list-price` input → recalcular `listPriceTaxWithTax`, `_total`, `agreedPriceInLocal` (si `!userEditedAgreed`)
   - `#item-list-price-tax-with-tax` input → recalcular `_total`, `agreedPriceInLocal` (si `!userEditedAgreed`)
   - `#item-service-fee-in-local` input → recalcular `_total`, `agreedPriceInLocal` (si `!userEditedAgreed`)
   - `#item-agreed-price-in-local` input → `userEditedAgreed = true`
   - `#item-list-price` input → si `#item-real-price` vacío, `#item-real-price.val(this.value)`
   - `#item-product-type` change → llamar `?handler=ProductType&id={val}`, poblar `serviceFeeInLocal` y `estimateShipping`; recalcular
   - `#item-product-image` change → validar tamaño (<= 2MB), mostrar preview; si >2MB, mostrar error y limpiar input

8. **Modal de ítem — guardado**:
   - Construir payload incluyendo `productImageBase64` (si se seleccionó imagen nueva)
   - POST a `?handler=Insert` o `?handler=Update` según `item-id` vacío o no
   - On success: cerrar modal, refresh jsGrid, ejecutar Order Calculation Suite

9. **Dirty state del header de orden**:
   - Agregar listener `change` a los 4 inputs editables del header
   - En change: `orderDirty = true`, mostrar `#order-dirty-warning`
   - En save success: `orderDirty = false`, ocultar `#order-dirty-warning`, ejecutar Trigger A (recalcular todos los ítems si cambiaron TaxPercentage o ExchangeRate)

10. **Previsualización de imagen guardada**:
    - Botón "Ver Imagen" en jsGrid (solo si `hasImage == true`)
    - Click → `$.get('?handler=ItemImage&itemId={id}', ...)` — mostrar en `#imagePreviewModal`

11. **Actualizar jsGrid**: agregar columnas `productSourceCode` y `Total` (computed), ajustar `listPriceTax` → `listPriceTaxWithTax`.

---

## Complexity Tracking

No hay violaciones de constitution. No hay abstracciones injustificadas.

La única nueva abstracción introducida es `OrderHeaderDto` — justificada porque `Order` completo no debe enviarse para una edición parcial de 4 campos.

---

## Notas de Implementación

- Modificar `Orders/Items.cshtml.cs` en una sola pasada (no en paralelo) para evitar conflictos entre los cambios de Layer 2
- Modificar `items.js` en una sola pasada — muchos cambios interdependientes
- La migración debe ejecutarse antes de cualquier cambio en el código de aplicación (Layer 0 primero)
- El rename en EF Core debe ser `migrationBuilder.RenameColumn(...)` para preservar datos
- Verificar que el `select2` esté disponible en `_Layout.cshtml` antes de implementar el combobox de búsqueda; si no está, implementar filtro JS simple
