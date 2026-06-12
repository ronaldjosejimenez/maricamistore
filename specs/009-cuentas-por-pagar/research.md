# Research: Módulo Cuentas por Pagar

**Feature**: 009-cuentas-por-pagar
**Date**: 2026-06-10

## Decision: Dónde enganchar los registros CxP automáticos

**Decision**: Inyectar `ICxPService` en `OrderService` y llamarlo desde `TransitionOrderAsync`.

**Rationale**: `TransitionOrderAsync` ya crea `Transactions` de forma análoga (cargos al cliente cuando la orden se activa). El mismo punto de extensión sirve para CxP. No hay dependencia circular: `ICxPService` depende de `IPaymentService` e `ICurrentOrganizationService`; `IOrderService` depende de `ICxPService`.

**Alternatives considered**:
- Domain events / mediator: sobrecomplejo para esta escala de proyecto.
- Page-level call from transition handler: rompería el encapsulamiento de lógica de negocio.

---

## Decision: Estructura del modelo de datos CxP

**Decision**: Dos nuevas tablas (`PeriodControl`, `CxPEntry`) más un campo nuevo en `Orders`. Global query filter de organización en `PeriodControl`.

**Rationale**: El proyecto ya usa global query filters por `OrganizationId` en `Configuration`, `Order` y `Transaction`. `PeriodControl` sigue el mismo patrón. `CxPEntry` hereda el filtro implícitamente a través de `PeriodControl`.

**Alternatives considered**:
- Un solo modelo de período sin tabla de entradas (campos de totales almacenados): descartado por pérdida de trazabilidad.
- Reutilizar `Transaction` para CxP: los dominios son distintos (cobros a clientes vs. pagos a proveedores); mezclarlos añade complejidad sin beneficio.

---

## Decision: `ActualShippingAmountToCR` en la orden

**Decision**: Agregar `decimal ActualShippingAmountToCR` a `Order` (no nullable, default 0). Se llena solo al transicionar a Delivered.

**Rationale**: Persistir el valor real en la entidad `Order` da trazabilidad (cuánto se pagó realmente de shipping para esa orden). Null no es semánticamente útil: si la orden nunca llega a Delivered, el campo queda en 0 sin ambigüedad.

**Alternatives considered**:
- `decimal?` nullable: no agrega valor; 0 es un estado válido y sin ambigüedad.
- Solo almacenar en `CxPEntry` sin campo en `Order`: la orden pierde historia de su shipping real.

---

## Decision: Extensión de `TransitionOrderDto`

**Decision**: Agregar parámetro `decimal? ActualShippingAmountToCR = null` al final del record posicional.

**Rationale**: Los records de C# con parámetros opcionales al final son retrocompatibles con todas las llamadas existentes que no pasan el parámetro.

**Alternatives considered**:
- Nuevo DTO solo para Delivered: innecesario; un campo opcional es suficiente.

---

## Decision: Alcance del servicio `ICxPService`

**Decision**: `ICxPService` no depende de `IOrderService`. Solo lee órdenes en estado Active directamente del `DbContext` para calcular "Shipping CR Pendientes".

**Rationale**: Evita dependencia circular. `IOrderService` → `ICxPService`, no al revés.

---

## Decision: Registro de navegación

**Decision**: Agregar sección nueva "Cuentas por Pagar" (`menuCxP`) en `_Layout.cshtml`, separada de la sección "Finanzas" existente.

**Rationale**: El usuario pidió explícitamente una nueva sección; además CxP es un dominio distinto a los pagos a clientes y transacciones.

---

## Decision: Identificación de la moneda Colones

**Decision**: `Configuration.LocalCurrencyId` identifica la moneda local (Colones). Se usa para las entradas de tipo `SaldoAnterior` y en los cálculos de conversión.

**Rationale**: El patrón ya existe en `BuildTransaction()` de `OrderService` que usa `config?.LocalCurrencyId`. No se necesita lógica adicional de búsqueda por nombre.

---

## Decision: Patrón de indicadores del panel

**Decision**: `ICxPService.GetPeriodIndicatorsAsync` retorna un DTO con todos los indicadores calculados en el servidor. La página recibe los valores ya computados.

**Rationale**: Simplifica el JS del cliente. Los cálculos son pocos y el conjunto de datos es pequeño (escala de una tienda). No hay riesgo de rendimiento.

---

## Decision: No global query filter en `CxPEntry`

**Decision**: `CxPEntry` no tiene `OrganizationId` propio. El filtro por organización se aplica al cargar `PeriodControl` (que sí tiene `OrganizationId`).

**Rationale**: Simplifica el modelo. Las entradas son siempre accedidas a través del período, que ya filtra por org.
