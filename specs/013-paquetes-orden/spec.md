# Feature Specification: Paquetes de Envío por Orden (OrderPackage)

**Feature Branch**: `013-paquetes-orden`

**Created**: 2026-09-28

**Status**: Draft

**Input**: Brainstorm `brainstorm/13-paquetes-orden.md`: registrar los paquetes de envío (shipping a CR) de cada orden; cada paquete genera su deuda en Cuentas por Pagar y al eliminarlo se genera un movimiento de reversión; "Shipping CR Pendientes" pasa a calcularse por orden (Activas y Entregando) descontando los paquetes; se descarta la regla de shipping real al entregar del requerimiento 009.

## Clarifications

### Session 2026-09-28

- Q: ¿Se valida algo al pasar de Entregando a Entregada (p. ej. pendiente de shipping distinto de 0)? → A: No; la transición no valida paquetes en esta versión.
- Q: ¿Las entradas CxP manuales deben aceptar montos negativos por coherencia con las reversiones? → A: No; solo las entradas automáticas Reverso Paquete pueden ser negativas.
- Q: ¿Cómo se muestran los montos negativos en la tabla de entradas CxP? → A: Con signo "−" delante del monto y en color rojo; restan del total de su moneda.
- Q: ¿Qué nombre de la orden se usa en la referencia? → A: El nombre de la orden (NameOfOrder) vigente al momento de registrar o eliminar el paquete.
- Q: (Agregado tras la implementación, a pedido del usuario) ¿Se registra el número de guía del paquete? → A: Sí, campo opcional "Número de tracking" (TrackingNumber), texto libre de hasta 100 caracteres; se muestra en el formulario y en la lista de paquetes; no se incluye en la referencia CxP. Se entrega en una migración separada.
- Q: ¿Se pueden borrar desde la pantalla CxP las entradas Auto-Paquete y Reverso Paquete? → A: Sí, igual que hoy con cualquier entrada del mes abierto (regla FR-018 del 009). Borrarlas no modifica el paquete; el cuadre queda a cargo del usuario.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar un paquete y que su costo llegue a CxP (Priority: P1)

Mientras una orden está Activa o Entregando, el usuario registra cada paquete que llega a Costa Rica (fecha de entrega, monto y descripción opcional) desde la pantalla de la orden. Cada paquete registrado aparece automáticamente como deuda en Cuentas por Pagar, en la moneda de la orden.

**Why this priority**: Es el objetivo central: que el costo real del shipping llegue a CxP. Hoy no llega nunca.

**Independent Test**: En una orden Activa, con un mes CxP abierto, agregar un paquete de $30 con descripción "Caja 1"; verificar que aparece en la lista de paquetes de la orden y que en CxP existe una entrada "Auto-Paquete" de $30 con referencia "{Nombre de la orden} - Caja 1".

**Acceptance Scenarios**:

1. **Given** una orden Activa o Entregando y un mes CxP abierto, **When** el usuario agrega un paquete con fecha, monto mayor a 0 y descripción, **Then** el paquete se guarda en la moneda de la orden y se crea una entrada CxP de tipo Auto-Paquete con ese monto, esa moneda, la orden asociada y referencia "{Nombre de la orden} - {Descripción}".
2. **Given** un paquete sin descripción, **When** se registra, **Then** la referencia de la entrada CxP es solo "{Nombre de la orden}".
3. **Given** que no hay un mes CxP abierto, **When** el usuario intenta agregar un paquete, **Then** el paquete no se guarda y se muestra "No hay un período CxP abierto.".
4. **Given** el formulario de agregar paquete, **When** se abre, **Then** la fecha de entrega sugiere el día de hoy y puede cambiarse a cualquier fecha.
5. **Given** un monto vacío, cero o negativo, **When** el usuario intenta guardar, **Then** el paquete no se guarda y se muestra un mensaje de validación.

---

### User Story 2 - "Shipping CR Pendientes" refleja lo que falta por llegar (Priority: P1)

En la pantalla CxP, "Shipping CR Pendientes" suma, para cada orden Activa o Entregando, cuánto de su envío estimado todavía no se ha registrado como paquetes, convertido a colones. Así la Posición descuenta correctamente el shipping que falta, sin contar dos veces lo que ya está en CxP.

**Why this priority**: Sin esto, las órdenes en Entregando no aportan su shipping a ningún lado y la Posición se ve mejor de lo que es.

**Independent Test**: Con una orden Entregando con envío estimado $100 y un paquete de $30, y tipo de cambio 500, verificar que esa orden aporta ₡35.000 ((100 − 30) × 500) a "Shipping CR Pendientes"; agregar paquetes hasta superar $100 y verificar que la orden aporta 0.

