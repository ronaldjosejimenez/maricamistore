# Quickstart / Manual Verification: 017

## Build & migrate
```bash
cd MariCamiStore
dotnet build
dotnet ef database update     # applies RemoveDeliveredOrderStatus
```
**Deploy order:** apply the migration to production (`dotnet ef migrations script --idempotent`) before or together with the code. Pages use the tolerant `OrderStatus.LabelFor`, so an unmigrated `Delivered` order no longer returns 500, but until migrated it is hidden from the status filter and cannot transition. Before deploying to production, count the affected orders: `SELECT COUNT(*) FROM dbo.Orders WHERE Status='Delivered';`

## 1. Órdenes
1. After the migration: `SELECT COUNT(*) FROM dbo.Orders WHERE Status='Delivered'` → 0. Each previously Delivered order has a new history row containing "Migración automática".
2. Orders list: the status filter has no "Entregada". An Active order shows "Iniciar entrega" + "Anular". A Delivering order shows "Completar" + "Anular". A Completed order shows no transition buttons.
3. Complete a Delivering order → it becomes Completada.
4. Items of a migrated order → the history table shows "Entregada → Completada" with the migration note, and old rows show Spanish labels.
5. Pending order items: "Editar" and "Eliminar", no "Ver".
6. Active/Delivering/Completed/Voided order items: "Ver" opens the modal titled "Ver Ítem". Every field is disabled (customer and product type included), there is no Guardar button, and the image preview is visible. Close it, and "Ver Img"/"Reasignar" still work.
7. Delivering order: the "Recibido" checkbox works. Completed order: no "Recibido" column.

## 2. Metas
1. Mes Actual: four boxes. Adjust a day's actual amounts so "% al día de hoy" hits 84.9 / 85.0 / 85.9 / 86.0 / 110.0 / 110.1, and check the commission shows 0 red / 2.5 yellow / 2.5 yellow / 5 green / 5 green / 6 green with highlight.
2. The amount in parentheses = Real × rate, with the currency sign. It updates right after editing a value.
3. Histórico: a "% Comisión" column after "% Cumplimiento". Closed months use the final compliance %, and the current month matches Mes Actual.

## 3. Clientes
1. Type part of a phone number in the global search → the list narrows while you type.
2. "maria" finds "María" (accents and case ignored).
3. Combine the global search with the Email column filter → AND semantics.
4. Clear everything → the full list returns. A search with no matches shows "No se encontraron clientes".
5. With a filter active, edit, insert and delete a customer → each works, and the list still respects the filter.
