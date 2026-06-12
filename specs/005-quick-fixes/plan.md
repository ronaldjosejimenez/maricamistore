# Implementation Plan: Quick Fixes — Decimales y SourceId Null

**Branch**: `005-quick-fixes` | **Date**: 2026-06-08 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-quick-fixes/spec.md`

## Summary

Dos correcciones mínimas independientes entre sí:
1. Agregar `step: 0.01` al campo `estimateShipping` en la configuración jsGrid de Tipos de Producto — permite ingresar decimales en el campo de envío estimado.
2. Cambiar `SourceId = Guid.Empty` → `SourceId = null` en `PaymentService.RegisterPaymentAsync` — corrige la semántica del campo cuando un pago manual no tiene orden asociada.

## Technical Context

**Language/Version**: C# / .NET 8, JavaScript (ES5 compatible con jQuery)

**Primary Dependencies**: ASP.NET Core Razor Pages, jsGrid 1.5.3, Entity Framework Core 8

**Storage**: SQL Server — no se requieren migraciones (columnas ya soportan los tipos correctos)

**Testing**: Manual — verificar en navegador (campo decimal) y en BD (SourceId IS NULL)

**Target Platform**: ASP.NET Core web app en Windows/Linux

**Project Type**: Web application (Razor Pages + AJAX)

**Performance Goals**: N/A — cambios de presentación y semántica, sin impacto de rendimiento

**Constraints**: Sin migraciones. Solo 2 archivos modificados.

**Scale/Scope**: 2 cambios de 1 línea cada uno.

## Constitution Check

No hay constitución activa. Sin violations.

## Project Structure

### Documentation (this feature)

```text
specs/005-quick-fixes/
├── spec.md              ✓
├── plan.md              # Este archivo
└── tasks.md             # Por generar con /speckit-tasks
```

### Source Code (archivos modificados)

```text
MariCamiStore/
├── wwwroot/js/pages/product-types/
│   └── index.js         ← Fix 1: agregar step: 0.01 (línea 44)
└── Services/
    └── PaymentService.cs ← Fix 2: SourceId = null (línea 36)
```

## Implementation Details

### Fix 1 — `step: 0.01` en `estimateShipping`

**Archivo**: `MariCamiStore/wwwroot/js/pages/product-types/index.js:44`

**Cambio**:
```js
// Antes:
{ name: 'estimateShipping', title: 'Envío Estimado', type: 'number', width: 130, validate: 'required' },

// Después:
{ name: 'estimateShipping', title: 'Envío Estimado', type: 'number', step: 0.01, width: 130, validate: 'required' },
```

**Por qué**: jsGrid `type: 'number'` renderiza un `<input type="number">`. Sin `step`, el browser establece `step="1"` por defecto, bloqueando decimales. Con `step: 0.01`, el campo acepta valores con hasta 2 decimales.

### Fix 2 — `SourceId = null` en `PaymentService`

**Archivo**: `MariCamiStore/Services/PaymentService.cs:36`

**Cambio**:
```csharp
// Antes:
SourceId = Guid.Empty,

// Después:
SourceId = null,
```

**Por qué**: `SourceId` es `Guid?` (nullable). `Guid.Empty` es semánticamente incorrecto — implica que existe un origen con ID vacío. `null` comunica correctamente que el pago no tiene un ítem de orden asociado. Esto es importante para filtrar transacciones manuales vs. automáticas en futuras pantallas.

## Verification

1. Abrir `/ProductTypes`, editar un registro → campo "Envío Estimado" debe aceptar `12.50` sin redondear.
2. Registrar un pago desde `/Payments` → verificar en BD: `SELECT SourceId FROM Transactions ORDER BY CreatedAt DESC` → debe ser `NULL`, no `00000000-0000-0000-0000-000000000000`.
