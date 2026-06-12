---
name: flexibilidad-items-activos
description: Reasignación de cliente y ajuste de precio en ítems de órdenes no-Pending, con cliente genérico "Sin Cliente" e impacto transaccional correcto.
metadata:
  type: project
---

# Brainstorm: Flexibilidad de Ítems en Órdenes Activas

**Date:** 2026-06-11
**Status:** active

## Problem Framing

El sistema necesita permitir modificaciones en los ítems de una orden ya activada (o en estados posteriores). Los escenarios de negocio son:

1. **Inventario especulativo:** Se compran productos sin cliente asignado. Deben verse como "potencial por cobrar" en la pantalla de Saldos.
2. **Reasignación por rechazo:** El cliente A rechaza un producto (quizás después de hacer abonos). Se reasigna al cliente B, posiblemente a otro precio. Los saldos de ambos clientes deben quedar correctos.
3. **Desasignación por preferencia:** El cliente ya no quiere el producto → se desasigna (queda sin cliente) para asignarlo luego.
4. **Descuento para retener:** Para evitar un rechazo, se baja el precio acordado con el cliente actual.
5. **Ajuste de precio al alza:** El precio negociado sube (por cambio de condiciones).

**Restricción inmutable:** El costo al proveedor (`TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`) no puede cambiar por estas operaciones.

**Fuente del análisis:** `requerimientos/6-Flexibilidad de los items.txt`

---

## Hallazgos de Código Relevantes

- `OrderItem.CustomerId` es `Guid` no-nullable.
- Al activar una orden (`TransitionOrderAsync`): se crea un `Charge` por ítem con `SourceId = item.Id, Amount = item.AgreedPriceInLocal`.
- Al anular una orden activa: se crea un `Void` por ítem. Este mecanismo se **reutiliza** para reasignaciones individuales.
- El balance de saldos = `SUM(Charge) - SUM(Payment) - SUM(Void)` por cliente.
- `AgreedPriceInLocal` es independiente de `RealPrice` — cambiar el precio acordado con el cliente **no afecta** el costo al proveedor.
- La tabla de Saldos hoy filtra `Balance > 0`; habrá que incluir balances negativos (créditos a favor).

---

## Approaches Considered

### A: "Sin Cliente" como cliente genérico + operación `ReasignarItem` (ELEGIDA)

- Agregar `Customer.IsGeneric bool`. Crear seed "Sin Cliente" con `IsGeneric = true`.
- `OrderItem.CustomerId` no cambia (sigue siendo `Guid` no-nullable). Sin migración de columna.
- Nueva operación de servicio `ReasignarItemAsync(itemId, newCustomerId, newAgreedPriceInLocal)`.
- Botón "Reasignar" visible en todos los estados excepto `Pending` y `Voided`.
- Pros: reutiliza infraestructura de Void+Charge, "Sin Cliente" aparece naturalmente en Saldos, sin migración de columna.
- Contras: "Sin Cliente" debe filtrarse del dropdown de pagos; el Saldos report necesita ajuste para saldos negativos.

### B: `CustomerId` nullable + indicador separado de potencial

- `OrderItem.CustomerId` → `Guid?`. Migración de columna requerida.
- Ítems null no generan Charge al activar. Indicador "Potencial por Cobrar" nuevo en panel CxP.
- Pros: semánticamente más limpio.
- Contras: migración + ajuste de todos los DTOs + no integra con Saldos existentes + más superficie de cambio.

### C: Tabla `OrderItemAssignment` para auditoría completa

- Nueva tabla de asignaciones con historial. La "actual" = fila sin `UnassignedAt`.
- Pros: historial explícito de cada asignación.
- Contras: overkill para el volumen actual; el historial transaccional ya provee auditoría implícita; complejidad innecesaria.

---

## Decision

**Enfoque A.** Reutiliza infraestructura existente (Void+Charge), no requiere migración de columna, el saldo de "Sin Cliente" cubre el "potencial por cobrar" de forma nativa en Saldos, y la restricción del costo al proveedor se cumple naturalmente.

---

## Key Requirements

### Modelo de datos
- `Customer.IsGeneric bool DEFAULT false` — nueva columna.
- Seed: un cliente `"Sin Cliente"` con `IsGeneric = true`. Aparece en todos los dropdowns de cliente en la pantalla de ítems.

### Operación `ReasignarItem` (nueva)
- **Entrada:** `itemId, newCustomerId, newAgreedPriceInLocal`
- **Disponible en:** estados `Active`, `Delivering`, `Delivered`, `Completed`
- **Bloqueada en:** `Pending` (se usa edición normal) y `Voided`

### Lógica transaccional por caso

| Caso | Transacciones |
|------|--------------|
| Cambio de cliente (mismo precio) | Void(clienteAnterior, montoOriginal) + Charge(clienteNuevo, montoOriginal) |
| Cambio de cliente + cambio de precio | Void(clienteAnterior, montoOriginal) + Charge(clienteNuevo, nuevoPrecio) |
| Mismo cliente, precio sube | Charge(cliente, diferencia) — descripción: "Ajuste de precio – {ProductDescription}" |
| Mismo cliente, precio baja (descuento) | Payment(cliente, diferencia) — descripción: "Descuento por ajuste de precio – {ProductDescription}" |

- El Void identifica el Charge original usando `Transaction.SourceId = item.Id`

### Recálculo de orden post-reasignación
- **Sí:** `TotalAgreedPriceInLocal = SUM(AgreedPriceInLocal)`, `EstimatedProfitInLocal`
- **No:** `TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`, `ShippingAmountToCR`

### UI — Botón "Reasignar" en Orders/Items
- Visible como acción de fila en `Orders/Items.cshtml` cuando `Order.Status` ∈ {Active, Delivering, Delivered, Completed}
- Abre modal con:
  - Dropdown "Cliente" (todos los clientes incluyendo "Sin Cliente") — precargado con cliente actual
  - Campo "Precio Acordado" — precargado con `AgreedPriceInLocal` actual
- Al confirmar: llama `ReasignarItemAsync`

### Pantalla de Saldos (Payments/Index)
- Remover filtro `WHERE Balance > 0` de `GetSaldosReportAsync`
- Mostrar filas con balance negativo con etiqueta "Crédito a favor" (en color diferente)
- "Sin Cliente" aparece en la tabla con etiqueta "(Especulativo)"

---

## Open Questions

- ¿Debe "Sin Cliente" aparecer en el dropdown de cliente del modal de *creación* de ítem (en Pending), o solo en el de reasignación?
- ¿El modal "Reasignar" debe validar que si el nuevo precio es 0 se requiera confirmación explícita?
- ¿Los totales recalculados (`TotalAgreedPriceInLocal`, `EstimatedProfitInLocal`) deben persistirse inmediatamente o solo al siguiente guardado manual de la orden?
