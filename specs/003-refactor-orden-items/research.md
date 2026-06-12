# Research: Refactor Orden e Ítems

**Date**: 2026-06-04
**Feature**: specs/003-refactor-orden-items/spec.md

---

## Decision 1 — Estado actual del modelo OrderItem

**Finding**: `OrderItem.cs` ya contiene `ProductLink` (max 500), `ProductSourceCode` (max 50), y `ProductImage` (byte[]?) con entity type configuration completa. Estos campos probablemente se agregaron en `20260603023734_new_field-other.cs`.

**Decision**: Asumir que estos campos ya existen en la DB. La nueva migración solo maneja: rename de `ListPriceTax` → `ListPriceTaxWithTax`, eliminación de `RequestedAt`, adición de `TotalAgreedPriceInLocal`.

**Rationale**: Evitar errores de columna duplicada. Si los campos aún no están en DB, EF Core lo detectará con un error claro en `dotnet ef database update`.

**Alternatives considered**: Agregar todos los campos con guards IF NOT EXISTS — rechazado porque EF Core no soporta DDL condicional nativo.

---

## Decision 2 — Estrategia de upload de imagen (Base64 en JSON)

**Finding**: Los handlers `OnPostInsertAsync` y `OnPostUpdateAsync` usan `[FromBody]` JSON binding. Las imágenes no se pueden enviar como JSON binario directamente.

**Decision**: Codificar la imagen como Base64 en el cliente e incluirla como campo string en el payload JSON. El backend la decodifica a `byte[]`.

**Rationale**:
- Mantiene consistencia con el patrón de handlers existente (`[FromBody]` JSON)
- Sin cambio de infraestructura (sin multipart form parser)
- 2 MB imagen → ~2.67 MB string Base64 → aceptable bajo el límite default de 30 MB de ASP.NET Core
- El campo `ProductImage` en el modelo ya es `byte[]?` — solo cambiar serialización

**Alternatives considered**:
- Endpoint separado `OnPostUploadImageAsync` con multipart: rechazado (UX de dos pasos, estado complejo)
- Multipart para todas las operaciones de ítem: rechazado (rompe patrón existente)

---

## Decision 3 — Modal dedicado para ítems (Bootstrap)

**Finding**: El proyecto usa Bootstrap 4 / AdminLTE 3 y jQuery. El flujo de creación de órdenes ya usa un modal Bootstrap (`#orderModal` en `Orders/Index.cshtml`). El jsGrid actualmente usa edición inline.

**Decision**: Agregar `#itemModal` Bootstrap modal a `Orders/Items.cshtml`. Las opciones `inserting: false` y `editing: false` del jsGrid se desactivan. Botones personalizados "Agregar Ítem" y "Editar" abren el modal.

**Rationale**: Consistente con el patrón modal existente del proyecto. Sin dependencias nuevas. jsGrid permanece para display y eliminación.

**Alternatives considered**:
- Página separada para editar ítems: rechazado (navegación extra innecesaria)
- Overlay custom sin Bootstrap: rechazado (inconsistente)

---

## Decision 4 — Tracking de override manual de AgreedPriceInLocal

**Finding**: La spec requiere que `AgreedPriceInLocal` editado manualmente no sea sobreescrito por recálculos (excepto al cambiar ExchangeRate). El proyecto ya usa el patrón `userEditedName = false` en `orders/index.js`.

**Decision**: En el modal de ítem, usar un flag JS `userEditedAgreed = false` que se activa en `$('#item-agreed-price').on('input', ...)`. Al abrir el modal para un ítem nuevo, resetear a `false`. Al disparar Trigger A (ExchangeRate changed), ignorar el flag y recalcular para todos los ítems.

**Rationale**: Flag client-side es la solución más simple. Sin cambios en DB. Consistente con `userEditedName` ya en uso.

---

## Decision 5 — Filtro de ProductType por moneda de la orden

**Finding**: `OnGetProductTypeAsync(Guid id)` ya retorna `CurrencyId`. `Order.CurrencyId` ya está en el modelo.

**Decision**: Agregar nuevo handler `OnGetProductTypesByCurrencyAsync(Guid currencyId)` en `Items.cshtml.cs` que retorna ProductTypes filtrados por moneda. Llamar al abrir el modal, pasando `Order.CurrencyId` (disponible como variable JS en la página).

