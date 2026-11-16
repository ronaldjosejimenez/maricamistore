# Review Guide: Indicadores en el registro de pago y estado Recibido de los ítems

**Generated**: 2026-11-15 | **Spec**: [spec.md](spec.md) | **Issues**: #44, #48

## Why This Change

En "Saldos de clientes" (Pagos), todos los deudores muestran la misma etiqueta, "Saldo pendiente", que no dice nada nuevo. El usuario necesita ver de un vistazo **por qué** debe cada cliente: si tiene mercadería en camino, si conviene pedirle un adelanto o si ya recibió todo y solo falta cobrar. Para eso hay que saber qué ítems se recibieron. Hoy eso es un booleano aparte (`IsReceived`) junto al `Status` del ítem que agregó la 020, y el issue #44 pide unificarlo en un estado `Received`.

## What Changes

- **Estado Recibido.** Los ítems ganan el estado **Recibido**, que reemplaza por completo a `IsReceived`. El checkbox "Recibido" se ve y funciona igual, pero ahora cambia el estado del ítem.
- **Los Recibidos siguen contando.** Un ítem Recibido cuenta en la orden igual que uno Activo: totales, ganancia, conteo, Reasignar y, sobre todo, la anulación de la orden completa, que debe dejar de cobrarlo.
- **Etiquetas.** En Saldos de clientes desaparece "Saldo pendiente" y aparecen "Productos en camino (n)" (azul), "Sugerir adelanto" (amarillo) y "Entregado – por cobrar" (rojo). El saldo **no** cambia.
- **Cambio de esquema.** Dos migraciones (datos y eliminación de la columna `IsReceived`) que hay que ejecutar antes del deploy.

## How It Works

- **Modelo.** `OrderItemStatus` agrega `Received` ("Recibido"). Se elimina `OrderItem.IsReceived` del modelo y de la configuración EF.
- **Migraciones.**
  - `MigrateIsReceivedToStatus` es solo SQL y se crea antes de quitar la propiedad. Pasa `IsReceived = 1` a `Status = 'Received'`, excepto en los Voided.
  - `DropOrderItemIsReceived` hace el `DropColumn`.
  - Las dos son reversibles: el Down repuebla `IsReceived` a partir de `Received`.
- **Regla "no anulado" (#48).** En `OrderService`, todas las condiciones `Status == Active` que significan "cuenta en la orden" pasan a `OrderItemStatus.CountedKeys.Contains(Status)` (Active y Received): conteo de ítems, `ComputeOrderTotals`, anulación de la orden completa, Reasignar y la vista previa y ejecución de la anulación parcial.
- **Checkbox.** `ToggleIsReceivedAsync` pasa a ser `SetItemReceivedAsync`, con los mismos guards y mensajes, y asigna `Status = Received | Active`. El handler `ToggleReceived` y su JSON no cambian. `items.js` lee `status === 'Received'`.
- **Etiquetas.** `PaymentService.GetSaldosReportAsync` deja intacto el cálculo del saldo y **después**:
  1. toma los deudores (`Balance > 0`);
  2. hace una consulta agrupada sobre sus ítems no anulados en órdenes Active o Delivering, con el mismo alcance de organización que el saldo (`IgnoreQueryFilters` + `organizationId`);
  3. deriva `ItemsInTransit`, `SuggestAdvance` y `DeliveredToCollect` en `SaldoReportRow`.

  `renderSaldos` solo pinta los badges.

## When It Applies

**Applies when**:
- Se marca o desmarca "Recibido" en una orden Entregando.
- Se calculan totales o conteos de una orden, o se anula una orden que tiene ítems recibidos.
- Se carga Saldos de clientes, para los clientes con saldo pendiente positivo que tienen ítems en órdenes Activas o Entregando.

**Does not apply when**:
- "Productos en camino" y "Sugerir adelanto" solo miran órdenes Activas o Entregando. Un saldo que viene solo de órdenes Completadas o de transacciones manuales muestra "Entregado – por cobrar".
- El cliente tiene saldo a favor: solo se muestra "Crédito a favor", como hoy.
- Se calcula el saldo en sí, que no cambia.

## Key Decisions

1. **Todo el #48 entra en este feature.** Se consideró hacer solo los 3 hallazgos críticos, pero partirlo dejaba el código en un estado inconsistente. La mayoría son cambios de una línea.
2. **Lista positiva `CountedKeys` en lugar de `!= Voided`.** Una definición en negativo contaría por omisión cualquier estado futuro; con la lista positiva hay que decidir a propósito si el nuevo estado cuenta. EF la traduce a `IN (...)`. Se mantiene en un solo lugar para no repetirla en 6 consultas.
3. **Dos migraciones reversibles.** El issue pide separar datos y esquema. Con ese orden, el rollback también recupera `IsReceived`.
4. **El contrato del checkbox no cambia.** `isReceived` pasa a significar "estado deseado". Así no hay cambios visibles y la operación es idempotente.
5. **Las reglas de las etiquetas van en el servidor y después del saldo.** La alternativa era calcularlas en JS, lo que habría llevado ítems al navegador y reglas de negocio a la UI. FR-012 exige no tocar el cálculo del saldo.
6. **"Entregado – por cobrar" es la regla literal del issue: saldo positivo y ningún ítem Activo en órdenes en curso.** No exige ítems Recibidos, así que cubre a los clientes con órdenes Completadas o con saldo manual (caso real detectado después del primer PR). "Sugerir adelanto" exige tener ítems Activos, por lo que nunca coincide con ella.

## Areas Needing Attention

- **Orden de las migraciones y del deploy.** `MigrateIsReceivedToStatus` tiene que generarse con `IsReceived` todavía en el modelo. Revisar que el snapshot y el Designer no tengan cambios inesperados. En producción, ejecutar el script idempotente antes de desplegar.
- **Anulación de la orden completa desde Entregando.** Es el cambio con impacto en dinero. Verificar que los ítems Recibidos generan su `Void` (quickstart §2.3).
- **"Sugerir adelanto" suma Activos y Recibidos.** Así lo dice la regla literal del issue. Se compara contra el saldo total del cliente, que puede incluir deudas de órdenes Completadas o manuales.
- **Alcance de organización de las etiquetas.** Replica el del saldo, tanto con "Ver saldo completo" como sin él. Revisar el caso h del quickstart.
- **Color azul.** Se usa `badge-primary`, porque `badge-info` en Bootstrap 4 es cian.

## Open Questions

No open questions identified.

## Review Checklist

- [ ] Key decisions are justified
- [ ] Breaking changes are documented with migration guidance
- [ ] Scope matches the stated boundaries
- [ ] Success criteria are achievable
- [ ] No unstated assumptions
- [ ] Ningún uso de `Status == Active` con sentido de "no anulado" quedó sin cambiar
- [ ] El cálculo del saldo en `GetSaldosReportAsync` no cambió

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
