# Feature Specification: Anulación parcial de ítems de una orden activa

**Feature Branch**: `020-anulacion-items-orden`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "Anulación parcial de ítems de una orden activa (issue #43). Fuente: brainstorm/19-anulacion-items-orden.md."

## Clarifications

### Session 2026-11-15

- Q: ¿El movimiento CxP "Anulación de ítems" se puede eliminar desde la pantalla de CxP? → A: Sí, como el resto de movimientos de un período abierto.
- Q: ¿La anulación parcial queda registrada en el Historial de la orden? → A: Sí, como una entrada Activa → Activa con una nota de los ítems y el ajuste, más la justificación.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Anular ítems seleccionados de una orden activa (Priority: P1)

Un cliente desiste de uno o más productos de una orden que ya está Activa (ya se pidió al proveedor). El usuario abre los ítems de la orden, presiona "Anular", marca los ítems que se van a anular, indica cuánto devuelve el proveedor por cada uno, escribe una justificación y confirma. Los ítems quedan anulados (no se borran), al cliente se le deja de cobrar cada ítem, los totales de la orden se recalculan sin esos ítems y la cuenta por pagar al proveedor se reduce en lo que el proveedor devuelve.

**Why this priority**: Es el núcleo del requerimiento. Hoy la única opción es anular la orden completa, lo que obliga a rehacer órdenes por un solo ítem.

**Independent Test**: Con una orden Activa de 3 ítems, se anula 1 ítem con el monto devuelto por defecto. Se verifica que el ítem quede anulado, que el saldo del cliente baje en el precio acordado del ítem, que los totales de la orden cambien y que la CxP del período baje en el monto devuelto.

**Acceptance Scenarios**:

1. **Given** una orden Activa, **When** el usuario abre la pantalla de ítems, **Then** ve en la barra de herramientas un botón rojo "Anular" junto a "Volver".
2. **Given** una orden en cualquier estado distinto de Activa, **When** el usuario abre la pantalla de ítems, **Then** el botón "Anular" no se muestra.
3. **Given** una orden Activa, **When** el usuario presiona "Anular", **Then** aparece una columna de casillas para seleccionar ítems. Solo se pueden marcar los ítems no anulados.
4. **Given** ítems seleccionados, **When** el usuario continúa, **Then** se abre una confirmación que pregunta "¿Está seguro que desea anular los ítems seleccionados?". Lista cada ítem con un campo editable "Monto a devolver por el proveedor", precargado con el precio de lista con impuesto del ítem en la moneda de la orden.
5. **Given** la confirmación abierta, **When** el usuario confirma con una justificación y montos válidos, **Then** los ítems quedan anulados, se registra una anulación de cobro por cada ítem para su cliente, se recalculan los totales de la orden y se registra un único movimiento de reducción en la CxP del período abierto por la suma de los montos a devolver.
6. **Given** la anulación completada, **When** se refresca la pantalla, **Then** los ítems anulados se muestran con fondo rojo pastel claro.
7. **Given** la confirmación abierta, **When** el usuario presiona "Cancelar", **Then** no se modifica nada.

---

### User Story 2 - Comparativa financiera antes/después y ajuste (Priority: P1)

Antes de confirmar, el usuario ve cómo quedará la orden y cuánto debería devolver el proveedor en teoría frente a lo que realmente devuelve, para decidir con información.

**Why this priority**: El usuario necesita validar el impacto antes de una operación que no se puede deshacer. El ticket lo pide expresamente.

**Independent Test**: Se abre la confirmación con 1 ítem seleccionado y se comparan los valores "Antes" con los de la orden actual y los de "Después" con el cálculo manual sin ese ítem. Al editar el monto a devolver, el ajuste y la diferencia se actualizan.

**Acceptance Scenarios**:

