# Data Model: Ajustes en Órdenes, Metas y Clientes

No schema changes.

## OrderStatus (enumeration, `Model/OrderStatus.cs`)

| Key | Label | Notes |
|---|---|---|
| Pending | Pendiente | |
| Active | Activa | |
| Delivering | Entregando | Transition button: "Iniciar entrega" |
| ~~Delivered~~ | ~~Entregada~~ | **Removed.** It survives only as a legacy key inside `OrderStatusHistory` rows (shown as "Entregada"). |
| Completed | Completada | Final |
| Voided | Anulada | Final |

### Transitions

```
Pending ──► Active ──► Delivering ──► Completed
              │            │
              └──► Voided ◄┘
```

| From | Allowed to |
|---|---|
| Pending | Active |
| Active | Delivering, Voided |
| Delivering | Completed, Voided |
| Completed | — |

### Rules by status
- Receipt marking (`IsReceived` toggle): Delivering only.
- Reassign item: Active, Delivering, Completed.
- Edit/Delete item: Pending only. "Ver" (read-only): every other status.

## Data migration `RemoveDeliveredOrderStatus`
For every `Orders.Status = 'Delivered'`:
1. Insert `OrderStatusHistory(FromStatus='Delivered', ToStatus='Completed', TransitionDate=UTC now, Notes='Migración automática: el estado Entregada fue eliminado (spec 017).', CreatedAt=UTC now)`.
2. Set `Orders.Status = 'Completed'` and `UpdatedAt = UTC now`.

Irreversible: `Down` does nothing.

## Commission (derived, not persisted)

Input: base % P (already rounded to 1 decimal). P = `TodayPercentage` if the goal is the current month (Costa Rica time), else `CompliancePercentage`.

| P | Rate | Status |
|---|---|---|
| P < 85 | 0 | red |
| 85 ≤ P < 86 | 2.5 | yellow |
| 86 ≤ P ≤ 110 | 5 | green |
| P > 110 | 6 | green-plus |

`CommissionAmount = max(0, round(ActualAmount × Rate / 100, 2))`

### DTO additions (`Services/ISalesGoalService.cs`)
- `GoalDto`: `decimal CommissionRate`, `decimal CommissionAmount`, `string CommissionStatus`
- `HistoryRowDto`: `decimal CommissionRate`, `decimal CommissionAmount`, `string CommissionStatus`