**Acceptance Scenarios**:

1. **Given** órdenes en estado Activa y Entregando, **When** el usuario abre CxP, **Then** "Shipping CR Pendientes" es la suma, por orden, de máx(0, envío estimado − suma de sus paquetes), convertida a colones con el tipo de cambio del período (sin conversión para órdenes en colones).
2. **Given** una orden cuyos paquetes superan su envío estimado, **When** se calcula el indicador, **Then** esa orden aporta 0 (no reduce lo pendiente de otras órdenes).
3. **Given** órdenes en estado Pendiente, Entregada, Completada o Anulada, **When** se calcula el indicador, **Then** no aportan nada.
4. **Given** el tipo de cambio del período en 0, **When** se calcula el indicador, **Then** se comporta como hoy (muestra 0 y el aviso de tipo de cambio).
5. **Given** que el indicador cambia, **When** se muestra la Posición, **Then** la Posición usa el nuevo valor con la misma fórmula de siempre.

---

### User Story 3 - Eliminar un paquete registrado por error (Priority: P2)

Si el usuario registró un paquete por error, lo elimina desde la lista de la orden (solo en Activa o Entregando). El sistema registra en CxP un movimiento de reversión por el mismo monto en negativo, de modo que la deuda y lo pendiente vuelven a cuadrar aunque el paquete original sea de un mes ya cerrado.

**Why this priority**: Corrige errores sin descuadres en CxP ni ajustes manuales.

**Independent Test**: Agregar un paquete de $30, eliminarlo (confirmando), y verificar que desaparece de la lista, que en CxP existe una entrada "Reverso Paquete" de −$30 con referencia "Reverso: {Orden} - {Descripción}", y que "Shipping CR Pendientes" vuelve a su valor anterior.

**Acceptance Scenarios**:

1. **Given** una orden Activa o Entregando con un paquete y un mes CxP abierto, **When** el usuario elimina el paquete y confirma, **Then** el paquete se elimina y se crea en el mes abierto una entrada CxP Reverso Paquete con el monto en negativo, la misma moneda, la misma orden y referencia "Reverso: {Nombre de la orden} - {Descripción}" (o "Reverso: {Nombre de la orden}" sin descripción).
2. **Given** que la entrada Auto-Paquete original pertenece a un mes ya cerrado, **When** se elimina el paquete, **Then** la entrada original no se modifica y la reversión se registra en el mes abierto.
3. **Given** que no hay un mes CxP abierto, **When** el usuario intenta eliminar un paquete, **Then** no se elimina y se muestra un mensaje.
4. **Given** una orden en un estado distinto de Activa o Entregando, **When** alguien intenta eliminar un paquete (incluso llamando directamente al sistema), **Then** la operación se rechaza.
5. **Given** el botón Eliminar, **When** el usuario lo presiona, **Then** se pide confirmación antes de eliminar.

---

### User Story 4 - Sección de paquetes según el estado de la orden (Priority: P2)

En la pantalla de la orden, la sección de paquetes muestra un resumen (Envío a CR estimado, Total en paquetes, Pendiente) y la lista de paquetes (fecha de entrega, descripción, monto). Lo que se puede hacer depende del estado de la orden.

**Why this priority**: Da visibilidad por orden de cuánto shipping ha llegado y evita cambios en estados donde no corresponden.

**Independent Test**: Abrir una orden en cada estado y verificar: Pendiente → sección oculta; Activa/Entregando → lista, resumen, agregar y eliminar; Entregada/Completada/Anulada → lista y resumen en solo lectura.

**Acceptance Scenarios**:

1. **Given** una orden Pendiente, **When** se abre su pantalla, **Then** la sección de paquetes no se muestra.
2. **Given** una orden Activa o Entregando, **When** se abre su pantalla, **Then** se muestran el resumen, la lista, el formulario para agregar y el botón Eliminar por fila; no hay opción de editar paquetes.
3. **Given** una orden Entregada, Completada o Anulada, **When** se abre su pantalla, **Then** se muestran el resumen y la lista en solo lectura, sin agregar ni eliminar.
4. **Given** el resumen, **When** se muestra, **Then** "Pendiente" es máx(0, envío estimado − total en paquetes) y todos los montos están en la moneda de la orden con su signo.
5. **Given** que se agrega o elimina un paquete, **When** termina la operación, **Then** la lista y el resumen se actualizan sin recargar manualmente.

---

### User Story 5 - Retiro de la regla de shipping del requerimiento 009 (Priority: P2)

