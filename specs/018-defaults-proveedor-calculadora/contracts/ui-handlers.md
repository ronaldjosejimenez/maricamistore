# UI Handler Contracts — 018

Todos los handlers son Razor Pages con respuesta JSON y camelCase en el cliente. Los POST requieren `RequestVerificationToken`.

## Suppliers/Index

### GET `?handler=Load` — MODIFY (solo la forma de la respuesta)
Devuelve `Supplier[]` con los campos nuevos:

```json
[{ "id": "guid", "name": "Shein", "taxPercentageDefault": 7.00, "currencyIdDefault": "guid|null",
   "exchangeRateDefault": 530.00, "shippingCostDefault": 2.00, "isActiveForCalculator": true }]
```

Lo consumen el grid de proveedores y `orders/index.js` (defaults de la orden nueva).

### GET `?handler=Configuration` — NEW
```json
{ "taxPercentage": 7.00, "exchangeRate": 530.00, "currencyId": "guid" }
```
Si no hay Configuración, devuelve `null` y la fila de inserción queda vacía.

### POST `?handler=Insert` / `?handler=Update` — MODIFY
Body: `Supplier` con los campos nuevos.
- OK: devuelve el `Supplier` guardado (sin cambio).
- Validación fallida (FR-002a): HTTP 400 con el mensaje en texto plano. El `ajaxPost` existente ya hace `alert('Error: ' + xhr.responseText)`. El `Deferred` del grid se rechaza para que la fila no quede como guardada.

## Calculator/Index

### GET `?handler=Suppliers` — NEW
Solo devuelve los proveedores con `IsActiveForCalculator = true`, ordenados por nombre:

```json
[{ "id": "guid", "name": "Shein", "taxPercentageDefault": 7.00,
   "exchangeRateDefault": 530.00, "shippingCostDefault": 2.00 }]
```

La página ya no lee parámetros del query string.

## Orders/Items

### Item DTO (`OrderItemDto`, `OrderItemWithCustomerDto`) — MODIFY
Se agrega `realPriceInLocal` (decimal) en las respuestas de Load, Insert y Update. En el request se ignora: el servidor lo recalcula como `Round(RealPrice × order.ExchangeRate, 2)`.

### POST `?handler=UpdateTotals` — sin cambio de contrato
El cliente envía `totalWithoutTaxes = Σ listPrice` (antes `Σ realPrice`).

### POST `?handler=UpdateOrder` — sin cambio de contrato
El comportamiento de Trigger A cambia en el servidor (FR-018a / research R8).

## Orders/Index

Sin handlers nuevos. `#order-supplier change` (solo en modo creación) asigna:
`#order-exchange-rate ← exchangeRateDefault`, `#order-tax ← taxPercentageDefault` y `#order-currency ← currencyIdDefault` (si no es null).