**Rationale**: Filtro server-side es más limpio que cargar todos y filtrar en JS. Aprovecha el `ICatalogService` existente.

**Alternatives considered**: Cargar todos los ProductTypes y filtrar en JS — más simple pero carga datos innecesarios.

---

## Decision 6 — Edición in-place del header de la orden

**Finding**: `Orders/Items.cshtml` tiene una tarjeta de header de orden de solo lectura con 5 campos. El botón "editar" actual navega o abre un modal en `Orders/Index.cshtml`.

**Decision**: Reemplazar los elementos de display con `<input>` fields para los 4 campos editables (`ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`). Agregar botón "Guardar Orden" que llama `OnPostUpdateAsync` del `IOrderService`. Detectar dirty state con JS.

**Rationale**: Inputs HTML directos no requieren nuevas abstracciones. Compatible con el handler `UpdateOrderAsync` existente.

---

## Decision 7 — Moneda de la orden (CurrencyId)

**Finding**: `Order.CurrencyId` ya existe en modelo y DB. `Configuration.OrderCurrencyIdDefault` provee el valor por defecto. `OnPostCreateAsync` ya asigna `CurrencyId` desde configuración al crear órdenes.

**Decision**: FR-032 (default desde org config) ya está implementado. Para FR-033/FR-034: hacer `CurrencyId` editable en el modal de creación y en el header de la orden solo cuando no hay ítems O el estado es Pending.

**Rationale**: Sin cambios en DB adicionales. La lógica de editabilidad es puramente condicional en el HTML y JS.

---

## Decision 8 — TotalAgreedPriceInLocal en OrderTotalsDto

**Finding**: `OrderTotalsDto` record es usado por `OnPostUpdateTotalsAsync` y `persistTotals()` JS. Necesita `TotalAgreedPriceInLocal` agregado.

**Decision**: Agregar `decimal TotalAgreedPriceInLocal` a `OrderTotalsDto`. Actualizar `persistTotals()` para incluirlo. Actualizar `recalcOrderHeader()` para calcularlo (= Σ `AgreedPriceInLocal` de todos los ítems).

**Rationale**: Extensión directa del DTO existente. Un solo cambio se propaga a través de la capa de servicio.

---

## Decision 9 — Dropdown de CustomerId con búsqueda

**Finding**: El jsGrid actual de ítems carga todos los Customers en un `<select>` simple. La spec requiere búsqueda tipo-as-you-type. Select2 no está confirmado como disponible en el layout.

**Decision**: Verificar si select2 está disponible en `_Layout.cshtml`. Si está disponible, aplicar `$(select).select2()` al selector del modal. Si no, implementar filtro simple con input de texto que filtre las opciones del select (compatible con jQuery).

**Rationale**: Evitar agregar dependencias npm nuevas. El select con filtro JS es suficiente para el volumen esperado de clientes.

---

## Decision 10 — Columna "Total" computada en el grid de ítems

**Finding**: `Total = (ListPriceTaxWithTax * ExchangeRate) + ServiceFeeInLocal` es un campo computado (no persiste). Necesita aparecer en la grilla de ítems.

**Decision**: Agregar `Total` como columna jsGrid con `itemTemplate` function que calcula el valor desde los datos del ítem. No editable, solo display. No enviar al backend.

**Rationale**: Cálculo puro en JS, sin cambio en backend. `itemTemplate` callback de jsGrid recibe el item data completo.

---

## Decision 11 — Imagen en grilla: indicador vs dato binario

**Finding**: Cargar `byte[]` completo en la respuesta del `OnGetLoadAsync` sería muy costoso en red (N ítems × hasta 2 MB cada uno).

**Decision**: 
- `OnGetLoadAsync` retorna `hasImage: true/false` en lugar del binario
- Agregar handler `OnGetItemImageAsync(Guid itemId)` que retorna la imagen como `FileContentResult`
- La grilla muestra botón "Ver Imagen" solo cuando `hasImage == true`
- El botón llama `?handler=ItemImage&itemId={id}` y muestra en popup

**Rationale**: La grilla carga rápido. La imagen solo se carga a demanda. Sin cambios en el modelo de datos.
