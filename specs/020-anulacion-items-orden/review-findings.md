# Deep Review Findings

**Date:** 2026-11-15
**Branch:** 020-anulacion-items-orden
**Rounds:** 1 (más una re-revisión acotada, sin hallazgos)
**Gate Outcome:** PASS
**Invocation:** manual

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 6 | 6 | 0 |
| Minor | 7 | 4 | 3 |
| **Total** | **13** | **10** | **3** |

**Alcance:** 13 archivos de código de la 020 (diff contra el punto de partida del branch en `develop`; el diff contra `main` arrastraba las features 016–019).
**Agentes completados:** 5/5, más 1 de re-revisión sobre la ronda de correcciones. CodeRabbit y Copilot: omitidos (CLI no instalado). Suite de pruebas: omitida (el proyecto no tiene pruebas automatizadas ni comando detectable).

| Agente | Encontrados | Corregidos | Pendientes |
|---|---|---|---|
| Correctitud | 0 | 0 | 0 |
| Arquitectura e idioms | 6 | 4 | 2 |
| Seguridad | 1 | 1 | 0 |
| Producción | 2 | 2 | 0 |
| Calidad de pruebas | 5 | 4 | 1 |
| Re-revisión de la ronda 1 | 0 | 0 | 0 |

Un hallazgo (tope al monto a devolver) lo reportaron a la vez Seguridad y Producción; se cuenta una vez.

## Findings

### Importantes (todos corregidos en la ronda 1)

**F-1. La anulación no deja log** (Producción, `OrderService.cs`). Es una operación financiera y `ILogger` ya estaba inyectado. *Resolución:* un `LogInformation` después del guardado, con la orden, los ítems, el monto devuelto, la moneda y el período.

**F-2. La transacción de anulación duplicaba `BuildTransaction`** (Arquitectura, `OrderService.cs`). FR-013 exige que sea igual a la de la anulación de la orden completa, y dos copias del armado podían divergir. *Resolución:* `BuildTransaction` acepta una descripción opcional y `VoidItemsAsync` lo reutiliza.

**F-3. El signo de CxP dependía de que cada consumidor aplicara el helper** (Arquitectura, `CxPService.cs`, `CxP/Index.cshtml.cs`). Un total nuevo con `Sum(e => e.Amount)` habría sumado la anulación en vez de restarla. *Resolución:* el signo se aplica en un solo lugar por camino de lectura: `GetPeriodIndicatorsAsync` (indicadores, vista previa y cierre) y `GetEntriesByPeriodAsync` (el DTO ya sale con signo). La página dejó de aplicarlo. Se documentó junto a `ReversoPaquete` que ese sí se guarda negativo.

**F-4. Quickstart 5.1 no se podía ejecutar** (Calidad de pruebas). Cerrar un período desde la app crea el del mes siguiente, así que nunca falta un período abierto. *Resolución:* se cambió por un `UPDATE` de prueba sobre `PeriodControls`, con el aviso de revertirlo.

**F-5. Los escenarios A, B y C no eran comparables** (Calidad de pruebas). Los indicadores de CxP son globales al período. *Resolución:* se verifican por diferencias contra indicadores anotados justo antes, con una tabla de deltas. Recalculando a mano, el agente traía un error en el escenario B (Posición −1 600); el valor correcto es **−5 000**.

**F-6. Al quickstart le faltaban pasos para casos del spec** (Calidad de pruebas). *Resolución:* se agregaron monto vacío y cero, justificación de 1001, estado cambiado con el modal abierto, ítem reasignado, cobros originales intactos, vista previa del cierre, moneda y descripción de la transacción, y las dos consultas de SC-005.

### Menores corregidos

- **F-7. Monto a devolver sin tope y handlers sin guarda de solicitud nula** (Seguridad y Producción). Un monto enorme producía un 500 con texto crudo en el modal. *Resolución:* tope de 1 000 000 000 por línea, validado antes de modificar nada; guarda de solicitud nula en ambos handlers; el modal muestra un mensaje genérico si el servidor devuelve HTML. El tope es fijo y no depende del precio del ítem, porque el spec admite que el proveedor devuelva más.
- **F-8. La guarda de ítem anulado en `Update` estaba después de procesar la imagen** (Arquitectura). *Resolución:* se movió justo después de la comprobación de existencia.
- **F-9. `ComputeOrderTotals` dependía de que el llamador filtrara los activos** (Arquitectura). *Resolución:* filtra por sí misma, y su documentación avisa del espejo en `recalcOrderHeader` (items.js).
- **F-10. Ambigüedades del quickstart sobre qué ve el usuario** (Calidad de pruebas). *Resolución:* se describió el estado de los botones en modo selección y el conjunto exacto de botones de una fila anulada.

### Menores pendientes (aceptados)

- **F-11. Helper de truncado duplicado y constantes de longitud fijas** (Arquitectura). `TruncateText` repite `OrderPackageService.Truncate`, y el 500 y el 100 no están ligados a la configuración de las entidades. Es duplicación que ya existía entre servicios; consolidarla toca código de otras features.
- **F-12. Preview y Void repiten la carga y validación de la selección** (Arquitectura). Un `LoadVoidSelectionAsync` compartido evitaría que diverjan. El servidor revalida todo al confirmar, así que una divergencia no deja pasar nada indebido; como mucho el modal habilitaría un botón que el servidor rechaza.
- **F-13. Proyecto de pruebas opcional** (Calidad de pruebas). Un xUnit mínimo para `ComputeOrderTotals` y `SignedAmount` sería barato, pero no es la convención del proyecto ni lo pide el spec. Conviene si se va a seguir tocando la fórmula de totales.

### Observaciones bajo el umbral (no son hallazgos)

El agente de correctitud no reportó nada con confianza ≥ 70, pero anotó tres cosas: un doble envío realmente concurrente (sin control de concurrencia, aceptado por diseño; el botón se deshabilita al hacer clic), la selección que se pierde si la tabla se vuelve a dibujar en modo selección (por ejemplo, tras Reasignar), y que el formato del monto en la nota del historial depende de la cultura del servidor.

## Post-Fix Spec Coverage

La ronda 1 eliminó algo de código (la lectura de configuración y el armado manual de la transacción dentro de `VoidItemsAsync`). Se verificó que los requisitos afectados siguen cubiertos:

| Requisito | Implementación | Estado |
|---|---|---|
| FR-013: `Void` por ítem, cliente actual, precio acordado, moneda local, hora de Costa Rica | `BuildTransaction` vía `VoidItemsAsync` | ✓ |
| FR-011: validaciones y mensajes | `VoidItemsAsync` (justificación, ≤1000, monto ≥ 0 y tope, todos seleccionados, período) | ✓ |
| FR-016: la CxP resta el tipo nuevo | `SignedAmount` en `CxPService.cs:234` y `:332` | ✓ |
| FR-018: un solo guardado | un único `SaveChangesAsync` | ✓ |

Todos los requisitos del spec verificados tras la ronda de correcciones.

## Test Suite Results

No hay comando de pruebas detectable: el proyecto no tiene pruebas automatizadas. Tras las correcciones, `dotnet build` termina con 0 errores y los mismos 2 warnings previos (ambos en `SeedWork/Enumeration.cs`), y el JS pasa `node --check`.

## Remaining Findings

Ninguno Crítico ni Importante. Quedan los 3 Menores aceptados de arriba. La verificación manual del quickstart (tarea T024) sigue pendiente.
