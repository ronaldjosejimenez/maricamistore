# Contracts: Orders Index Page Handlers

**Page**: `MariCamiStore/Pages/Orders/Index.cshtml.cs`
**Feature**: specs/003-refactor-orden-items

---

## Handlers existentes — cambios necesarios

### `OnGetLoadAsync(string? statusFilter)` → GET `?handler=Load`

**Cambio**: La respuesta excluye `requestedAt` (campo eliminado). Sin otros cambios.

---

### `OnPostCreateAsync([FromBody] Order item)` → POST `?handler=Create`

**Cambio**: El formulario de creación ya solo muestra `SupplierId`, `NameOfOrder`, `ExchangeRate`, `TaxPercentage` — esto se implementa en el HTML/JS, no en el handler. El handler sigue recibiendo la misma estructura de Order. No se recibe `RequestedAt`.

El handler asigna `CurrencyId = Configuration.OrderCurrencyIdDefault` (ya implementado).

---

### `OnPostUpdateAsync([FromBody] Order item)` → POST `?handler=Update`

**Sin cambio en la firma.** El modelo `Order` ya no tiene `RequestedAt`.

---

## Sin cambios en otros handlers

`OnPostTransitionAsync`, `OnGetConfigurationAsync`, `OnPostDeleteAsync` — sin cambios requeridos.

---

## Cambios en el HTML/JS de Orders/Index

### Modal de creación (`openNewOrder()`)

**Cambio**: Solo mostrar los 4 campos requeridos (FR-015). Los campos `ShippingAmountIntern` y `DiscountAmount` se ocultan al crear y se muestran solo al editar.

**Implementación**: Agregar clase `edit-only-field` a los grupos de `#order-shipping-intern` y `#order-discount`. En `openNewOrder()` hacer `$('.edit-only-field').hide()`. En `openEditOrder()` hacer `$('.edit-only-field').show()`.