1. **Given** la confirmación abierta, **Then** se muestran los valores Antes y Después de Precio Acordado Total, SubTotal, Impuestos, Envío a CR, Total a Proveedor, Total Orden y Ganancia Est. El Después se calcula con las reglas actuales de la orden, excluyendo los ítems seleccionados y los ya anulados.
2. **Given** la confirmación abierta, **Then** se muestra el Ajuste financiero (suma de los montos a devolver), la diferencia teórica (Total a Proveedor Antes − Después) y la diferencia entre ambos.
3. **Given** que el usuario cambia el monto a devolver de un ítem, **Then** el Ajuste financiero y la diferencia se actualizan de inmediato.

---

### User Story 3 - Consistencia de CxP, Saldos y Posición según lo devuelto (Priority: P2)

Tras la anulación, las pantallas de CxP y Payments/Saldos quedan consistentes según lo que realmente devolvió el proveedor.

**Why this priority**: Garantiza la integridad contable, pero depende de la US1.

**Independent Test**: Se ejecutan los tres escenarios (devuelve exacto, menos y más) sobre órdenes equivalentes y se comparan los indicadores de CxP con los de una orden equivalente creada sin esos ítems.

**Acceptance Scenarios**:

1. **Given** que el proveedor devuelve exactamente el precio de lista con impuesto de los ítems (Escenario A), **When** se anula, **Then** "Por pagar", "Saldos por cobrar" y "Shipping CR pendiente" quedan como si los ítems nunca hubieran existido, salvo diferencias de redondeo de céntimos.
2. **Given** que el proveedor devuelve menos (Escenario B), **When** se anula, **Then** el saldo del cliente refleja que los ítems no se le cobran, y la deuda con el proveedor y la Posición reflejan que se devolvió menos (la Posición queda menor que en el Escenario A por la diferencia).
3. **Given** que el proveedor devuelve más (Escenario C), **When** se anula, **Then** la Posición queda mayor que en el Escenario A por la diferencia.
4. **Given** la pantalla de CxP, **Then** el movimiento de la anulación se muestra con un tipo propio ("Anulación de ítems"), con la referencia de la orden, y se muestra como una reducción.

---

### Edge Cases

- **Todos los ítems restantes seleccionados**: si la selección cubre todos los ítems no anulados de la orden, la confirmación bloquea "Continuar con la anulación" e indica que se debe usar la anulación de la orden.
- **Sin período CxP abierto**: la anulación se rechaza con el mensaje "No hay un período CxP abierto" y no se modifica nada.
- **Justificación vacía o de más de 1000 caracteres**: no se permite continuar.
- **Monto a devolver negativo o vacío**: no se permite continuar. Se permite 0 (el proveedor no devuelve nada).
- **Ítem ya anulado**: no se puede seleccionar de nuevo. Si llega uno por una solicitud concurrente, la operación se rechaza completa.
- **La orden cambió de estado mientras la confirmación estaba abierta**: se rechaza si la orden ya no está Activa.
- **Ítem reasignado antes de anularlo**: la anulación de cobro se hace al cliente actual del ítem, por su precio acordado actual.
- **Anular la orden completa después de una anulación parcial**: solo se generan anulaciones de cobro para los ítems que siguen sin anular. Los ya anulados no se anulan dos veces.
- **Se elimina desde CxP el movimiento "Anulación de ítems"**: se permite (período abierto). Los ítems siguen anulados y los cobros al cliente siguen revertidos; la CxP vuelve a reflejar la deuda completa hasta que el usuario registre la corrección que corresponda.
- **Falla a mitad de proceso**: ningún cambio queda aplicado parcialmente (todo o nada).

## Requirements *(mandatory)*

### Functional Requirements

**Estado de los ítems**

- **FR-001**: Cada ítem de orden MUST tener un estado con solo dos valores posibles: Activo y Anulado. Los ítems existentes y los nuevos quedan en Activo.
- **FR-002**: Los estados de ítem que hoy existen y no se usan (Pendiente, Revisado, Entregada) MUST eliminarse del sistema.
- **FR-003**: Los ítems anulados y sus transacciones MUST conservarse; nunca se borran.

