---
name: fase1-sales-order-management
description: Sistema completo de gestión de ventas y órdenes — Fase 1. Multi-tenant, Razor Pages + AJAX, ASP.NET Core 10.
metadata:
  type: project
---

# Brainstorm: Fase 1 — Sales & Order Management System

**Date:** 2026-06-03
**Status:** active

## Problem Framing

MariCamiStore necesita un sistema de gestión de ventas y órdenes multi-tenant para operar en Costa Rica. El negocio compra productos a proveedores externos (Amazon, Shein, etc.) y los revende localmente en colones. El sistema debe rastrear órdenes de compra, calcular costos y márgenes, gestionar pagos de clientes, y mantener un ledger contable inmutable de todas las transacciones financieras.

El proyecto ya tiene los modelos de dominio definidos y migraciones existentes, pero carece de la lógica de negocio, la infraestructura multi-tenant, y las vistas AJAX funcionales.

## Approaches Considered

### A: Implementación incremental por capas (Elegida)
- Pros: Cada capa es funcional antes de avanzar. Los catálogos se completan antes de las órdenes (dependencia real del negocio). Facilita verificación incremental. Menor riesgo de inconsistencias en una entrega grande.
- Cons: Tarda más en ver el flujo completo de negocio.

### B: Core primero (Orders → Catálogos)
- Pros: Valida la lógica de negocio compleja (status machine, transaction automation) desde el inicio.
- Cons: Requiere datos semilla. Catálogos quedan como deuda técnica visible.

### C: Todo en una pasada
- Pros: Entrega todo a la vez.
- Cons: Mayor riesgo de inconsistencias. Difícil de debuggear errores parciales.

## Decision

**Enfoque A: Implementación incremental por capas**, en este orden:
1. Infraestructura multi-tenant
2. CRUDs de catálogos
3. Order management + business logic
4. Reportes

## Key Requirements

### Arquitectura
- Framework: ASP.NET Core 10, Razor Pages + jsGrid AJAX (mantener arquitectura existente)
- UI: AdminLTE 3.0.5, Bootstrap/jQuery. UI en español, código 100% en inglés.
- Multi-tenancy: `ICurrentOrganizationService` (scoped service) inyectado en DbContext para Global Query Filters.
- Session: Selector global de Organization en el navbar, persiste `OrganizationId` en `HttpContext.Session`.

### Capa 1 — Infraestructura Multi-tenant
- Crear `ICurrentOrganizationService` + `CurrentOrganizationService` que lee `OrganizationId` de sesión.
- Agregar Global Query Filters en `MariCamiStoreContext` para todas las entidades con `OrganizationId` (Orders, OrderItems, Transactions, etc.). Customer es global (sin filtro).
- Agregar dropdown de Organization en el layout navbar.
- Bloquear UI con spinner global durante requests AJAX.

### Capa 2 — CRUDs de Catálogos (Razor Pages + jsGrid)
- Configuration: parámetros del sistema (TaxPercentage, ExchangeRate, LocalCurrencyId). CRUD.
- Currency: catálogo de monedas. CRUD.
- ProductType: catálogo con campos `EstimateShipping` y `ServiceFeeInLocal` (valores fijos predefinidos). CRUD.
- Supplier: catálogo de proveedores/sitios. CRUD.
- Customer: catálogo global (sin filtro por organización). CRUD.

### Capa 3 — Order Management & Business Logic
- **Order Management Dashboard**: lista de órdenes con filtro default (Pending + Active). Crear/editar órdenes. Cambio de status con state machine.
- **State machine de Order.Status**: `Pending → Active → Delivering → Delivered → Completed`. Puede ser `Voided` desde cualquier estado excepto `Pending`. Campos son editables solo en estado `Pending`.
- **OrderStatusHistory**: Nuevo modelo para registrar cada cambio de estado. Evita múltiples campos de fecha en `Order`. Campos: `OrderId`, `FromStatus`, `ToStatus`, `TransitionDate` (default hoy, editable), `Notes` (requerido solo en Voided), `CreatedAt`.
- **Datos por transición de estado** (todas requieren confirmación + fecha editable, default = hoy):
  - `Pending → Active`: fecha de activación
  - `Active → Delivering`: fecha estimada de entrega
  - `Delivering → Delivered`: fecha real de entrega
  - `Delivered → Completed`: fecha de completado
  - `* → Voided` (excepto Pending): fecha de anulación + justificación **(requerida)**
- **Order Items Editor**: CRUD de items de una orden. Habilitado solo si `Order.Status == Pending`. Al seleccionar `ProductTypeId`, auto-llenar `EstimateShipping` y `ServiceFeeInLocal` desde el catálogo.
- **Fórmulas calculadas** (valores se recalculan en tiempo real con JS; se persisten al guardar; todos son editables manualmente):
  - `ListPriceTax = ListPrice * TaxPercentage`
  - `AgreedPriceInLocal = (ListPrice + ListPriceTax) * ExchangeRate + ServiceFeeInLocal`. UI muestra el valor calculado como referencia pero permite editarlo.
  - `TotalWithoutTaxes = Sum(RealPrice) de OrderItems`
  - `TaxesAmount = Sum(ListPriceTax) de OrderItems`
  - `TotalToPayToSupplier = TotalWithoutTaxes + TaxesAmount + ShippingAmountIntern - DiscountAmount`
  - `ShippingAmountToCR = Sum(EstimateShipping) de OrderItems` (editable, pero se sobreescribe si cambia algún item)
  - `TotalOfTheOrder = TotalToPayToSupplier + ShippingAmountToCR`
  - `EstimatedProfitInLocal = Sum(AgreedPriceInLocal) - TotalOfTheOrder * ExchangeRate`

### Capa 3 — Transaction Automation (Ledger)
- **Order → Active**: Crear una `Transaction` por cada `OrderItem` con `TransactionType = Charge`, `Amount = AgreedPriceInLocal`.
- **Order → Voided** (desde cualquier estado ≠ Pending): Crear una `Transaction` por cada `OrderItem` con `TransactionType = Void`, `Amount = AgreedPriceInLocal`.
- **Pago registrado**: Crear una `Transaction` con `TransactionType = Payment`, `Amount = monto pagado`.
- Transacciones son inmutables una vez creadas (no se editan ni borran).

### Capa 4 — Reportes
- **Payment Registry**: UI para seleccionar Customer + ingresar monto. Al guardar, registra Payment transaction. Muestra balance total del cliente (global + por organización activa).
- **Accounts Receivable (Saldos)**: Lista de clientes con balance > 0. Fórmula: `Sum(Charge) - Sum(Payment + Void)`. Columnas: Nombre, Total Adeudado.

### Fuera de Scope Fase 1
- Autenticación / Autorización (código comentado en Program.cs)
- Azure Container Apps deployment
- Tests automatizados
- Notificaciones o emails

## Open Questions

_(todas resueltas en sesión de revisión del 2026-06-03)_