La transición a "Entregada" deja de pedir "Shipping real a CR" y deja de crear la entrada CxP Auto-Entregada. El dato de shipping real al entregar se elimina. La documentación del requerimiento 009 indica que esa regla fue descartada y reemplazada por este requerimiento.

**Why this priority**: Evita dos mecanismos de shipping en paralelo y datos que no se usan.

**Independent Test**: Pasar una orden de Entregando a Entregada y verificar que el diálogo solo pide fecha y notas, y que no se crea ninguna entrada CxP.

**Acceptance Scenarios**:

1. **Given** una orden Entregando, **When** el usuario la pasa a Entregada, **Then** el diálogo de transición no muestra el campo "Shipping real a CR" y no se crea ninguna entrada CxP.
2. **Given** entradas Auto-Entregada históricas en meses anteriores (si existieran), **When** se consultan, **Then** se siguen mostrando con su etiqueta.
3. **Given** la documentación del requerimiento 009 (brainstorm y spec), **When** se lee, **Then** incluye una nota de que la regla de shipping (FR-008 a FR-010 y el cálculo de Shipping CR Pendientes solo con órdenes Activas) fue descartada y reemplazada por el requerimiento 013.

---

### Edge Cases

- **Paquetes que superan el estimado**: la orden aporta 0 a "Shipping CR Pendientes"; el resumen de la orden muestra Pendiente 0.
- **Orden sin envío estimado (0)** con paquetes: aporta 0; los paquetes siguen generando deuda en CxP.
- **Orden anulada con paquetes**: los paquetes y sus entradas CxP se conservan (sin reversión automática al anular, igual que la regla del 009 para anulaciones); la lista queda en solo lectura.
- **Error al crear la entrada CxP**: el paquete no se guarda (todo o nada); igual al eliminar con su reversión.
- **Doble clic en Agregar/Eliminar**: el botón se deshabilita durante la operación (FR-017a).
- **Entrada Auto-Paquete borrada desde CxP**: el paquete sigue en la orden y sigue descontándose de "Shipping CR Pendientes"; si luego se elimina el paquete, se genera su Reverso Paquete igualmente. El cuadre es responsabilidad del usuario.
- **Montos con decimales**: se aceptan 2 decimales, igual que el resto de montos.
- **Tipo de cambio 0**: el indicador en colones muestra 0 con el aviso existente; la entrada CxP del paquete se crea igual (se guarda en la moneda de la orden).
- **Entradas CxP negativas**: las reversiones se muestran con signo negativo en la tabla de entradas y restan del total por moneda.

## Requirements *(mandatory)*

### Functional Requirements

**Paquetes**

- **FR-001**: El sistema MUST permitir registrar paquetes de envío por orden con: fecha de entrega (requerida), monto (requerido, mayor que 0, hasta 2 decimales), número de tracking (opcional, texto libre, máximo 100 caracteres), descripción (opcional) y moneda.
- **FR-002**: La moneda del paquete MUST ser siempre la moneda de la orden, asignada automáticamente y no editable.
- **FR-003**: Los paquetes MUST ser inmutables: solo se pueden agregar y eliminar, nunca editar.
- **FR-004**: Agregar y eliminar paquetes MUST estar permitido únicamente cuando la orden está Activa o Entregando; el sistema MUST rechazar la operación en cualquier otro estado, incluso si se invoca directamente.
- **FR-005**: El formulario de agregar MUST sugerir la fecha de hoy y aceptar cualquier fecha.

**Integración con CxP**

- **FR-006**: Al agregar un paquete, el sistema MUST crear una entrada CxP en el período abierto con: moneda de la orden, monto del paquete, tipo Auto-Paquete, orden asociada, vínculo al paquete y referencia "{Nombre de la orden} - {Descripción}" (o "{Nombre de la orden}" si no hay descripción).
- **FR-007**: Al eliminar un paquete, el sistema MUST crear una entrada CxP en el período abierto con: el monto del paquete en negativo, la misma moneda, tipo Reverso Paquete, la orden asociada y referencia "Reverso: {Nombre de la orden} - {Descripción}" (o "Reverso: {Nombre de la orden}"); la entrada Auto-Paquete original MUST NOT modificarse.
- **FR-008**: Si no hay un período CxP abierto, agregar o eliminar un paquete MUST rechazarse con el mensaje "No hay un período CxP abierto.", sin cambios.
- **FR-009**: Guardar/eliminar el paquete y crear su entrada CxP MUST ser una sola operación atómica (ambas o ninguna).
- **FR-009a**: El vínculo de la entrada CxP con su paquete MUST ser opcional y quedar vacío cuando el paquete se elimina; la entrada Reverso Paquete no lleva vínculo al paquete.
- **FR-009b**: Las entradas Auto-Paquete y Reverso Paquete MUST poder borrarse desde la pantalla CxP igual que cualquier otra entrada del mes abierto; borrarlas MUST NOT modificar ni eliminar el paquete. Eliminar después el paquete genera igualmente su Reverso Paquete.
- **FR-010**: La tabla de entradas CxP MUST mostrar los tipos nuevos con etiquetas legibles ("Auto-Paquete", "Reverso Paquete") y los montos negativos con signo "−" y en color rojo.

