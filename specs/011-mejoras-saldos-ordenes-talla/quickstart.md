# Quickstart / Manual Verification: 011-mejoras-saldos-ordenes-talla

## Setup

```bash
cd MariCamiStore
dotnet build
dotnet ef database update     # applies AddOrderItemSize to the Development DB
dotnet run
```

## 1. Saldos total (Payments)

1. Open `/Payments`. With balances e.g. 10 000, 5 000, −2 000 → footer "Total" shows 13 000 in bold, local currency sign.
2. Type part of one customer's name in "Filtrar por cliente..." → Total equals that customer's balance.
3. Type text that matches nobody → row "Ningún cliente coincide con el filtro", Total 0.
4. Filter to only customers with credit → Total shown with "−" prefix, no badge.
5. Register a payment → table reloads, Total updated.

## 2. Orders status checkboxes

1. Open Orders from the menu → six checkboxes, only Pendiente and Activa checked; no combo.
2. Check Entregada → grid reloads automatically including delivered orders.
3. Uncheck all → grid empty.
4. Toggle several boxes quickly → final grid matches final selection.

## 3. Volver keeps selection

1. Select only Entregada + Completada → click "Items" on an order → URL contains `statuses=Delivered,Completed`.
2. Click "Volver" → Orders with exactly Entregada + Completada checked.
3. Click Orders in the menu → back to Pendiente + Activa.
4. Visit `/Orders?statuses=Foo` → defaults; `/Orders?statuses=Foo,Voided` → only Anulada.

## 4. Talla

1. Create an item with Talla "XL" → edit → shows "XL".
2. Edit with Talla empty → saves.
3. Try typing 25 chars → input stops at 20.
4. Enter " M " → saved as "M".
5. Open an item created before the migration → Talla empty, saves fine.
6. Items grid has no Talla column.
7. In an Active order, reassign an item with Talla "S" (Reasignar) → edit shows Talla still "S".
