# Review Guide: Anulación parcial de ítems de una orden activa

**Generated**: 2026-11-15 | **Spec**: [spec.md](spec.md) | **Issue**: #43

## Why This Change

Hoy, si un cliente desiste de un producto en una orden ya Activa (el pedido ya está hecho con el proveedor), la única salida es anular la orden completa y rehacerla. Además, el proveedor suele devolver un monto distinto al que costó el ítem, y no hay forma de registrar esa diferencia sin descuadrar la CxP, los saldos de clientes y la Posición.

## What Changes

En una orden Activa, el usuario puede seleccionar ítems y anularlos. Para cada uno indica cuánto devuelve el proveedor y escribe una justificación obligatoria. Antes de confirmar ve cómo cambian los totales de la orden. Al confirmar:
- los ítems quedan Anulados (no se borran);
- al cliente se le deja de cobrar cada ítem;
- los totales de la orden se recalculan sin ellos;
- la CxP se reduce exactamente en lo que devuelve el proveedor;
- queda una entrada en el Historial de la orden.

Los ítems anulados se muestran en rojo pastel. **Cambio de esquema**: una columna nueva, `OrderItems.Status`, cuya migración hay que ejecutar antes del deploy. Se eliminan los valores sin uso del enum `OrderItemStatus`.

## How It Works

- **Modelo**: `OrderItem.Status` (Active/Voided, default `'Active'`). En CxP, un tipo nuevo `AnulacionItems` que se guarda **positivo**; `CxPEntryType.SignedAmount()` lo vuelve negativo solo al agregar. Se aplica en `GetPeriodIndicatorsAsync`, de donde lo heredan la vista previa, el cierre y el saldo anterior, y en el listado de CxP.
- **Servicio** (`OrderService`):
  - `ComputeOrderTotals` es una función pura extraída de `ApplyOrderCalculationSuite`, que ahora solo suma los ítems Active.
  - `PreviewVoidItemsAsync` arma el Antes (los totales guardados) y el Después (el recálculo).
  - `VoidItemsAsync` valida y luego agrega en un solo `SaveChangesAsync` el estado de los ítems, un `Void` por ítem (moneda local, fecha CR), los totales, la entrada CxP y el historial.
- **Exclusiones**: los Voided quedan fuera del conteo, de Reasignar, del check de Recibido, de la edición, de la anulación de la orden completa y de los subtotales de la tabla.
- **UI** (`Items.cshtml` / `items.js`): toolbar con "Anular", un modo selección con checkboxes, y un modal con montos editables, justificación, comparativa Antes/Después (vista previa del servidor) y ajuste/teórico/diferencia en vivo. Al confirmar se recarga la página.

## When It Applies

**Applies when**:
- La orden está en estado Activa y quedaría al menos un ítem Activo después de anular.
- Hay un período CxP abierto.

**Does not apply when**:
- La orden está Pendiente (ahí los ítems se eliminan), Entregando, Completada o Anulada.
- Se seleccionan todos los ítems que quedan: se usa la anulación de la orden.
- Fuera de alcance: migrar `ReversoPaquete` (hoy negativo) al esquema de tipo que resta, y revertir `AutoActiva` al anular la orden completa.

## Key Decisions

1. **El ajuste se mide contra Total a Proveedor, no contra Total Orden** (el ticket decía lo segundo). La CxP se registró con Total a Proveedor; Total Orden incluye el envío a CR, que se lleva aparte y se corrige solo al recalcular. El default por ítem es `ListPriceTaxWithTax`, no `RealPrice`.
2. **No hay montos negativos**. El `Void` ya resta por tipo; en CxP se crea un tipo nuevo que resta, en vez de guardar un negativo (pedido explícito del usuario).
3. **Un solo paso, atómico**: varios `Void` y **una** entrada CxP. Alternativas descartadas: registrar la devolución en dos pasos (deja un estado intermedio y la Posición descuadrada) y anular ítem por ítem (sin comparativa y con varias entradas CxP).
4. **Estado del ítem con solo Active/Voided**. El ciclo de vida ya lo lleva la orden; un estado Pending en el ítem solo exigiría sincronizarlo.
5. **Se bloquea si no hay período CxP abierto** (a diferencia de AutoActiva, que se omite con un warning). Prioriza la consistencia contable.
6. **La vista previa la calcula el servidor**: la misma función calcula lo que se muestra y lo que se guarda (FR-014).

## Areas Needing Attention

- **Órdenes anteriores a la 018**: el Antes son los totales guardados y el Después se recalcula con las reglas actuales. En esas órdenes la diferencia teórica puede incluir efectos ajenos a los ítems anulados. Es un riesgo aceptado: lo que va a CxP es el monto que ingresa el usuario.
- **El movimiento `AnulacionItems` se puede eliminar desde CxP** (Clarifications). Si se elimina, los ítems y los cobros siguen anulados y la CxP queda descuadrada hasta que alguien la corrija a mano.
- **Inconsistencia de signo**: `ReversoPaquete` sigue guardándose negativo y `AnulacionItems` positivo. Conviven dos convenciones hasta que se migre la primera.
- **Concurrencia**: no hay rowversion. Se asume un solo operador (gana el último que guarda); una solicitud repetida falla porque los ítems ya están Voided.
- **Atomicidad vs. patrón actual**: la entrada CxP se agrega al contexto directamente (como `OrderPackageService`), no con `CreateAutoEntryAsync`, que guarda por su cuenta.

## Open Questions

- ¿Migrar `ReversoPaquete` a monto positivo con tipo que resta?
- ¿Alinear la anulación de la orden completa para que revierta `AutoActiva` con el nuevo esquema?

## Review Checklist

- [ ] Key decisions are justified
- [ ] Breaking changes are documented with migration guidance (script idempotente de `AddOrderItemStatus` antes del deploy)
- [ ] Scope matches the stated boundaries
- [ ] Success criteria are achievable
- [ ] No unstated assumptions
- [ ] Los escenarios A/B/C del quickstart cuadran con la fórmula de Posición (`saldos + enCuenta − deuda − shipping`)

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
