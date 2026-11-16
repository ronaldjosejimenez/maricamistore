# Brainstorm: Indicadores en el registro de pago + estado Received de ítems

**Date:** 2026-11-15
**Status:** spec-created (specs/021-indicadores-registro-pago)

## Problem Framing
En "Saldos de clientes" (pantalla Pagos) la etiqueta "Saldo pendiente" no aporta: todos los clientes listados con saldo positivo la tienen. Se quiere ver de un vistazo **por qué** debe el cliente: si tiene mercadería en camino, si convendría pedirle un adelanto (guía comercial 50% al ordenar / 50% al recibir) o si ya recibió todo y solo falta cobrar.

Para eso hay que saber, por ítem, si ya se recibió. Hoy existe el booleano `OrderItem.IsReceived` junto al campo `Status` (agregado en la 020, valores `Active` y `Voided`). El issue pide que un nuevo valor de estado `Received` reemplace por completo a `IsReceived`.

Fuentes:
- Issue #44: https://github.com/ronaldjosejimenez/maricamistore/issues/44
- Issue #48 (hallazgos de impacto, creado en esta sesión): https://github.com/ronaldjosejimenez/maricamistore/issues/48

### Hallazgos de código relevantes
- Se verificó que la 020 está mergeada completa en `develop`. Su spec **conservaba** `IsReceived` a propósito: no estaba en su alcance eliminarlo, así que el refactor es parte de este requerimiento.
- Hoy el código usa `Status == Active` con el significado **"ítem no anulado"**. Con `Received`, esa condición excluiría los ítems recibidos. El análisis completo está en el #48. Lo crítico:
  - Anular una orden `Delivering` no generaría el `Void` de los ítems recibidos, así que **el cliente quedaría cobrado de más**.
  - `ComputeOrderTotals` y Reasignar recalcularían los totales y la ganancia sin los ítems recibidos.
- El saldo de "Saldos de clientes" se calcula en `PaymentService.GetSaldosReportAsync` con la fórmula Charge − Payment − Void. No cambia.

## Approaches Considered

### A: Todo el #44 + todo el #48 en un solo feature (ELEGIDA)
- Pros: el refactor queda consistente de una sola vez y el #48 se cierra con el feature. Los arreglos son chicos (casi todos son cambiar `== Active` por `!= Voided`).
- Cons: el feature es algo más grande que solo las etiquetas.

### B: #44 + solo los hallazgos críticos 1–3 del #48
- Pros: el feature es más chico.
- Cons: deja el código en un estado inconsistente (por ejemplo, el conteo de ítems en el listado de órdenes `Delivering` saldría mal) hasta que se haga el resto del #48.

### C: Primero las etiquetas con `IsReceived` y el refactor después
- Descartada: el requerimiento pide que las reglas usen el estado del ítem y que `IsReceived` desaparezca.

## Decision
**Enfoque A.** El #48 se resuelve y se cierra con este feature; el PR debe decir "Closes #44, closes #48". Las reglas hacen referencia **solo a `Status`**. `IsReceived` solo aparece en la migración de datos, como origen de la conversión, y deja de existir después del feature.

## Key Requirements

### Estado del ítem
- Nuevo valor `Received` ("Recibido") en `OrderItemStatus`. Hay que incluirlo en `List()` y `FromKey`.
- El checkbox de Orders/Items cambia el `Status` entre `Active` (desmarcado) y `Received` (marcado). Se mantiene la UI y la regla actual: solo en órdenes `Delivering` y nunca sobre ítems `Voided`.
- El grid pinta la fila en verde y marca el checkbox según `status === 'Received'`.

### Migraciones (EF Core)
1. **Datos:** los ítems con `IsReceived = true` que no están `Voided` pasan a `Status = Received`. Incluye los de órdenes `Completed`.
2. **Esquema:** se elimina la columna `IsReceived`.

### Consistencia (los hallazgos del #48)
- Regla general: un ítem cuenta en la orden mientras no esté anulado (`Status != Voided`). Se compara contra `Active` solo cuando significa "pendiente de recibir".
- Lugares a corregir:
  - la anulación de la orden completa (Void de **todos** los no anulados);
  - `ComputeOrderTotals`;
  - Reasignar;
  - el conteo de ítems del listado de órdenes;
  - la anulación parcial (protección).
- Los criterios de aceptación del #48 aplican.

### Etiquetas en "Saldos de clientes" (no son exclusivas)
- Se **quita** "Saldo pendiente". "Crédito a favor" se mantiene.
- **"Productos en camino"**: el cliente tiene al menos un ítem con `Status = Active` en órdenes `Active` o `Delivering`.
- **"Sugerir adelanto"**: la suma del precio acordado (`AgreedPriceInLocal`) de los ítems no anulados del cliente en órdenes `Active` o `Delivering` es menor o igual a su saldo pendiente (el cliente aún no ha aportado nada). Corregido después del primer PR: la regla original estaba invertida.
- **"Entregado – por cobrar"**: el cliente tiene saldo pendiente y ningún ítem `Active` en órdenes `Active` o `Delivering` (regla literal del issue #44). No exige ítems `Received`: un saldo que viene de órdenes `Completed` o de transacciones manuales también la muestra. Ajustado después del primer PR, a partir de un caso real (cliente con todas sus órdenes `Completed`).

### Pipeline
- **El cálculo del saldo no cambia.** Las etiquetas se calculan **después**, reutilizando el saldo ya calculado y solo para los clientes que salen en la lista.

## Open Questions
- ¿Las etiquetas aparecen también en clientes con crédito a favor (saldo negativo)? Propuesta: solo con saldo positivo.
- ¿Las etiquetas respetan el filtro de organización y el checkbox "Ver saldo completo"? Es decir, ¿solo cuentan las órdenes de la organización filtrada?
- ¿Qué pasa con los clientes genéricos (especulativos)?
