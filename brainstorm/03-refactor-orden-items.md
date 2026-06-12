---
name: refactor-orden-items
description: Refactoring mayor del sistema de Orden e Items — modal para ítems, edición in-place, nuevos campos, renombres, lógica de cálculo completa.
metadata:
  type: project
---

# Brainstorm: Refactor Orden + Items

**Date:** 2026-06-03
**Status:** active

## Problem Framing

El sistema de gestión de Órdenes e Items de Orden (Fase 1) requiere un refactoring mayor para alinear la UI, los modelos, el esquema de DB y la lógica de cálculo con los requerimientos funcionales definidos en `requerimientos/Mejoras Fase 1 v1.txt`:

1. El formulario de ítems inline tiene demasiados campos — necesita un modal dedicado.
2. El diálogo separado de edición de Orden introduce fricción — debe reemplazarse por edición in-place en la pantalla de resumen.
3. Faltan campos en los ítems: `ProductLink`, `ProductSourceCode`, `ProductImage`.
4. El campo `ListPriceTax` debe renombrarse a `ListPriceTaxWithTax` (modelo C# + columna DB).
5. La Orden carece de un campo `TotalAgreedPriceInLocal` y el campo `RequestedAt` debe eliminarse.
6. La lógica de cálculo (triggers, fórmulas) no está formalizada ni implementada de forma completa.

## Approaches Considered

### A: Spec único "Refactor Orden+Items" con fases internas (Elegido)
- **Fase I:** Migraciones de DB (rename columna, nuevas columnas, nuevo campo en Order, eliminación de RequestedAt).
- **Fase II:** Modelos y servicios backend (computed properties, lógica de cálculo, triggers).
- **Fase III:** Frontend (modal de ítems, edición in-place, cálculos JS reactivos, UI warning).
- Pros: Un solo pipeline, cambios cohesivos, no hay dependencias entre specs. Review unificado.
- Cons: Spec más largo. Una sola PR grande.

### B: Dos specs separados (Backend + Frontend)
- Pros: Specs más enfocados. Backend revisable antes de empezar frontend.
- Cons: El frontend depende del backend — hay que coordinar el orden. Dos PRs, más overhead.

### C: Implementación directa sin spec formal
- Pros: Velocidad máxima dado que el documento de requerimientos es muy detallado.
- Cons: Sin trazabilidad, sin gate de calidad. No alineado con el flujo SDD del proyecto.

## Decision

**Enfoque A: Spec único** con fases internas ordenadas: DB migrations → Backend → Frontend.

## Key Requirements

### UI & Estructura
- **Modal para ítems:** Reemplazar la adición inline de ítems por un Form/Modal dedicado (agregar y editar).
- **Edición in-place de Orden:** Eliminar el diálogo separado de edición. La pantalla de resumen de la orden permanece en "edit mode" mientras el estado sea `Pending`.
- **UI Warning dinámico:** Mostrar el label "Presionar guardar para recalcular si hay algo pendiente de guardar" solo cuando hay cambios sin guardar (dirty state detectado en JS).
- **Formulario de creación (nueva orden):** Mostrar solo: `SupplierId`, `NameOfOrder`, `ExchangeRate`, `TaxPercentage`. Ocultar el resto.

### Cambios en el Modelo Order
- **Eliminar:** Campo `RequestedAt` del modelo y la DB.
- **Agregar:** Campo `TotalAgreedPriceInLocal` (decimal) en el modelo y la DB.
- **Campos editables en Pending:** `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`.
- **Campos no editables en Pending:** `SupplierId`, `NameOfOrder`, `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal`.

### Cambios en el Modelo OrderItem
- **Nuevos campos (nullable):** `ProductLink` (text), `ProductSourceCode` (text).
- **Nuevo campo imagen:** `ProductImage` (VARBINARY(MAX), opcional/nullable). Vista previa en popup en modo Query.
- **Rename:** `ListPriceTax` → `ListPriceTaxWithTax` — en el modelo C# Y en la columna de la DB (migración ALTER TABLE).
- **Filtro ProductTypeId:** Solo mostrar ProductTypes cuya moneda coincida con la moneda de la Orden.
- **ServiceFeeInLocal:** Cargar default desde ProductType al seleccionar. Refrescar si cambia ProductType. (Ya existe en ProductType.)
- **EstimateShipping:** Cargar default desde ProductType al seleccionar. Editable manualmente. (Ya existe en ProductType.)

### Campos Calculados en OrderItem
- **ListPriceTaxWithTax (editable + calculado):** `ListPrice + TaxAmount`. Se calcula al vuelo al escribir. `TaxAmount = ListPrice * TaxPercentage`.
- **Total (computed, no persiste):** `(ListPriceTaxWithTax * ExchangeRate) + ServiceFeeInLocal`. Computed property en C# + cálculo JS en tiempo real.
- **AgreedPriceInLocal:** Default = `Total`. Editable manualmente. Se actualiza si cambia `ListPrice`, `ServiceFeeInLocal`, o el `ExchangeRate` de la orden (para TODOS los ítems).
- **RealPrice:** Default = `ListPrice`. Editable manualmente.

### Campos Incluidos en la Vista Query
`CustomerId`, `ProductDescription`, `ProductSourceCode`, `ServiceFeeInLocal`, `Total`, `AgreedPriceInLocal`, `RealPrice`, `EstimateShipping`.

### Lógica de Triggers de Recálculo

**Trigger A — Al guardar la Orden** (por cambios en `TaxPercentage` o `ExchangeRate`):
- Recalcular a nivel ítem: `ListPriceTaxWithTax` y `ServiceFeeInLocal`.
- NO recalcular `AgreedPriceInLocal` en este trigger.
- Ejecutar Order Calculation Suite.

**Trigger B — Al agregar, editar o eliminar un ítem:**
- Ejecutar Order Calculation Suite inmediatamente.

### Order Calculation Suite (ejecutar en orden)
1. `TotalAgreedPriceInLocal` = Σ `AgreedPriceInLocal` de todos los ítems.
2. `ShippingAmountToCR` = Σ `EstimateShipping` de todos los ítems.
3. `TotalWithoutTaxes` = Σ `RealPrice` de todos los ítems.
4. `TaxesAmount` = `(TotalWithoutTaxes - DiscountAmount) * TaxPercentage`.
5. `TotalToPayToSupplier` = `ShippingAmountIntern + TotalWithoutTaxes + TaxesAmount - DiscountAmount`.
6. `TotalOfTheOrder` = `TotalToPayToSupplier + ShippingAmountToCR`.
7. `EstimatedProfitInLocal` = `TotalAgreedPriceInLocal - (TotalOfTheOrder * ExchangeRate)`.

## Open Questions

_(todas resueltas en revisit del 2026-06-04)_

---

## Revisit: 2026-06-04

### Preguntas Abiertas Resueltas

**¿La pantalla de detalle/resumen de la orden es rediseño de la página existente o nueva página?**
- Respuesta: Rediseño de `Orders/Items.cshtml` (página existente). Para agregar/editar ítems se usa un **modal scrollable** dentro de esa misma página, consistente con el modal de "Nueva Orden" ya existente. Sin cambios de ruta ni navegación.

**¿Hay límite de tamaño para las imágenes de producto?**
- Respuesta: **2 MB**. Validación tanto en frontend (antes de enviar) como en backend (antes de guardar).
