# Feature Specification: Indicadores en el registro de pago y estado Recibido de los ítems

**Feature Branch**: `021-indicadores-registro-pago`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "Indicadores en Saldos de clientes y estado Recibido de los ítems (issues #44 y #48). Fuente: brainstorm/20-indicadores-registro-pago.md."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Marcar ítems como recibidos mediante su estado (Priority: P1)

Cuando la orden está Entregando, el usuario marca o desmarca la casilla "Recibido" de cada ítem, igual que hoy. La diferencia es que ahora la casilla cambia el **estado** del ítem entre Activo y Recibido, en lugar de un indicador aparte. El estado Recibido reemplaza por completo al indicador anterior: los ítems que ya estaban marcados como recibidos aparecen en Recibido sin que nadie tenga que volver a marcarlos.

**Why this priority**: Es la base de las etiquetas. Además, el estado Recibido tiene que convivir con la anulación de ítems sin alterar ningún monto.

**Independent Test**: En una orden Entregando, marcar un ítem y desmarcar otro. Verificar el estado de cada uno. Verificar también que los ítems marcados antes del cambio aparecen como recibidos.

**Acceptance Scenarios**:

1. **Given** una orden Entregando con un ítem Activo, **When** el usuario marca su casilla "Recibido", **Then** el ítem pasa a estado Recibido y la fila se muestra resaltada en verde, como hoy.
2. **Given** un ítem Recibido en una orden Entregando, **When** el usuario desmarca la casilla, **Then** el ítem vuelve a estado Activo.
3. **Given** ítems que estaban marcados como recibidos antes de este cambio, **When** se aplica la actualización, **Then** quedan en estado Recibido, con la casilla marcada, y el indicador anterior deja de existir.
4. **Given** una orden en un estado distinto de Entregando, o un ítem Anulado, **When** se intenta cambiar la recepción, **Then** el sistema lo rechaza con el mismo mensaje de hoy.

---

### User Story 2 - Los ítems recibidos siguen contando en la orden (Priority: P1)

Un ítem recibido sigue siendo parte de la orden con todos sus efectos. Cuenta en los totales, en la ganancia y en el conteo de ítems. Si la orden se anula, también se le deja de cobrar al cliente. Solo los ítems Anulados quedan fuera de la orden.

