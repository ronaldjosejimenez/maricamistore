# Data Model — 021

## OrderItem
| Campo | Cambio |
|-------|--------|
| `Status` (nvarchar(20), default `'Active'`) | Sin cambio de esquema; acepta el nuevo valor `'Received'` |
| `IsReceived` (bit) | **Se elimina** (migración `DropOrderItemIsReceived`) |

## OrderItemStatus
| Key | Nombre | ¿Cuenta en la orden? | Significado |
|-----|--------|----------------------|-------------|
| `Active` | Activo | Sí | Pendiente de recibir |
| `Received` | Recibido | Sí | Recibido por el cliente |
| `Voided` | Anulado | No | Anulado; no se modifica |

### Transiciones
```
Active ──(marcar Recibido, orden Delivering)──▶ Received
Received ──(desmarcar, orden Delivering)──▶ Active
Active ──(anulación parcial, orden Active)──▶ Voided
```
- Un ítem `Received` no se puede anular parcialmente, porque la anulación parcial solo aplica a órdenes Active.
- Si la orden completa se anula, el `Status` de los ítems no cambia; solo se generan los Void.
- Un ítem `Voided` no cambia de estado.

## SaldoReportRow (DTO, `IPaymentService.cs`)
```
SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric,
               int ItemsInTransit, bool SuggestAdvance, bool DeliveredToCollect)
```
- Los tres campos nuevos solo se calculan cuando `Balance > 0`. En los demás casos valen `0` / `false`.
- Se calculan sobre los ítems no anulados del cliente en órdenes `Active` o `Delivering`, dentro del mismo alcance de organización que el saldo:

| Campo | Regla |
|-------|-------|
| `ItemsInTransit` | Cantidad de ítems `Active` |
| `DeliveredToCollect` | `Balance > 0 && ItemsInTransit == 0` (también sin ninguna orden en curso) |
| `SuggestAdvance` | `ItemsInTransit > 0 && Σ AgreedPriceInLocal ≥ Balance` |

## Migraciones (en orden)
1. `MigrateIsReceivedToStatus`: solo datos. Up: `IsReceived = 1 AND Status <> 'Voided'` → `Status = 'Received'`. Down: `Status = 'Received'` → `IsReceived = 1, Status = 'Active'`.
2. `DropOrderItemIsReceived`: esquema. Up: `DropColumn IsReceived`. Down: `AddColumn IsReceived bit NOT NULL DEFAULT 0`.
