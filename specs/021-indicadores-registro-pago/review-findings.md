# Deep Review Findings

**Date:** 2026-11-15
**Branch:** 021-indicadores-registro-pago
**Rounds:** 1
**Gate Outcome:** PASS
**Invocation:** manual (desde el hook after_implement)

> Revisión hecha en la misma sesión, sin subagentes ni herramientas externas (`--no-external`). Las cinco perspectivas (correctitud, arquitectura, seguridad, producción y calidad de pruebas) se aplicaron sobre el diff completo de código contra `develop`. Es menos aislada que la revisión con agentes en contexto limpio.

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 1 | 1 | 0 |
| Minor | 3 | - | 3 |
| **Total** | **4** | **1** | **3** |

## Findings

### FINDING-1
- **Severity:** Important
- **Confidence:** 85
- **File:** `MariCamiStore/Infrastructure/Persistance/Migrations/20261116044635_DropOrderItemIsReceived.cs` (`Down`)
- **Category:** production-readiness
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:** el `Down` de `DropOrderItemIsReceived` recreaba la columna `IsReceived` con valor 0 para todos los ítems.

**Why this matters:** si solo se revierte esa migración (por ejemplo, para volver a la versión anterior del código), los ítems que ya estaban en `Received` aparecen como no recibidos para el código viejo, y se pierde qué ítems llegaron. El `Down` de la migración de datos no lo corrige, porque solo se ejecuta si se revierten las dos.

**How it was resolved:** el `Down` ahora repuebla `IsReceived = 1` donde `Status = 'Received'`. Se probó: al revertir solo esa migración el ítem recibido queda con `IsReceived = 1` y `Status = 'Received'`, y al reaplicar la columna desaparece de nuevo. El script idempotente de producción se regeneró.

### FINDING-2
- **Severity:** Minor
- **Confidence:** 70
- **File:** `MariCamiStore/wwwroot/js/pages/orders/items.js:11`
- **Category:** architecture
- **Resolution:** remaining (aceptado)

**What is wrong:** `COUNTED_STATUSES` en el JS repite la lista `OrderItemStatus.CountedKeys` del servidor. Si se agrega un estado nuevo hay que acordarse de editar los dos lugares.

**Why this matters:** hay riesgo de que se desincronicen. Ya hay un precedente igual en el proyecto (`recalcOrderHeader` replica `ComputeOrderTotals`), y el comentario del código deja la relación explícita.

### FINDING-3
- **Severity:** Minor
- **Confidence:** 65
- **File:** `specs/021-indicadores-registro-pago/migration.sql`
- **Category:** production-readiness
- **Resolution:** remaining (aceptado)

**What is wrong:** entre ejecutar el script y terminar el despliegue, las instancias con el código anterior fallan al leer la columna `IsReceived`, que ya no existe.

**Why this matters:** es una ventana corta de errores durante el despliegue. Es el mismo patrón que en la 020 (el pipeline no aplica migraciones). Para evitarla del todo habría que desplegar en dos pasos, lo que es desproporcionado para este proyecto.

### FINDING-4
- **Severity:** Minor
- **Confidence:** 90
- **File:** `specs/021-indicadores-registro-pago/tasks.md` (T014, T018, T022)
- **Category:** test-quality
- **Resolution:** remaining (pendiente de verificación manual)

**What is wrong:** el proyecto no tiene pruebas automatizadas. La verificación fue manual, a nivel de endpoints, y no cubrió Reasignar con un ítem Recibido, el aspecto visual de los colores de las etiquetas ni el tiempo de carga (SC-006).

**Why this matters:** quedan sin ejecutar T014, T018 y T022 del quickstart.

## Verificación realizada

- Línea base antes y después de migrar: totales de las 8 órdenes, conteos de ítems y saldos idénticos. Solo cambió el estado del ítem recibido (Active → Received).
- Anular una orden Entregando con un ítem Recibido (sobre una copia de la base): se generó el `Void` del ítem y el saldo bajó exactamente su monto. El conteo del listado de órdenes no cambió al marcar el ítem como Recibido.
- Etiquetas: cinco casos con datos temporales (suma igual al saldo, un peso menos, mezcla de Activos y Recibidos, todo recibido y sin ítems en curso). Todos dieron lo esperado, y los datos se restauraron.
- La consulta de etiquetas se traduce a SQL con `IN (N'Active', N'Received')` y `IN (N'Active', N'Delivering')`.
- Rollback de las migraciones, completo y parcial.

## Post-Fix Spec Coverage

El fix de FINDING-1 solo agregó una línea al `Down` de una migración. No se eliminó código y no se tocó ningún requisito. Los 18 FR siguen cubiertos (ver el reporte de compliance de esta revisión).
