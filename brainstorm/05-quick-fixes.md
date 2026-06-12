# Brainstorm: Quick Fixes — Mejoras varias fase 2

**Fecha**: 2026-06-08  
**Fuente**: `requerimientos/Mejoras varias fase 2.txt` ítems 2 y 4 (parcial)

---

## Problema

Hay dos pequeños bugs que afectan la experiencia:

1. **Decimales en Envío Estimado**: El campo `estimateShipping` en la pantalla de Tipos de Producto (`/ProductTypes`) usa jsGrid `type: 'number'` sin `step`, así que el browser lo trata como entero. Los precios de envío necesitan decimales (ej: $12.50).

2. **SourceId null en pagos manuales**: `PaymentService.RegisterPaymentAsync` asigna `SourceId = Guid.Empty` en vez de `null` cuando registra un pago manual. `SourceId` es `Guid?` y debe ser `null` si no hay origen de orden.

---

## Solución propuesta

### Fix 1 — `step: 0.01` en estimateShipping

Archivo: `MariCamiStore/wwwroot/js/pages/product-types/index.js`

Agregar `step: 0.01` al field `estimateShipping` en la configuración de jsGrid. Una línea.

El modelo ya es `decimal(18,2)`, no se requiere migración.

### Fix 2 — `SourceId = null`

Archivo: `MariCamiStore/Services/PaymentService.cs`

Cambiar `SourceId = Guid.Empty` → `SourceId = null`. Una línea.

La columna ya admite NULL en BD, no se requiere migración.

---

## Notas

- Los dos fixes son independientes entre sí.
- Ítem 5 (Saldos) ya está implementado correctamente.
