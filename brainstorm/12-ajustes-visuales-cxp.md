---
name: ajustes-visuales-cxp
description: Ajustes visuales del dashboard de Cuentas por Pagar (CxP) - renombres, total colonizado junto a Entradas del Período y reorganización en 3 columnas con resaltado de Deuda a Pagar y Pendiente de Recoger.
metadata:
  type: project
---

# Brainstorm: Ajustes Visuales de la Pantalla CxP

**Date:** 2026-09-28
**Status:** spec-created
**Spec:** specs/012-ajustes-visuales-cxp

## Problem Framing

El panel "Control del Mes" de Cuentas por Pagar (brainstorm #09 / spec 009) muestra sus indicadores en filas uniformes. Esto genera confusión y no deja claro qué es lo importante:

- Hay dos recuadros "Por pagar en Colones". El de la primera fila es el **total de todas las entradas convertido a colones**. El otro es el recuadro dinámico por moneda, que solo suma las entradas en colones. Además, el total coincide con "Deuda a Pagar" cuando Pagos Realizados = 0.
- "Saldos por Cobrar" no dice que se trata de clientes.
- Los valores clave para operar (Deuda a Pagar y Pendiente de Recoger) no se distinguen del resto.
- La lista "Entradas del Período" no muestra su total.

Solo son cambios de presentación: no cambian los cálculos (`CxPService.GetPeriodIndicatorsAsync`) ni los datos.

## Approaches Considered

La decisión principal fue cómo resaltar la columna 1 (Deuda a Pagar y Pendiente de Recoger).

### A: Tarjeta grande (AdminLTE "small-box")
- Pros: máxima visibilidad, número grande, patrón típico de dashboard.
- Cons: tamaño distinto al resto de recuadros; ocupa más alto.

### B: Recuadro con color (AdminLTE "info-box" con fondo de color) — elegido
- Pros: mismo tamaño y estructura que los demás recuadros (consistente), resaltado claro por color, ícono y número en negrita.
- Cons: menos impacto que la tarjeta grande.

### C: Solo número más grande
- Pros: cambio mínimo.
- Cons: resalta poco; no cumple el objetivo de destacar.

## Decision

Enfoque **B**. Deuda a Pagar va en **rojo** (`bg-danger`) y Pendiente de Recoger en **amarillo** (`bg-warning`), ambos con ícono y número en negrita. El total junto a "Entradas del Período" se muestra como **texto en la misma línea del título**.

## Key Requirements

1. **Renombrar** el indicador del panel "Por pagar en Colones" (total de todas las entradas convertido a colones con el Tipo de Cambio) a **"Total por pagar colonizado"**. Los recuadros dinámicos "Por pagar en {Moneda}" (uno por moneda, sin conversión) **no cambian**.
2. **Renombrar** "Saldos por Cobrar" a **"Saldos por Cobrar a Clientes"**.
3. **Total junto a Entradas:** sin quitarlo del panel, mostrar además el valor de "Total por pagar colonizado" junto al título "Entradas del Período", como texto en la misma línea ("Entradas del Período — Total por pagar colonizado: ₡…") y cerca del botón "Agregar entrada". Se actualiza al mismo tiempo que el indicador del panel.
4. **Reorganizar** los indicadores del panel en **3 columnas**, con los recuadros apilados verticalmente en cada columna:
   - **Columna 1 (resaltada):** "Deuda a Pagar" (rojo) y "Pendiente de Recoger" (amarillo), con fondo de color, ícono y número en negrita.
   - **Columna 2:** "Shipping CR Pendientes" y "Saldos por Cobrar a Clientes" (estilo actual).
   - **Columna 3:** "Total por pagar colonizado" y los recuadros dinámicos "Por pagar en {Moneda}" (estilo actual; si hay más monedas, aparecen más recuadros en esta columna).
   - **Responsive:** en pantallas angostas las columnas se apilan.
5. **Sin cambios:** "Posición" (valor grande verde/rojo) se queda donde está; los campos editables (Tipo de Cambio, Pagos Realizados, En Cuenta), el botón Guardar, el aviso de Tipo de Cambio en 0, el cierre de mes y la tabla de entradas siguen igual.

### Fuera de alcance
- Cambios en cálculos o datos de los indicadores.
- Usar el signo de moneda local configurado en lugar del "₡" escrito en el código.
- Cambios en "Posición" o en los campos editables.

## Open Questions
_(todas resueltas en la spec 012)_
- Sin entradas o con Tipo de Cambio 0: el texto junto a "Entradas del Período" muestra el mismo valor que el indicador del panel (₡0,00).
- Íconos: `fa-file-invoice-dollar` (Deuda a Pagar) y `fa-hand-holding-usd` (Pendiente de Recoger).

## Cierre (2026-09-28)
- Implementado en `specs/012-ajustes-visuales-cxp`: PR #13 (→ develop) y PR #14 (→ main), desplegado a producción con éxito.
- Probado manualmente por el usuario antes del merge.
- Decisiones de la spec: el color cubre el recuadro completo, las columnas se apilan por debajo de 768 px y el texto junto a Entradas sigue visible con el período cerrado.
- Observación: entre 768 y 991 px los títulos largos pueden cortarse con "…" (aceptado).
