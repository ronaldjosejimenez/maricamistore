# Brainstorm: Anulación parcial de ítems de una orden activa

**Date:** 2026-11-15
**Status:** active

## Problem Framing
Hoy solo se puede anular la orden completa. Se necesita anular ítems específicos de una orden `Active` (el pedido ya fue enviado al proveedor), dejando la orden como si esos ítems nunca hubieran existido, pero reflejando lo que el proveedor realmente devuelve (que puede ser menos, o raramente más, que lo que costó el ítem). La diferencia nunca se le cobra al cliente. Saldos, CxP y Payments deben quedar contablemente consistentes.

Fuente: issue #43 (https://github.com/ronaldjosejimenez/maricamistore/issues/43).

### Hallazgos de código relevantes
- `OrderItem` **no tiene columna de estado**. El enum `OrderItemStatus` (Pending, Active, Checked, Delivered, Voided) existe pero no se usa en ningún lado.
- Al activar la orden se crea un `Charge` por ítem (`AgreedPriceInLocal`) y una entrada CxP `AutoActiva` por `TotalToPayToSupplier`. Al anular la orden completa se crea un `Void` por ítem; el `Void` (monto positivo) ya resta en Saldos.
- `TotalToPayToSupplier` **no incluye** el envío a CR. El envío CR se maneja aparte (`ShippingAmountToCR` − paquetes = "Shipping CR pendiente") y entra separado en la Posición.
- Por ítem, `RealPrice = ListPriceTaxWithTax + EstimateShipping`: incluye envío CR, así que no sirve como base del monto devuelto por el proveedor.
- La CxP se agrega con `Sum(Amount)` por moneda. `ReversoPaquete` hoy se guarda con monto negativo (precedente no deseado).
- `ApplyOrderCalculationSuite` recalcula los totales de la orden a partir de los ítems; `ShippingAmountIntern` y `DiscountAmount` son de nivel orden.

## Approaches Considered

### A: Anulación parcial en un solo paso (ELEGIDA)
Operación atómica: ítems → `Voided`, un `Void` por ítem al cliente, recálculo de la orden sin los anulados, y **un solo** movimiento CxP de un tipo nuevo que resta.
- Pros: cumple los escenarios A/B/C de una vez y todo queda consistente; reutiliza el mecanismo `Void` existente.
- Cons: se necesita conocer el monto devuelto al anular (si cambia después, se corrige con un movimiento CxP manual).

### B: Anular ahora y registrar la devolución del proveedor en un segundo paso
- Pros: refleja el momento real del reembolso.
- Cons: estado intermedio "pendiente de devolución" que hay que rastrear; la Posición queda descuadrada mientras tanto; más superficie.

### C: Anulación ítem por ítem (como Reasignar)
- Pros: UI más simple.
- Cons: no hay comparativa antes/después del lote, no respeta la UI de checkboxes del ticket y genera varios movimientos CxP.

## Decision
**Enfoque A**, con estas precisiones del usuario:
- Varios ítems por anulación → **varios `Void` al cliente** (uno por ítem) y **un solo movimiento CxP** al final.
- **Nada de montos negativos** en transacciones ni en CxP. El `Void` ya resta por su tipo; para CxP se crea un **tipo de movimiento nuevo que resta**, guardado con monto positivo.
- El ajuste se mide contra **Total a Proveedor**, no contra Total Orden (que incluye envío CR, que se corrige solo al recalcular).

## Key Requirements

### Modelo / limpieza
- Nueva columna de estado en `OrderItem`; el enum `OrderItemStatus` queda **solo con `Active` y `Voided`** (se eliminan Pending, Checked, Delivered). Los ítems existentes quedan en `Active`.
- Nuevo tipo CxP que resta (p. ej. `AnulacionItems`): monto positivo, con `OrderId` y una referencia. **Todas** las sumas de CxP aplican el signo según el tipo: indicadores del panel, preview/cierre de mes, saldo anterior y listado/visualización.
- Sin borrado físico de ítems ni transacciones.

### UI (Orders/Items)
- Toolbar oficial de la pantalla, con un botón rojo **"Anular"** junto a "Volver", visible solo si la orden está `Active`.
- "Anular" habilita una columna de checkboxes en el grid; solo se pueden seleccionar ítems `Active`.
- Modal de confirmación:
  - mensaje "¿Está seguro que desea anular los ítems seleccionados?";
  - lista de ítems con campo editable **"Monto a devolver por el proveedor"**, en la moneda de la orden. Default = **precio de lista con impuesto** (`ListPriceTaxWithTax`, sin envío CR);
  - **justificación obligatoria**;
  - comparativa **Antes vs. Después**: Precio Acordado Total, SubTotal, Impuestos, Envío a CR, Total a Proveedor, Total Orden y Ganancia Est., calculados con las reglas actuales sin los ítems anulados;
  - **Ajuste financiero** = suma de los montos a devolver, mostrado junto a la diferencia teórica (Total a Proveedor antes − después) y la diferencia entre ambos;
  - si la selección cubre **todos** los ítems activos restantes, se bloquean los botones con un aviso de usar la anulación de la orden;
  - botones "Cancelar" y "Continuar con la anulación".
- Al terminar: se refresca el grid y las filas `Voided` se pintan con fondo **rojo pastel claro**.

### Procesamiento (atómico)
- Validar: orden `Active`, ítems `Active` de esa orden, que no sean todos, justificación presente y montos ≥ 0.
- Si **no hay período CxP abierto**, se **bloquea** la anulación con un error y no se cambia nada.
- Ítems → `Voided`.
- Un `Void` por ítem, al cliente actual del ítem, por su `AgreedPriceInLocal`, con la justificación en la descripción.
- Recalcular y persistir los totales de la orden excluyendo los ítems `Voided`.
- Un movimiento CxP del tipo nuevo por la suma de los montos a devolver, en el período abierto y en la moneda de la orden.
- Escenarios:
  - **A** (devuelve exactamente el precio de lista con impuesto): CxP y Posición quedan como si los ítems no hubieran existido.
  - **B** (devuelve menos): la Posición queda menor.
  - **C** (devuelve más): la Posición queda mayor.

### Ítems `Voided` en el resto del sistema
- Se excluyen de: recálculos (`ApplyOrderCalculationSuite`, `RecalcItemsOnOrderSaveAsync`), conteo de ítems, Reasignar, toggle IsReceived, edición/eliminación.
- Al anular luego la orden completa, **no** se genera un segundo `Void` para los ítems ya anulados.

## Open Questions
- ¿Migrar `ReversoPaquete` (hoy monto negativo) al mismo esquema "tipo que resta" con monto positivo, y migrar los datos existentes?
- `ShippingAmountIntern` y `DiscountAmount` (nivel orden) no se tocan al anular ítems: confirmar en el spec.
- Impacto en Metas de Ventas / comisiones: ¿los ítems anulados restan de la venta del período? (verificar de dónde lee el módulo de metas)
- Al anular la orden completa hoy no se revierte la entrada CxP `AutoActiva`: ¿fuera de alcance, o se alinea con el nuevo tipo que resta?
- ¿El tipo CxP nuevo puede eliminarse manualmente desde la UI de CxP como las entradas manuales?
