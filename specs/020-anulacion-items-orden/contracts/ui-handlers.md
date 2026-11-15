# UI Handlers — 020

Todos en `Pages/Orders/Items.cshtml.cs`. Son POST con JSON y antiforgery (`RequestVerificationToken`), igual que los handlers existentes, y se llaman con `?handler=X&orderId=...`.

## POST `VoidItemsPreview`
Request:
```json
{ "orderId": "guid", "itemIds": ["guid", "..."] }
```
Response (éxito):
```json
{
  "success": true,
  "allSelected": false,
  "before": { "totalAgreedPriceInLocal": 0, "totalWithoutTaxes": 0, "taxesAmount": 0, "shippingAmountToCR": 0,
              "totalToPayToSupplier": 0, "totalOfTheOrder": 0, "estimatedProfitInLocal": 0 },
  "after":  { "...": "mismos campos" },
  "theoreticalRefund": 0,
  "items": [ { "id": "guid", "productDescription": "", "customerDisplayName": "", "agreedPriceInLocal": 0, "defaultRefund": 0 } ]
}
```
- `before` = los totales guardados de la orden; `after` = `ComputeOrderTotals` sobre los Active que no están seleccionados.
- `theoreticalRefund` = `before.totalToPayToSupplier − after.totalToPayToSupplier`.
- `defaultRefund` = `ListPriceTaxWithTax` del ítem.
- `allSelected` = la selección cubre todos los ítems Active. En ese caso `after` puede venir en cero; la UI bloquea "Continuar".

Errores: `{ "success": false, "error": "..." }`. Casos: orden no encontrada o no Activa, lista vacía, ítem que no es de la orden o no está Active.

## POST `VoidItems`
Request:
```json
{ "orderId": "guid", "justification": "texto", "items": [ { "itemId": "guid", "refundAmount": 0 } ] }
```
Response:
```json
{ "success": true, "error": null }
```
Validaciones del servidor (FR-011), en este orden, con mensajes:
- "Orden no encontrada." / "Solo se pueden anular ítems de órdenes en estado Activa."
- "Seleccione al menos un ítem."
- "Se requiere justificación para anular ítems."
- "El monto a devolver no puede ser negativo."
- "Hay ítems que no pertenecen a la orden o ya están anulados."
- "No se pueden anular todos los ítems; use la anulación de la orden."
- "No hay un período CxP abierto."

Si tiene éxito, el JS recarga la página para refrescar la tabla y los totales guardados del header (`savedTotals` se genera en el servidor).

## Cambios en handlers existentes
- `OnGetLoadAsync`: `OrderItemWithCustomerDto` agrega `Status`.
- `OnPostUpdateAsync`: devuelve `{ error: "No se puede modificar un ítem anulado." }` si el ítem está Voided.
- `Pages/CxP/Index.cshtml.cs OnGetEntriesAsync`: `amount` y `total` usan `CxPEntryType.SignedAmount`.
