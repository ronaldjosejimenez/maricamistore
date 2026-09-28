# Quickstart / Manual Verification: 013-paquetes-orden

```bash
cd MariCamiStore
dotnet build
dotnet ef database update   # DEV database only — applies AddOrderPackages
dotnet run
```
Needs an open CxP period and orders in several states.

1. **Pending order** → Items page: no "Paquetes de Envío" card.
2. **Active order** (estimated shipping e.g. $100): card shows summary (100 / 0 / 100), add form, empty list.
   - Add $30 "Caja 1" (date defaults to today) → list shows it; summary 100 / 30 / 70.
   - CxP → entries: "Auto-Paquete", "{Orden} - Caja 1", $30. "Shipping CR Pendientes" includes 70 × TC.
   - Add without description → reference is only the order name.
   - Amount 0 or empty → validation message, nothing saved.
   - Double-click Agregar → only one package.
3. **Delete** the $30 package → confirm → list empty; CxP has "Reverso Paquete" −$30 in red with "Reverso: {Orden} - Caja 1"; Shipping CR Pendientes back to previous value.
4. **Over estimate**: packages totaling $120 on a $100 order → summary Pending 0; order contributes 0 to CxP indicator.
5. **Delivering order**: same as Active (add/delete allowed); its pending shipping now counts in CxP.
6. **Delivered / Completed / Voided**: list + summary read-only, no form, no delete.
7. **No open period** (e.g. test DB with period closed and none open): add/delete → "No hay un período CxP abierto."
8. **Transition Entregando → Entregada**: dialog shows only Fecha/Notas; no CxP entry created.
9. **CxP deletion**: delete an Auto-Paquete entry from CxP → package still in the order.
10. Server-side: POST AddPackage for a Delivered order (dev tools) → rejected.
