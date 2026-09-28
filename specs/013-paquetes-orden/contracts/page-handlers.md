# Page Handler Contracts: 013-paquetes-orden

All on `/Orders/Items` (ItemsModel). POSTs require the antiforgery header used by existing handlers.

## GET ?handler=Packages&orderId={guid}
```json
{ "estimatedShipping": 100.00, "totalPackages": 30.00, "pending": 70.00, "canManage": true,
  "packages": [ { "id": "…", "deliveryDate": "2026-09-28", "amount": 30.00, "description": "Caja 1", "createdAt": "…" } ] }
```
Returns JSON `null` when the order does not exist (or belongs to another organization).

## POST ?handler=AddPackage
Body: `{ "orderId": "…", "deliveryDate": "2026-09-28", "amount": 30.00, "description": "Caja 1" }`
The server rounds `amount` to 2 decimals before validating (> 0); `deliveryDate` is required and stored as a date (no time).
Response: `{ "success": true }` or `{ "success": false, "error": "No hay un período CxP abierto." | "La fecha de entrega es requerida." | "Solo se pueden agregar paquetes en órdenes Activas o Entregando." | "El monto debe ser mayor a cero." | "Orden no encontrada." | "La descripción no puede superar 500 caracteres." }`

## POST ?handler=DeletePackage
Body: `{ "packageId": "…" }`
Response: `{ "success": true }` or `{ "success": false, "error": "No hay un período CxP abierto." | "Solo se pueden eliminar paquetes en órdenes Activas o Entregando." | "Paquete no encontrado." }`

## Changed: POST /Orders?handler=Transition
`actualShippingAmountToCR` removed from the payload/DTO; transition to Delivered creates no CxP entry.

## Unchanged: /CxP handlers
`Entries` may now return types `AutoPaquete` / `ReversoPaquete` and negative amounts.
