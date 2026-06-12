# Design: Quick Fixes — Mejoras varias fase 2

**Spec**: 005 | **Date**: 2026-06-08 | **Source**: `requerimientos/Mejoras varias fase 2.txt` ítems 2, 4 (parcial), 5

---

## Fix 1 — Decimales en Envío Estimado (ítem 2)

**Problema**: El campo "Envío Estimado" en la pantalla de Tipos de Producto (`/ProductTypes`) usa jsGrid `type: 'number'` que no configura `step`, por lo que el browser trata el input como entero.

**Solución**: Agregar `step: 0.01` al field `estimateShipping` en `MariCamiStore/wwwroot/js/pages/product-types/index.js`.

**Archivos**: Solo `product-types/index.js` — una línea.  
**Sin migración** — el modelo ya es `decimal(18,2)`.

---

## Fix 2 — SourceId null en pagos manuales (ítem 4, parcial)

**Problema**: `PaymentService.RegisterPaymentAsync` asigna `SourceId = Guid.Empty` en lugar de `null` al registrar un pago manual. Semánticamente incorrecto — `SourceId` es `Guid?` y debe ser `null` cuando no hay origen de orden.

**Solución**: Cambiar `SourceId = Guid.Empty` → `SourceId = null` en `MariCamiStore/Services/PaymentService.cs`.

**Archivos**: Solo `PaymentService.cs` — una línea.  
**Sin migración** — columna ya admite NULL en BD.

---

## Ítem 5 — Fix Saldos ✓ Ya implementado

La fórmula `Cargo − Pago − Anulación` en `PaymentService.GetSaldosReportAsync` está correcta y verificada por pruebas manuales. No requiere cambios. Se documenta aquí como referencia de trazabilidad con el requerimiento.

---

## Notas

- Los dos fixes son independientes entre sí.
- Fix 2 está en `PaymentService.cs` y se puede hacer en el mismo commit que otros cambios del servicio de pagos (Spec 006).
- El reporte de Saldos en `/Reports/Saldos` será eliminado por el Spec 006 (unificación).
