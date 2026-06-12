# Brainstorm: Cuentas por Pagar

**Date:** 2026-06-10
**Status:** active

## Problem Framing

La tienda necesita un módulo de control de Cuentas por Pagar (CxP) hacia proveedores. Actualmente no existe ningún registro estructurado de lo que se debe, y los saldos se llevan manualmente fuera del sistema. El módulo debe integrar automáticamente los montos generados por el flujo de órdenes (al activar y al entregar), manejar un concepto de "mes de transacción" distinto al calendario natural, y proporcionar un panel financiero consolidado con indicadores clave para la toma de decisiones.

## Approaches Considered

### A: Página única con panel de control arriba y tablas por moneda abajo (ELEGIDA)
- Pros: Todo visible en una sola URL sin cambios de contexto; el indicador "Posición" es visible mientras se revisan las entradas; consistente con el patrón del proyecto (Payments tiene form + tabla en una página)
- Cons: Página densa si hay muchas monedas activas en el mes

### B: Dos páginas separadas — Entradas y Control del Período
- Pros: Separación de responsabilidades, cada página más simple
- Cons: Navegar entre pantallas rompe el flujo; la "Posición" está en otra pantalla al revisar entradas

### C: Página única con tabs
- Pros: Menor densidad por tab
- Cons: La Posición queda oculta en otro tab; los tabs no son el patrón dominante en AdminLTE de este proyecto

## Decision

Enfoque A — página única `/CxP/Index` con:
- Panel de control del período activo al tope
- Tablas de entradas agrupadas por moneda debajo
- Botón "+ Agregar entrada" (modal para entrada manual)
- Botón "Cerrar Mes"

## Key Requirements

### Gestión de Período (`PeriodControl`)
- Tabla `PeriodControl`: `Id`, `TransactionMonth`, `TransactionYear`, `ExchangeRate`, `PagosRealizados`, `EnCuenta`, `IsClosed`
- El "mes actual" es el período con `IsClosed = false` más reciente
- Al cerrar: `IsClosed = true`, se crea nuevo `PeriodControl` para el mes siguiente, y se registra un `CxPEntry` de tipo `SaldoAnterior` con el valor de `DeudaAPagar` en la moneda de Colones
- Períodos cerrados son de solo lectura (no se pueden agregar ni editar entradas)
- El historial de períodos cerrados se conserva en BD aunque no haya UI de consulta en esta versión

### Entradas CxP (`CxPEntry`)
- Tabla `CxPEntry`: `Id`, `PeriodControlId`, `CurrencyId`, `Amount`, `Reference`, `Type` (Manual / AutoActiva / AutoDelivered / SaldoAnterior), `OrderId` (nullable), `CreatedAt`
- **Manual**: creadas por el usuario con referencia libre, moneda y monto
- **AutoActiva**: creada automáticamente en `TransitionOrderAsync` cuando orden → `Active`; monto = `TotalToPayToSupplier`; moneda = moneda de la orden
- **AutoDelivered**: creada automáticamente en `TransitionOrderAsync` cuando orden → `Delivered`; monto = `ActualShippingAmountToCR`; moneda = moneda de la orden
- **SaldoAnterior**: creada automáticamente al cerrar el mes; siempre en Colones

### Campo nuevo en `Order`
- `ActualShippingAmountToCR` (decimal): capturado en el diálogo de transición a `Delivered`, pre-llenado con `ShippingAmountToCR` existente; no afecta ningún cálculo previo de la orden

### Indicadores calculados del período (pantalla)
| Indicador | Fórmula / Origen |
|---|---|
| Tipo de Cambio | `PeriodControl.ExchangeRate` (editable) |
| Por pagar en {Moneda} | SUM(CxPEntry.Amount) WHERE CurrencyId = {Moneda} |
| Por pagar en Colones | SUM de todos los saldos convertidos usando ExchangeRate (Colones sin conversión) |
| Saldos por Cobrar | SUM de `GetSaldosReportAsync` (todos en colones) |
| Pagos Realizados | `PeriodControl.PagosRealizados` (editable, en colones) |
| Deuda a Pagar | Por pagar en Colones − Pagos Realizados |
| En Cuenta | `PeriodControl.EnCuenta` (editable, en colones) |
| Pendiente de Recoger | Deuda a Pagar − En Cuenta (puede ser negativo) |
| Shipping CR Pendientes | SUM(ShippingAmountToCR de órdenes Activas) × ExchangeRate |
| **Posición** | Saldos por Cobrar + En Cuenta − Deuda a Pagar − Shipping CR Pendientes |

- "Posición" se muestra en negrita y fuente más grande

### Navegación
- Nueva sección "Cuentas por Pagar" en el menú lateral con ítem único que lleva a `/CxP/Index`

### Regla de anulación de órdenes
- No hay reversión automática de CxPEntry al anular una orden; se elimina manualmente si es necesario

## Open Questions

- ¿Cómo se crea el primer `PeriodControl` si la tabla está vacía? (¿bootstrap automático al primer acceso o pantalla de inicialización?)
- ¿Se puede tener más de una entrada AutoActiva por orden si la orden pasa por múltiples estados intermedios antes de Active?
- ¿Las entradas AutoDelivered/AutoActiva deben poder editarse o eliminarse manualmente desde la UI, o son solo de lectura?
