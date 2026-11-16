# UI Handlers — 021

## POST `Orders/Items?handler=ToggleReceived` (contrato sin cambios)
Request:
```json
{ "itemId": "guid", "isReceived": true }
```
- `isReceived` es el estado deseado: `true` → `Status = Received`, `false` → `Status = Active`.

Response:
```json
{ "success": true, "error": null }
```

Errores (los mismos de hoy):
- "Artículo no encontrado."
- "No se puede modificar un ítem anulado."
- "Orden no encontrada."
- "Solo se puede marcar recepción en órdenes en estado Entregando."

## GET `Orders/Items?handler=Load` (cambia el DTO de ítem)
- Se **elimina** `isReceived` de cada ítem.
- `status` puede valer `"Active"`, `"Received"` o `"Voided"`.

## GET `Payments?handler=Saldos[&organizationId=guid]` (campos nuevos por fila)
```json
[
  {
    "customerId": "guid",
    "customerName": "Ana",
    "balance": 50000,
    "isGeneric": false,
    "itemsInTransit": 3,
    "suggestAdvance": true,
    "deliveredToCollect": false
  }
]
```
- `balance` se calcula igual que hoy.
- Los tres campos nuevos valen `0` / `false` cuando `balance <= 0`.
- `suggestAdvance` y `deliveredToCollect` nunca valen `true` al mismo tiempo.
- La UI pinta las etiquetas así:

| Condición | Etiqueta | Clase |
|-----------|----------|-------|
| `balance < 0` | "Crédito a favor" | `badge-success` |
| `itemsInTransit > 0` | "Productos en camino (n)" | `badge-primary` |
| `suggestAdvance` | "Sugerir adelanto" | `badge-warning` |
| `deliveredToCollect` | "Entregado – por cobrar" | `badge-danger` |

  "Saldo pendiente" ya no se muestra.
