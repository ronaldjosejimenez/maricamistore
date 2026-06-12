# Feature Specification: Módulo Cuentas por Pagar (CxP)

**Feature Branch**: `009-cuentas-por-pagar`

**Created**: 2026-06-10

**Status**: Draft

**Input**: Módulo de control de cuentas por pagar a proveedores con gestión de períodos de transacción, entradas automáticas desde el flujo de órdenes, y panel de indicadores financieros consolidados.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Ver el estado financiero del período actual (Priority: P1)

El usuario abre la pantalla de Cuentas por Pagar y ve de inmediato el panel de control del mes activo: tipo de cambio, lo que debe por moneda, el saldo total en colones, los cobros pendientes a clientes, los pagos ya realizados, la deuda neta y el indicador de posición financiera global.

**Why this priority**: Sin este panel, el módulo no entrega su valor central: visibilidad financiera consolidada. Todos los demás componentes son alimentación de datos para este indicador.

**Independent Test**: Navegar a `/CxP` con un período abierto y verificar que el panel muestra los indicadores calculados correctamente con datos reales.

**Acceptance Scenarios**:

1. **Given** que existe un período abierto con entradas CxP, **When** el usuario navega a `/CxP`, **Then** el panel superior muestra: Tipo de Cambio, Por pagar por moneda, Por pagar en Colones, Saldos por Cobrar, Pagos Realizados, Deuda a Pagar, En Cuenta, Pendiente de Recoger, Shipping CR Pendientes de Aplicar, y Posición.
2. **Given** que el panel está visible, **When** el usuario observa el indicador "Posición", **Then** aparece en negrita y con fuente mayor que el resto de indicadores.
3. **Given** que el período tiene entradas en USD y CRC, **When** el panel carga, **Then** "Por pagar en USD" y "Por pagar en Colones" se muestran como saldos separados, y "Por pagar en Colones" consolida todos los saldos usando el tipo de cambio del período.
4. **Given** que no existe ningún período abierto, **When** el usuario navega a `/CxP`, **Then** la página muestra un formulario inline con campos Mes (número), Año (número) y Tipo de Cambio inicial (número); al confirmar con datos válidos se crea el primer período y la página recarga mostrando el panel normal.

---

### User Story 2 — Registrar una entrada manual de CxP (Priority: P2)

El usuario necesita registrar una obligación de pago que no proviene de una orden (por ejemplo, una factura de un proveedor local). Hace clic en "+ Agregar entrada", completa el formulario y la entrada aparece en la tabla de la moneda correspondiente.

**Why this priority**: Las entradas manuales cubren casos que el flujo automático de órdenes no captura. Sin ellas el total de CxP es incompleto. Depende de US1 para ser visible.

**Independent Test**: Agregar una entrada manual y verificar que aparece en la tabla de su moneda con el monto y referencia correctos.

**Acceptance Scenarios**:

1. **Given** que el período está abierto, **When** el usuario hace clic en "+ Agregar entrada", **Then** se abre un modal con campos: Texto de referencia (requerido), Moneda (dropdown del catálogo, requerido) y Monto (número positivo, requerido).
2. **Given** que el modal está abierto con datos válidos, **When** el usuario confirma, **Then** la entrada se guarda, el modal se cierra, la tabla de la moneda seleccionada se actualiza y los indicadores del panel se recalculan.
3. **Given** que el período está cerrado, **When** el usuario intenta agregar una entrada, **Then** el botón "+ Agregar entrada" está deshabilitado o ausente y el sistema no permite la operación.
4. **Given** que el usuario deja campos requeridos vacíos, **When** intenta confirmar, **Then** el formulario muestra mensajes de validación por campo faltante sin cerrar el modal.

---

### User Story 3 — Entradas automáticas al cambiar estado de órdenes (Priority: P3)

Cuando una orden pasa a estado **Activa**, el sistema registra automáticamente en CxP el monto que se debe al proveedor. Cuando la orden pasa a **Entregada**, se registra el costo real de shipping a CR, capturado en ese mismo momento.

**Why this priority**: Automatizar los registros más frecuentes elimina el error humano y garantiza que el panel de CxP refleje la deuda real en tiempo real.

**Independent Test**: Activar una orden y verificar que aparece automáticamente una entrada CxP en la pantalla `/CxP` con el monto correcto al proveedor.

**Acceptance Scenarios**:

1. **Given** que existe un período abierto, **When** una orden cambia al estado "Activa", **Then** se crea automáticamente una entrada CxP con referencia al nombre de la orden, monto = total a pagar al proveedor, en la moneda de la orden, de tipo "AutoActiva".
2. **Given** que existe un período abierto, **When** el usuario marca una orden como "Entregada", **Then** el diálogo de transición muestra un campo "Shipping real a CR" pre-llenado con el shipping estimado de la orden y editable.
3. **Given** que el usuario confirma la transición a "Entregada", **When** el modal se cierra, **Then** se crea automáticamente una entrada CxP con el monto de shipping real, en la moneda de la orden, de tipo "AutoDelivered".
4. **Given** que no existe un período abierto al momento de la transición, **When** la orden cambia a "Activa" o "Entregada", **Then** el sistema no crea la entrada CxP y registra un aviso en el log; el cambio de estado de la orden se completa igual.

---

### User Story 4 — Cerrar el mes de transacción (Priority: P4)

Al finalizar el período de trabajo, el usuario ejecuta el cierre del mes. El sistema consolida el saldo pendiente, lo arrastra al nuevo período como "Saldo anterior" y bloquea el período cerrado para edición.

**Why this priority**: El cierre es el mecanismo de control que conecta períodos consecutivos y mantiene la continuidad contable del módulo.

**Independent Test**: Ejecutar el cierre de un mes y verificar que: el período anterior queda bloqueado, existe un nuevo período abierto y la primera entrada del nuevo mes es el "Saldo anterior" con el valor de "Deuda a Pagar" del período cerrado.

**Acceptance Scenarios**:

1. **Given** que el período está abierto, **When** el usuario hace clic en "Cerrar Mes" y confirma, **Then** el período actual queda bloqueado (solo lectura), se crea un nuevo período para el mes siguiente, y la primera entrada del nuevo período es de tipo "SaldoAnterior" con el valor de "Deuda a Pagar" del período cerrado, en Colones.
2. **Given** que el período fue cerrado, **When** el usuario navega a `/CxP`, **Then** el panel muestra el nuevo período activo con el "Saldo anterior" como primera entrada en la tabla de Colones.
3. **Given** que el período está cerrado, **When** el usuario intenta agregar una entrada manual al período cerrado, **Then** la operación es rechazada y las tablas son de solo lectura.
4. **Given** que el "Pendiente de Recoger" es negativo al cerrar, **When** el usuario confirma el cierre, **Then** el cierre procede normalmente (no se bloquea por valor negativo).

---

### User Story 5 — Editar los campos manuales del período (Priority: P5)

Durante el mes, el tipo de cambio puede variar o el usuario puede registrar pagos realizados a proveedores. El usuario puede actualizar estos campos directamente desde el panel de control sin salir de la pantalla.

**Why this priority**: Los campos editables son los únicos insumos manuales del panel de control; sin ellos los indicadores derivados ("Deuda a Pagar", "Posición") no reflejan la realidad.

**Independent Test**: Modificar el tipo de cambio del período y verificar que "Por pagar en Colones" y "Posición" se recalculan inmediatamente.

**Acceptance Scenarios**:

1. **Given** que el período está abierto, **When** el usuario modifica el "Tipo de Cambio" en el panel, **Then** los indicadores "Por pagar en Colones", "Deuda a Pagar", "Pendiente de Recoger" y "Posición" se recalculan con el nuevo valor.
2. **Given** que el período está abierto, **When** el usuario ingresa un valor en "Pagos Realizados" y guarda, **Then** "Deuda a Pagar" y "Posición" reflejan el nuevo valor.
3. **Given** que el período está abierto, **When** el usuario ingresa un valor en "En Cuenta" y guarda, **Then** "Pendiente de Recoger" y "Posición" reflejan el nuevo valor.
4. **Given** que el período está cerrado, **When** el usuario intenta editar los campos del panel, **Then** los campos están deshabilitados y no aceptan cambios.

---

### Edge Cases

- ¿Qué pasa si no existe ningún período abierto al cargar `/CxP`? → La página muestra un formulario inline de inicialización (Mes, Año, Tipo de Cambio). Ver US1 escenario 4 y FR-019.
- ¿Qué pasa si el usuario elimina la entrada SaldoAnterior del período? → La entrada se elimina, los indicadores se recalculan; el saldo anterior ya no contribuye al total de CxP.
- ¿Qué pasa si una orden tiene `ActualShippingAmountToCR` = 0? → Se crea la entrada CxP de tipo AutoDelivered con monto 0 (entrada registrada pero sin impacto financiero).
- ¿Qué pasa si el tipo de cambio es 0 o no está configurado? → Los campos que dependen de conversión muestran 0 con una advertencia visible en el panel.
- ¿Qué pasa si "Deuda a Pagar" es negativa al momento del cierre? → El cierre procede; el "Saldo anterior" del nuevo período tendrá un valor negativo en Colones.
- ¿Qué pasa si el usuario intenta cerrar un mes sin ninguna entrada? → El cierre procede; el "Saldo anterior" del nuevo período será 0 (o el valor de `PagosRealizados` si había pagos sin deuda).
- ¿Qué pasa si hay un error al guardar una entrada automática durante la transición de orden? → La transición de orden se completa; el error en CxP se registra en el log sin bloquear el flujo de la orden.