**Pantalla de ítems**

- **FR-004**: La pantalla de ítems de la orden MUST tener una barra de herramientas oficial que contenga "Volver" y, solo cuando la orden está Activa, un botón rojo "Anular".
- **FR-005**: Al presionar "Anular", el sistema MUST mostrar una columna de selección en la tabla de ítems, que solo permite marcar ítems Activos, junto con los botones "Anular seleccionados" (habilitado solo con al menos un ítem marcado; abre la confirmación) y "Cancelar" (oculta la columna y descarta la selección).
- **FR-006**: La confirmación MUST mostrar el mensaje "¿Está seguro que desea anular los ítems seleccionados?", la lista de ítems seleccionados con su descripción, su cliente y el campo editable "Monto a devolver por el proveedor" (en la moneda de la orden, precargado con el precio de lista con impuesto del ítem), un campo de justificación obligatorio y los botones "Cancelar" y "Continuar con la anulación".
- **FR-007**: La confirmación MUST mostrar la comparativa Antes/Después de Precio Acordado Total, SubTotal, Impuestos, Envío a CR, Total a Proveedor, Total Orden y Ganancia Est. El Antes son los totales guardados actualmente en la orden. El Después aplica las reglas de cálculo vigentes de la orden sobre los ítems que quedarían activos. El descuento y el envío interno de la orden no cambian.
- **FR-008**: La confirmación MUST mostrar el Ajuste financiero (suma de los montos a devolver), la diferencia teórica (Total a Proveedor Antes − Después) y la diferencia entre ambos, y actualizarlos al editar cualquier monto.
- **FR-009**: Si la selección cubre todos los ítems Activos de la orden, la confirmación MUST deshabilitar "Continuar con la anulación" e indicar que se use la anulación de la orden.
- **FR-010**: Tras una anulación exitosa, la tabla de ítems MUST refrescarse, y los ítems Anulados MUST mostrarse con fondo rojo pastel claro y una etiqueta de estado "Anulado".

**Procesamiento**

- **FR-011**: El sistema MUST validar, antes de aplicar cualquier cambio, que la orden esté Activa, que todos los ítems pertenezcan a la orden y estén Activos, que quede al menos un ítem Activo, que la justificación no esté vacía ni supere 1000 caracteres, que cada monto a devolver sea ≥ 0 y que exista un período CxP abierto. Si alguna validación falla, se rechaza con un mensaje claro y no se modifica nada.
- **FR-012**: Al confirmar, el sistema MUST cambiar a Anulado el estado de cada ítem seleccionado.
- **FR-013**: Por cada ítem anulado, el sistema MUST registrar una transacción de anulación (monto positivo, tipo Anulación) al cliente actual del ítem, por su precio acordado actual, en la moneda local (la misma de los cargos al cliente), en la organización de la orden y con la fecha y hora actual de Costa Rica, igual que las anulaciones de la orden completa. La descripción incluye la justificación.
- **FR-014**: El sistema MUST recalcular y guardar los totales de la orden (Precio Acordado Total, SubTotal, Impuestos, Envío a CR, Total a Proveedor, Total Orden, Ganancia Est.) excluyendo los ítems Anulados, con los mismos valores mostrados en el Después de la confirmación.
- **FR-015**: El sistema MUST registrar un único movimiento CxP por anulación, en el período abierto y en la moneda de la orden, de un tipo nuevo "Anulación de ítems" que reduce la deuda. Su monto se guarda **positivo** e igual al Ajuste financiero, y lleva la referencia a la orden y la justificación.
- **FR-015a**: El sistema MUST registrar una entrada en el Historial de la orden con estado anterior y nuevo = Activa, la fecha actual, la nota "Anulación parcial: N ítem(s) – ajuste {monto con signo de moneda}" y la justificación (máximo 1000 caracteres).
- **FR-016**: Todos los cálculos que agregan movimientos CxP (Por pagar por moneda, Por pagar en colones, Deuda, Posición, vista previa y cierre de mes, saldo anterior) MUST restar los movimientos de tipo "Anulación de ítems" en lugar de sumarlos.
- **FR-017**: La pantalla de CxP MUST mostrar los movimientos "Anulación de ítems" con su etiqueta de tipo y presentarlos como reducción, igual que se presentan hoy las reversiones.
- **FR-018**: Todo el procesamiento de FR-012 a FR-015a MUST ser atómico: o se aplica completo o no se aplica nada.