**Shipping CR Pendientes**

- **FR-011**: "Shipping CR Pendientes" MUST calcularse como la suma, sobre las órdenes en estado Activa o Entregando, de máx(0, envío estimado a CR − suma de sus paquetes), en la moneda de cada orden y convertido a colones con el tipo de cambio del período (sin conversión para órdenes en la moneda local).
- **FR-012**: Las órdenes en cualquier otro estado MUST quedar excluidas del cálculo.
- **FR-013**: La fórmula de Posición y el comportamiento con tipo de cambio 0 MUST NOT cambiar.

**UI**

- **FR-014**: En la pantalla de la orden, la sección de paquetes MUST estar oculta en estado Pendiente; visible con agregar/eliminar en Activa y Entregando; y visible en solo lectura en los demás estados.
- **FR-015**: La sección MUST mostrar un resumen con Envío a CR estimado, Total en paquetes y Pendiente (máx(0, estimado − total)), y una lista con fecha de entrega, descripción y monto, todo en la moneda de la orden.
- **FR-016**: Eliminar un paquete MUST pedir confirmación.
- **FR-017**: La lista y el resumen MUST actualizarse tras agregar o eliminar sin recargar la página manualmente.
- **FR-017a**: Los botones Agregar y Eliminar MUST deshabilitarse mientras la operación está en curso, para que un doble clic no genere paquetes o reversiones duplicadas.

**Retiro de la regla del requerimiento 009**

- **FR-018**: La transición a Entregada MUST NOT pedir "Shipping real a CR" ni crear entradas CxP.
- **FR-019**: El dato "shipping real a CR" de la orden MUST eliminarse del sistema.
- **FR-020**: Las entradas Auto-Entregada históricas MUST conservarse y seguir mostrándose con su etiqueta.
- **FR-021**: La documentación del requerimiento 009 (brainstorm y spec) MUST registrar que su regla de shipping fue descartada y reemplazada por el requerimiento 013.

### Key Entities

- **Paquete de orden (OrderPackage)**: envío/paquete recibido de una orden. Atributos: orden, fecha de entrega, monto, moneda (la de la orden), número de tracking opcional (máx. 100), descripción opcional, fecha de creación. Inmutable.
- **Entrada CxP (modificada)**: se agregan los tipos Auto-Paquete y Reverso Paquete, y el vínculo opcional al paquete que la originó; puede tener monto negativo (solo en reversiones).
- **Orden (modificada)**: se elimina el dato "shipping real a CR"; conserva el envío estimado a CR.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100 % de los paquetes registrados aparece en CxP como deuda en el mismo momento, sin registro manual.
- **SC-002**: Para cada paquete eliminado, la suma neta de su entrada Auto-Paquete y su Reverso Paquete es 0 en la moneda de la orden, y el pendiente de shipping de la orden vuelve a su valor previo.
- **SC-003**: Ninguna orden Activa o Entregando queda fuera de "Shipping CR Pendientes" mientras tenga envío estimado sin cubrir por paquetes.
- **SC-004**: En 0 casos se pueden agregar, eliminar o editar paquetes fuera de los estados permitidos.
- **SC-005**: La transición a Entregada no genera ninguna entrada CxP.

## Assumptions

- "Envío estimado a CR" es el valor de shipping a CR ya calculado en la orden a partir de sus ítems.
- Los paquetes de órdenes Anuladas conservan sus entradas CxP; su corrección, si hace falta, es manual (misma política del requerimiento 009 para anulaciones).
- No se validan paquetes al pasar de Entregando a Entregada.
- Las entradas CxP manuales siguen aceptando solo montos positivos.
- Las 2 órdenes que se pusieron en Entregada por fuera de la app no se corrigen en este requerimiento.
- La actualización de base de datos (tabla de paquetes, vínculo en entradas CxP y eliminación del dato "shipping real a CR") se entrega como migración y se aplica manualmente en cada ambiente antes de desplegar.

## Out of Scope

- Editar paquetes.
- Paquetes en moneda distinta a la de la orden.
- Reversión automática de CxP al anular una orden.
- Autenticación o permisos por usuario.