## Requirements *(mandatory)*

### Functional Requirements

**Gestión de Período:**

- **FR-001**: El sistema DEBE mantener una tabla de períodos de transacción con mes, año, tipo de cambio, pagos realizados, monto en cuenta, e indicador de cierre.
- **FR-002**: El sistema DEBE identificar el "período actual" como el único período con estado abierto.
- **FR-003**: Al ejecutar el cierre de un período, el sistema DEBE: marcar el período como cerrado, calcular la "Deuda a Pagar" final, crear un nuevo período para el mes siguiente, y registrar automáticamente una entrada de tipo "SaldoAnterior" en Colones con el valor de "Deuda a Pagar" en el nuevo período.
- **FR-004**: Un período cerrado NO DEBE permitir creación, edición ni eliminación de sus entradas CxP.
- **FR-005**: El tipo de cambio del período DEBE tomar por defecto el valor del campo `ExchangeRate` de la tabla `Configuration` del sistema, pero DEBE ser editable a nivel de período sin afectar la configuración general.

**Entradas CxP:**

- **FR-006**: El usuario DEBE poder crear entradas manuales en el período abierto especificando: texto de referencia (requerido, texto libre), moneda (requerida, del catálogo existente) y monto (requerido, número positivo).
- **FR-007**: Al cambiar el estado de una orden a "Activa", el sistema DEBE crear automáticamente una entrada CxP en el período abierto con: monto = total a pagar al proveedor, moneda = moneda de la orden, tipo = AutoActiva, referencia = nombre de la orden.
- **FR-008**: Al cambiar el estado de una orden a "Entregada", el sistema DEBE presentar al usuario un campo de "Shipping real a CR" en el diálogo de transición, pre-llenado con el shipping estimado de la orden y editable.
- **FR-009**: Al confirmar la transición a "Entregada", el sistema DEBE crear automáticamente una entrada CxP en el período abierto con: monto = valor del "Shipping real a CR" ingresado por el usuario, moneda = moneda de la orden, tipo = AutoDelivered, referencia = nombre de la orden.
- **FR-010**: El campo "Shipping real a CR" agregado en FR-008 NO DEBE modificar el campo de shipping estimado existente en la orden ni afectar ningún cálculo previo de la orden.
- **FR-011**: Si no existe un período abierto al momento de la transición de orden, el sistema DEBE completar la transición de estado sin crear la entrada CxP, registrando el evento en el log.

**Panel de Indicadores (período abierto):**

- **FR-012**: El sistema DEBE calcular y mostrar los siguientes indicadores para el período abierto:
  - **Por pagar en {Moneda}**: suma de entradas CxP del período agrupadas por moneda.
  - **Por pagar en Colones**: suma de todos los saldos por moneda convertidos a Colones usando el tipo de cambio del período; las entradas ya en Colones se suman directamente.
  - **Saldos por Cobrar**: suma de saldos de clientes de la consulta existente en el sistema (todos en Colones).
  - **Pagos Realizados**: campo editable del período, en Colones.
  - **Deuda a Pagar**: Por pagar en Colones − Pagos Realizados.
  - **En Cuenta**: campo editable del período, en Colones.
  - **Pendiente de Recoger**: Deuda a Pagar − En Cuenta (puede ser negativo).
  - **Shipping CR Pendientes de Aplicar**: suma del shipping estimado de todas las órdenes en estado "Activa", convertido a Colones usando el tipo de cambio del período.
  - **Posición**: Saldos por Cobrar + En Cuenta − Deuda a Pagar − Shipping CR Pendientes de Aplicar.
- **FR-013**: El indicador "Posición" DEBE mostrarse en negrita y con fuente visualmente mayor que los demás indicadores.
- **FR-014**: Los campos "Tipo de Cambio", "Pagos Realizados" y "En Cuenta" DEBEN ser editables en línea en el panel, con guardado explícito.

**Pantalla y Navegación:**

- **FR-015**: La pantalla `/CxP` DEBE mostrar el panel de control del período activo en la sección superior y, debajo, una tabla separada por cada moneda que tenga al menos una entrada en el período, con subtotal por moneda.
- **FR-016**: Cada tabla de entradas DEBE incluir: referencia, tipo (Manual / Auto-Activa / Auto-Entregada / Saldo Anterior), monto con signo de moneda, fecha de creación, y un botón de eliminación por fila.
- **FR-017**: El menú de navegación DEBE incluir una nueva sección "Cuentas por Pagar" con acceso a `/CxP`.
- **FR-018**: El usuario DEBE poder eliminar cualquier entrada CxP del período abierto, incluyendo entradas de tipo AutoActiva, AutoDelivered y SaldoAnterior. Tras la eliminación, el panel de indicadores DEBE recalcularse inmediatamente.
- **FR-019**: Si la tabla de períodos está vacía al acceder a `/CxP`, el sistema DEBE mostrar un formulario inline con campos Mes (número requerido), Año (número requerido) y Tipo de Cambio inicial (número requerido). Al confirmar con datos válidos, se crea el primer PeriodControl y la página recarga mostrando el panel normal.