**Ítems anulados en el resto del sistema**

- **FR-019**: Los ítems Anulados MUST excluirse de cualquier recálculo de totales de la orden, incluido el recálculo al guardar la orden.
- **FR-020**: Los ítems Anulados MUST excluirse del conteo de ítems de la orden mostrado en el listado de órdenes.
- **FR-021**: Los ítems Anulados MUST NOT poder editarse, eliminarse, reasignarse ni marcarse/desmarcarse como recibidos.
- **FR-022**: Al anular la orden completa, el sistema MUST generar transacciones de anulación solo para los ítems que siguen Activos.

### Key Entities

- **Ítem de orden**: producto pedido para un cliente dentro de una orden. Nuevo atributo: estado (Activo / Anulado).
- **Transacción**: movimiento de cobro al cliente. La anulación usa el tipo existente "Anulación" (monto positivo que resta en Saldos), con origen en el ítem.
- **Movimiento CxP**: movimiento de la cuenta por pagar al proveedor dentro de un período. Nuevo tipo "Anulación de ítems", guardado con monto positivo, que resta en todos los totales de CxP.
- **Orden**: sus totales se recalculan sin los ítems anulados.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un usuario puede anular uno o varios ítems de una orden activa en menos de 1 minuto, sin tener que anular ni rehacer la orden.
- **SC-002**: En el Escenario A, los indicadores Por pagar, Saldos por cobrar, Shipping CR pendiente y Posición coinciden (±0,05 por redondeo) con los de una orden equivalente creada sin los ítems anulados.
- **SC-003**: En los Escenarios B y C, la Posición difiere de la del Escenario A exactamente en la diferencia entre lo devuelto y el precio de lista con impuesto de los ítems (convertido con el tipo de cambio del período).
- **SC-004**: El 100% de las anulaciones deja intactos los registros originales (ítems, cargos y movimiento AutoActiva) y agrega solo registros nuevos.
- **SC-005**: Ninguna transacción ni movimiento CxP generado por esta funcionalidad tiene monto negativo.
- **SC-006**: Ninguna anulación fallida deja cambios parciales.

## Assumptions

- Solo aplica a órdenes en estado Activa (no Pendiente, Entregando, Completada ni Anulada), como pide el ticket.
- El monto a devolver por el proveedor se mide contra Total a Proveedor (precio de lista con impuesto), no contra Total Orden: la CxP se registró con Total a Proveedor, y el envío a CR se corrige solo al recalcular la orden (decisión del brainstorm 19).
- No hay operación para revertir una anulación de ítems. Si el monto devuelto real cambia después, se corrige con un movimiento CxP manual.
- El descuento y el envío interno de la orden no cambian al anular ítems.
- En órdenes activadas antes de la 018 (totales guardados con fórmulas anteriores), el Después recalcula todo con las reglas actuales. La diferencia teórica puede incluir ese efecto además de los ítems anulados; se acepta, y el Ajuste financiero sigue siendo lo que manda para la CxP.
- Las Metas de Ventas no se ven afectadas: su monto real se ingresa manualmente.
- Los movimientos "Anulación de ítems" siguen las mismas reglas de eliminación que el resto de movimientos CxP en un período abierto.
- Fuera de alcance: migrar "Reverso Paquete" (hoy con monto negativo) al esquema de tipo que resta, y revertir el movimiento AutoActiva al anular la orden completa. Quedan como temas abiertos del brainstorm 19.
- Sin integración con sistemas del proveedor; la devolución se gestiona fuera del sistema.