**Why this priority**: Sin esto, introducir el estado Recibido rompería saldos de clientes y totales de órdenes (hallazgos del issue #48). Es un riesgo financiero.

**Independent Test**: En una orden Entregando con ítems Recibidos, comparar los totales y el conteo antes y después de marcarlos. Después anular la orden y verificar que el saldo de cada cliente baja en el monto de **todos** sus ítems no anulados.

**Acceptance Scenarios**:

1. **Given** una orden Entregando, **When** se marcan ítems como Recibidos, **Then** los totales de la orden (precio acordado total, subtotal, impuestos, envío a CR, total a proveedor, total orden y ganancia estimada) y el conteo de ítems del listado de órdenes no cambian.
2. **Given** una orden Entregando con ítems Recibidos y Activos, **When** se anula la orden completa, **Then** se registra una anulación de cobro por cada ítem Recibido y Activo. A los Anulados no se les registra otra.
3. **Given** una orden Entregando con ítems Recibidos, **When** se reasigna o se cambia el precio de un ítem, **Then** el precio acordado total y la ganancia de la orden incluyen a los ítems Recibidos.
4. **Given** una orden con ítems Anulados y Recibidos, **When** se calculan totales o conteos, **Then** solo los Anulados quedan excluidos.

---

### User Story 3 - Etiquetas informativas en Saldos de clientes (Priority: P2)

En "Saldos de clientes" de la pantalla de Pagos, el usuario ve junto a cada cliente con saldo pendiente unas etiquetas que explican su situación. **"Productos en camino"** indica que el cliente tiene mercadería por recibir. **"Sugerir adelanto"** indica que conviene pedirle un anticipo; la práctica comercial es 50% al ordenar y 50% al recibir, como guía y no como bloqueo. **"Entregado – por cobrar"** indica que ya recibió todo lo de sus órdenes en curso y falta cobrar. Un cliente puede tener varias etiquetas a la vez. La etiqueta "Saldo pendiente" desaparece.

**Why this priority**: Es el valor visible del requerimiento, pero depende de las historias 1 y 2.

**Independent Test**: Preparar clientes en cada situación y verificar las etiquetas de cada uno en Saldos de clientes.

**Acceptance Scenarios**:

1. **Given** cualquier cliente en Saldos de clientes, **When** se muestra la lista, **Then** ya no aparece la etiqueta "Saldo pendiente". Los clientes con saldo a favor siguen mostrando "Crédito a favor".
2. **Given** un cliente con saldo pendiente y al menos un ítem Activo en una orden Activa o Entregando, **When** se muestra la lista, **Then** aparece "Productos en camino".
3. **Given** un cliente con saldo pendiente de 50.000, cuyos ítems no anulados en órdenes Activas o Entregando suman 60.000 en precio acordado, **When** se muestra la lista, **Then** aparece "Sugerir adelanto". Si suman exactamente 50.000, también aparece.
4. **Given** el mismo cliente, pero sus ítems suman 40.000, **When** se muestra la lista, **Then** no aparece "Sugerir adelanto".
5. **Given** un cliente con saldo pendiente, al menos un ítem Recibido en órdenes Activas o Entregando y ningún ítem Activo en esas órdenes, **When** se muestra la lista, **Then** aparece "Entregado – por cobrar".
6. **Given** un cliente con saldo pendiente que solo proviene de transacciones manuales u órdenes Completadas, sin ítems en órdenes Activas o Entregando, **When** se muestra la lista, **Then** no aparece ninguna de las tres etiquetas nuevas.
7. **Given** un cliente con un ítem Activo en una orden Activa y un ítem Recibido en una orden Entregando, **When** se muestra la lista, **Then** aparece "Productos en camino" y no aparece "Entregado – por cobrar".

---

### Edge Cases

- **Ítem marcado como recibido y luego anulado:** no puede ocurrir. La anulación parcial solo aplica a órdenes Activas y la recepción solo a órdenes Entregando. Aun así, la conversión de datos no pasa a Recibido ningún ítem que ya esté Anulado.
- **Órdenes Completadas o Anuladas con ítems recibidos:** sus ítems también pasan a Recibido en la conversión. No influyen en las etiquetas, porque las etiquetas solo miran órdenes Activas o Entregando.
- **Cliente con saldo a favor (negativo):** solo muestra "Crédito a favor". Las etiquetas nuevas se evalúan únicamente con saldo pendiente positivo.
- **Filtro de organización:** las etiquetas consideran las órdenes de la misma organización que se usa para calcular el saldo mostrado. Con la vista de saldo completo se consideran todas las organizaciones.
- **Cliente genérico (especulativo):** se le aplican las mismas reglas que a cualquier otro cliente.
- **Ítems sin cliente asignado:** no generan etiquetas, porque no pertenecen a ningún cliente de la lista.
- **El filtro de texto de la lista** no cambia las etiquetas de cada cliente, solo cuáles se muestran.

## Requirements *(mandatory)*

### Functional Requirements

**Estado Recibido**

- **FR-001**: Los ítems de orden MUST tener un estado adicional **Recibido** junto a Activo y Anulado.
- **FR-002**: La casilla "Recibido" de la pantalla de ítems MUST cambiar el estado del ítem a Recibido al marcarla y a Activo al desmarcarla. Su apariencia y comportamiento visibles no cambian.
- **FR-003**: El cambio de recepción MUST seguir permitido solo en órdenes Entregando y MUST NOT aplicarse a ítems Anulados.
- **FR-004**: Los ítems que estaban marcados como recibidos MUST pasar a estado Recibido mediante una conversión de datos. Se exceptúan los ítems Anulados, que conservan su estado.
- **FR-005**: Después de la conversión, el indicador anterior de recepción MUST dejar de existir. Ninguna regla, cálculo ni pantalla lo usa.

**Consistencia de la orden (issue #48)**

- **FR-006**: Toda regla que hoy significa "ítem no anulado" MUST tratar como incluidos a los ítems Activos y Recibidos. Solo los Anulados quedan fuera.
- **FR-007**: Los totales de la orden y la ganancia estimada MUST incluir los ítems Recibidos. Esto aplica al recalcular por edición, por reasignación o por cambio de precio.
- **FR-008**: El conteo de ítems de cada orden en el listado de órdenes MUST incluir los ítems Recibidos. Las reglas que dependen de ese conteo MUST comportarse igual que antes del cambio.
- **FR-009**: Al anular una orden completa, el sistema MUST registrar una anulación de cobro por cada ítem Activo o Recibido. No se registra otra para los ítems que ya estaban Anulados.
- **FR-010**: La anulación parcial de ítems MUST seguir permitiendo seleccionar solo ítems no anulados y MUST seguir impidiendo anular todos los ítems no anulados restantes.

**Etiquetas en Saldos de clientes**

- **FR-011**: La etiqueta "Saldo pendiente" MUST eliminarse. "Crédito a favor" se mantiene.
- **FR-012**: El saldo de cada cliente MUST seguir calculándose exactamente como hoy. Las etiquetas MUST evaluarse después de ese cálculo, reutilizando el saldo obtenido y solo para los clientes incluidos en la lista.
- **FR-013**: Las etiquetas nuevas MUST evaluarse solo para clientes con saldo pendiente positivo y MUST considerar solo ítems del cliente en órdenes Activas o Entregando. Las órdenes se limitan a la misma organización usada para calcular el saldo, o a todas cuando el saldo es global.
- **FR-014**: "Productos en camino" MUST mostrarse cuando el cliente tiene al menos un ítem Activo en esas órdenes.
- **FR-015**: "Sugerir adelanto" MUST mostrarse cuando la suma del precio acordado de los ítems no anulados del cliente en esas órdenes es mayor o igual que su saldo pendiente.
- **FR-016**: "Entregado – por cobrar" MUST mostrarse cuando el cliente tiene al menos un ítem Recibido y ningún ítem Activo en esas órdenes.
- **FR-017**: Las etiquetas MUST NOT ser excluyentes. Un cliente puede mostrar varias a la vez.

### Key Entities

- **Ítem de orden**: producto pedido para un cliente dentro de una orden. Su estado es Activo (pendiente de recibir), Recibido o Anulado. Activo y Recibido cuentan en la orden; Anulado no.
- **Saldo de cliente**: lo que el cliente debe (positivo) o tiene a favor (negativo), según sus cargos, pagos y anulaciones. No cambia en este feature.
- **Etiqueta de saldo**: indicador informativo calculado al mostrar la lista, a partir del saldo y de los ítems del cliente en órdenes Activas o Entregando. No se guarda.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los ítems marcados como recibidos antes del cambio aparecen como Recibidos después, y ninguno de los otros cambia de estado.
- **SC-002**: Para todas las órdenes existentes, los totales, la ganancia estimada y el conteo de ítems son idénticos antes y después del cambio.
- **SC-003**: Al anular una orden Entregando con ítems recibidos, el saldo de cada cliente baja exactamente en la suma del precio acordado de sus ítems no anulados de esa orden.
- **SC-004**: Los saldos mostrados en Saldos de clientes son idénticos antes y después del cambio. Solo cambian las etiquetas.
- **SC-005**: El usuario identifica, sin abrir órdenes, qué clientes tienen mercadería en camino, a cuáles sugerirles adelanto y cuáles ya recibieron todo y deben. Para cada cliente de prueba las etiquetas coinciden con las reglas en el 100% de los casos.
- **SC-006**: La lista de Saldos de clientes sigue cargando en un tiempo similar al actual (sin demora perceptible para el usuario).

## Assumptions

- "Precio acordado" es el monto que se le cobra al cliente por el ítem.
- La etiqueta "Crédito a favor" y el resaltado de los clientes con saldo a favor no cambian.
- Las órdenes Completadas no influyen en las etiquetas, aunque el cliente tenga saldo por ellas. Esa es la intención de "solo mira órdenes".
- No se agrega un historial de cambios de recepción. Se conserva solo la fecha de última actualización del ítem, como hoy.
- Las etiquetas solo se muestran en "Saldos de clientes". La tarjeta de saldo del cliente seleccionado no cambia.
- Este feature cierra los issues #44 y #48.