### Key Entities

- **PeriodControl** (período de transacción): mes de transacción, año de transacción, tipo de cambio, pagos realizados (manual), en cuenta (manual), indicador de cierre. Un único registro tiene estado abierto en todo momento.
- **CxPEntry** (entrada CxP): período al que pertenece, referencia descriptiva, moneda, monto, tipo de entrada (Manual, AutoActiva, AutoDelivered, SaldoAnterior), orden de origen (opcional, solo para entradas automáticas), fecha de creación.
- **Order** (modificación): nuevo campo de shipping real a CR, capturado en la transición a "Entregada". No afecta los cálculos existentes de la orden.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede ver el indicador de "Posición" financiera del mes actual en menos de 5 segundos desde que navega a `/CxP`, sin necesidad de cálculos manuales adicionales.
- **SC-002**: Cero entradas del flujo de órdenes (Activa / Entregada) requieren registro manual en CxP — el 100% se registra automáticamente.
- **SC-003**: El cierre de un período se completa en una sola acción del usuario (clic en "Cerrar Mes" + confirmación) sin pasos adicionales.
- **SC-004**: Los campos editables del panel (Tipo de Cambio, Pagos Realizados, En Cuenta) se guardan y los indicadores dependientes se actualizan en menos de 2 segundos tras la confirmación del usuario.
- **SC-005**: Los períodos cerrados conservan todos sus datos históricos en el sistema indefinidamente, sin límite de almacenamiento impuesto por el módulo.

## Out of Scope

- Pantalla de historial o navegación entre períodos cerrados (los datos se conservan en BD pero no hay UI de consulta en esta versión).
- Reversión automática de entradas CxP al anular una orden (se gestiona mediante eliminación manual).
- Recalculación en cascada del "Saldo anterior" cuando se edita un período cerrado (períodos son inmutables tras el cierre).
- Integración con sistemas contables externos o exportación de datos.
- Aprobaciones o flujos de autorización para el cierre de período.
- Soporte multi-organización en el módulo CxP (aplica la organización activa del usuario).

## Assumptions

- La moneda "Colones" existe en el catálogo de monedas del sistema y puede ser identificada de forma unívoca (por nombre o abreviatura) para las entradas de tipo SaldoAnterior.
- El valor de "Saldos por Cobrar" se obtiene de la misma consulta que ya usa el sistema en la pantalla de Pagos; no requiere nueva lógica de cálculo.
- El tipo de cambio por defecto al crear un nuevo período (ya sea el primero o el generado por cierre de mes) se toma del campo `ExchangeRate` de la tabla `Configuration` del sistema.
- Las órdenes en estado "Activa" al calcular "Shipping CR Pendientes de Aplicar" incluyen solo las del período actual de la organización activa.
- El primer período del sistema se inicializa mediante el formulario inline en `/CxP` con campos Mes, Año y Tipo de Cambio inicial (ver FR-019).
- Las órdenes siguen un flujo unidireccional de estados y no pueden retroceder al estado "Activa" una vez avanzadas a estados posteriores; no se requiere protección contra entradas AutoActiva duplicadas por reactivación.
- Todas las transacciones de tipo `payment` en el cálculo de "Saldos por Cobrar" están en Colones y no requieren conversión.
- La autenticación y autorización de la pantalla CxP sigue el mismo modelo que las páginas existentes del sistema (sesión autenticada requerida).

## Clarifications

### Session 2026-06-10

- Q: ¿Puede el usuario eliminar entradas CxP desde la pantalla del período abierto? → A: Sí, todas las entradas (manuales y automáticas) del período abierto son eliminables desde la UI.
- Q: ¿Cómo inicializa el usuario el primer período si la tabla está vacía? → A: Formulario inline en `/CxP` con campos Mes, Año y Tipo de Cambio inicial; al confirmar se crea el período y la página recarga.
- Q: ¿Qué hace el sistema si una orden vuelve al estado 'Activa' cuando ya tiene una entrada AutoActiva en el período? → A: Las órdenes no pueden retroceder al estado Activa; si ocurriera un problema se gestiona manualmente.
- Q: ¿De dónde toma el sistema el tipo de cambio por defecto al crear un nuevo período? → A: Del campo `ExchangeRate` de la tabla `Configuration` del sistema.
