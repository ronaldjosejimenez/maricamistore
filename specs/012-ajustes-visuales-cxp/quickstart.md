# Quickstart / Manual Verification: 012-ajustes-visuales-cxp

```bash
cd MariCamiStore && dotnet build && dotnet run
```

Open `/CxP` with an open period.

1. **Labels**: one box "Total por pagar colonizado" (same value as the old "Por pagar en Colones"); per-currency boxes still "Por pagar en {Moneda}"; "Saldos por Cobrar a Clientes".
2. **Layout (desktop)**: three equal columns — (1) Deuda a Pagar (red box) + Pendiente de Recoger (yellow box), both with icon and bold number; (2) Shipping CR Pendientes + Saldos por Cobrar a Clientes; (3) Total por pagar colonizado + per-currency boxes stacked.
3. **Only colored boxes** are Deuda a Pagar and Pendiente de Recoger.
4. **Phone width** (<768px, browser dev tools): columns stack 1-2-3; heading/total/button wrap; no horizontal scroll.
5. **Entradas heading**: "Total por pagar colonizado: ₡X" equals the panel value; add an entry, delete it, change Tipo de Cambio and save → both values update together.
6. **Unchanged**: Posición, editable fields, Guardar, Cerrar Mes, TC=0 warning, entries tables.
7. **Closed period** (if available): "Agregar entrada" hidden, total text still visible.
8. **No entries**: column 3 shows only the total (₡0,00); heading shows ₡0,00.
