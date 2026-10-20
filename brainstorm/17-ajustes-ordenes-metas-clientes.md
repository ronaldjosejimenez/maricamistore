---
name: ajustes-ordenes-metas-clientes
description: Eliminar el estado Delivered de las órdenes, renombrar "Enviar", vista de ítem en solo lectura, % de Comisión en Metas y buscador funcional en Clientes.
metadata:
  type: project
---

# Brainstorm: Ajustes en Órdenes, Metas y Clientes

**Date:** 2026-10-19
**Status:** implemented (specs/017-ajustes-ordenes-metas-clientes)

## Problem Framing

Tres ajustes independientes pedidos en un mismo lote:

1. **Órdenes.** El estado `Delivering` se refiere al negocio, no a los paquetes del proveedor: la orden entra en Delivering cuando llega el primer paquete y empieza la entrega a nuestros clientes, y termina cuando todos los clientes recibieron lo suyo. El botón de transición dice "Enviar", lo que da a entender lo contrario. Al revisarlo apareció una duda: ¿tiene sentido `Delivered`? Hoy `Delivered` y `Completed` no tienen efectos secundarios (no generan transacciones ni entradas de CxP). Lo único que distingue a Delivered es que la orden todavía se puede anular y todavía se puede marcar recepción. Es un paso extra sin significado propio para el negocio. Además, fuera de Pending los ítems no tienen forma de verse en detalle: solo están "Ver Img" y "Reasignar".
2. **Metas.** El dashboard de Mes Actual muestra Real, % de cumplimiento y % al día de hoy, pero no la comisión que corresponde a ese desempeño.
3. **Clientes.** El grid muestra una fila de filtros por columna (`filtering: true`), pero `loadData` ignora el filtro, así que hoy no filtra nada y no hay forma de buscar un cliente.

## Approaches Considered

### A: Un spec con 3 historias independientes (elegido)
- Pros: un branch y un PR, igual que los specs anteriores de "mejoras" y "quick-fixes". Cada historia se prueba por separado.
- Cons: si la migración de Delivered se complica, frena el PR completo.

### B: Separar la eliminación de Delivered en su propio spec
- Pros: aísla el único cambio que toca datos existentes.
- Cons: dos ciclos de spec, branch y PR para cambios pequeños.

### C: Tres specs
- Pros: máximo aislamiento.
- Cons: demasiado proceso para el tamaño de los cambios.

Decisiones parciales consideradas:
- **Delivered:** mantener y solo renombrar, eliminarlo ahora, o parquearlo. Se eligió eliminarlo ahora.
- **Botón de transición:** "Entregando" (literal) o "Iniciar entrega". Se eligió "Iniciar entrega", consistente con los otros verbos (Activar, Completar, Anular).
- **Ver ítem:** modal en solo lectura conservando "Ver Img", o reemplazándolo. Se conserva "Ver Img".
- **Rangos de comisión:** continuos, redondeo a entero, o rango amarillo más amplio. Se eligieron rangos continuos.
- **Buscador:** caja global, filtros por columna, o ambos. Se eligieron ambos.

## Decision

Enfoque **A**: un solo spec con tres historias independientes, priorizadas como P1 Órdenes, P2 Metas y P3 Clientes.

## Key Requirements

### Historia 1: Órdenes (P1)
- Se **elimina el estado `Delivered`** ("Entregada"). Nuevo flujo: `Pending → Active → Delivering → Completed`; desde Active y Delivering también se puede anular (`Voided`).
- Delivering → Completed se hace cuando todos los clientes recibieron sus paquetes. Completed sigue siendo final y no se puede anular.
- **Migración:** las órdenes que hoy están en `Delivered` pasan a `Completed`. El historial de estados (`OrderStatusHistory`) se conserva tal cual, y los registros históricos "Delivered" deben seguir mostrándose de forma legible.
- Las reglas que hoy aceptan Delivered se ajustan: la marcación de recepción solo se permite en Delivering, y la reasignación queda en Active, Delivering y Completed.
- Delivered desaparece del filtro de estados de la pantalla de órdenes.
- El botón de transición a Delivering pasa de "Enviar" a **"Iniciar entrega"**. El estado se sigue mostrando como "Entregando".
- **Ítems:** en Pending se mantiene "Editar". En cualquier otro estado, cada ítem tiene un botón **"Ver"** que abre el mismo modal del ítem con todos los campos en solo lectura y sin botón Guardar. "Ver Img" y "Reasignar" se mantienen como están.

### Historia 2: % de Comisión en Metas (P2)
- Nueva caja **"% de Comisión"** en el dashboard de Mes Actual, junto a Real, % de cumplimiento y % al día de hoy.
- Se calcula a partir del **"% de cumplimiento"** del mes con rangos continuos (corregido el 2026-10-20; originalmente decía "% al día de hoy"):
  | % de cumplimiento | Comisión | Estilo |
  |---|---|---|
  | < 85 | 0% | Rojo |
  | 85 ≤ x < 86 | 2.5% | Amarillo |
  | 86 ≤ x ≤ 110 | 5% | Verde |
  | > 110 | 6% | Verde con marco resaltado |
- Al lado del porcentaje, entre paréntesis y en letra más pequeña: el **monto de comisión = Real × % de comisión**, con el signo de moneda de la meta.
- También se muestra en **Histórico**, con el mismo cálculo sobre el % de cumplimiento de cada mes.

### Historia 3: Buscador de Clientes (P3)
- **Caja de búsqueda global** arriba del grid: busca el texto en Apodo, Nombre Completo, Teléfono, Email y Dirección a la vez.
- **Filtros por columna funcionales:** la fila de filtros existente filtra de verdad, y los filtros se combinan entre sí y con la búsqueda global.
- La búsqueda global y los filtros no distinguen mayúsculas.
- Insertar, editar y eliminar siguen funcionando con los filtros activos.

### Fuera de alcance
- Cambios a los estados a nivel de ítem (`OrderItemStatus`, que tiene su propio "Delivered").
- Cambios a CxP, incluido el tipo `AutoDelivered`, que hoy ninguna transición genera.

## Open Questions
- ¿La búsqueda de clientes debe ignorar tildes (p. ej. "jose" encuentra "José")?
- ¿Cuántas órdenes hay hoy en estado Delivered? Validarlo antes de migrar, y confirmar que pasarlas a Completed (ya no anulables) es aceptable para todas.
- En Mes Actual, si se consulta un mes que no es el actual, ¿la comisión usa el % de cumplimiento (igual que Histórico)?
- ¿El tipo CxP `AutoDelivered` (sin uso) debe limpiarse en un requerimiento futuro?

## Resolution (2026-10-20)
- Búsqueda de clientes: ignora tildes y mayúsculas; global + filtros por columna combinados (AND), mientras se escribe.
- Comisión: se calcula **siempre sobre el % de cumplimiento** del mes, en Mes Actual y en Histórico (corrección del negocio posterior a la implementación inicial, que usaba "% al día de hoy").
- Estado Delivered eliminado; migración `RemoveDeliveredOrderStatus` (Delivered → Completed + registro de historial). Debe aplicarse a producción manualmente antes o junto con el código.
- Review: corregido XSS almacenado en la tabla de historial de estados; `OrderStatus.LabelFor` evita errores si quedan órdenes sin migrar.
- Quedan fuera: `OrderItemStatus.Delivered` y el tipo CxP `AutoDelivered` (sin uso).
