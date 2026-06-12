# Brainstorm Overview

Last updated: 2026-06-11

## Sessions

| # | Date | Topic | Status | Spec |
|---|------|-------|--------|------|
| 01 | 2026-06-03 | fase1-sales-order-management | spec-created | specs/001-fase1-order-management |
| 02 | 2026-06-03 | mejoras-fase1 | spec-created | specs/002-mejoras-fase1 |
| 03 | 2026-06-03 | refactor-orden-items | spec-created | specs/003-refactor-orden-items |
| 04 | 2026-06-06 | mejoras-fase2 | spec-created | specs/004, 006, 007, 008 |
| 05 | 2026-06-08 | quick-fixes | spec-created | specs/005-quick-fixes |
| 06 | 2026-06-08 | saldos-pagos-unificados | spec-created | specs/006-saldos-pagos |
| 07 | 2026-06-08 | pantalla-transacciones | spec-created | specs/007-transactions-screen |
| 08 | 2026-06-08 | signo-moneda | spec-created | specs/008-currency-sign |
| 09 | 2026-06-10 | cuentas-por-pagar | active | - |
| 10 | 2026-06-11 | flexibilidad-items-activos | active | - |

## Open Threads

- ¿Cómo se crea el primer `PeriodControl` si la tabla está vacía al primer acceso? (from #09)
- ¿Se puede tener más de una entrada AutoActiva por orden si pasa por múltiples cambios de estado antes de quedar Active? (from #09)
- ¿Las entradas AutoDelivered/AutoActiva pueden editarse o eliminarse manualmente desde la UI CxP? (from #09)
- ¿"Sin Cliente" debe aparecer en el dropdown del modal de *creación* de ítem (Pending) o solo en reasignación? (from #10)
- ¿El modal "Reasignar" debe validar precio = 0 con confirmación explícita? (from #10)
- ¿Los totales recalculados (TotalAgreedPriceInLocal, EstimatedProfitInLocal) se persisten inmediatamente o al siguiente guardado manual? (from #10)

## Parked Ideas

_(ninguna)_
