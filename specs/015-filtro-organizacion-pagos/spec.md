# Feature Specification: Filtro de Organización en Payments

**Feature Branch**: `015-filtro-organizacion-pagos`

**Created**: 2026-09-29

**Status**: Draft

**Input**: Brainstorm `brainstorm/15-filtro-organizacion-pagos.md`: agregar un combo de Organización ("Todas" + lista) en la pantalla Payments que determina el alcance de los saldos mostrados y la organización a la que se asocia un pago registrado; con "Todas" seleccionado, el registro de pagos queda bloqueado con una leyenda explicativa.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Elegir la organización del filtro (Priority: P1)

Al abrir Payments, el usuario ve un combo de Organización arriba de la tarjeta "Registrar Pago", con "Todas" como primera opción seguida de todas las organizaciones existentes. Al cargar la pantalla, el combo tiene preseleccionada la organización activa de la sesión.

**Why this priority**: Es el control base sobre el que dependen todas las demás historias; sin el combo no hay nada que filtrar.

**Independent Test**: Abrir Payments y verificar que el combo muestra "Todas" primero, luego las organizaciones existentes, y que la organización de la sesión queda preseleccionada.

**Acceptance Scenarios**:

1. **Given** que el usuario abre Payments, **When** la pantalla carga, **Then** aparece un combo de Organización arriba de la tarjeta "Registrar Pago" con "Todas" como primera opción y luego el resto de las organizaciones existentes.
2. **Given** que el combo termina de cargar, **When** el usuario lo observa, **Then** la organización activa de la sesión aparece preseleccionada (no "Todas").
3. **Given** el combo cargado, **When** el usuario elige otra opción, **Then** la selección cambia sin recargar la página completa.

---

### User Story 2 - Ver saldos y saldo del cliente según la organización elegida (Priority: P1)

El usuario elige una organización específica en el filtro. La tabla "Saldos de Clientes" y, si hay un cliente seleccionado, la fila "Saldo Esta Org." de la tarjeta "Saldo del Cliente" reflejan únicamente esa organización, sin importar si es o no la organización activa de la sesión. Si el usuario vuelve a elegir "Todas", ambos vuelven a mostrar el alcance global de siempre.

**Why this priority**: Es el valor central del filtro: poder consultar información de cualquier organización sin tener que cambiar la organización activa de toda la aplicación.

**Independent Test**: Con transacciones de prueba en al menos dos organizaciones para un mismo cliente, elegir cada organización en el filtro y verificar que "Saldos de Clientes" y "Saldo Esta Org." muestran solo los montos de esa organización; elegir "Todas" y verificar que vuelven a sumar todas las organizaciones.

**Acceptance Scenarios**:

1. **Given** que el usuario elige una organización específica en el filtro, **When** la tabla "Saldos de Clientes" se recalcula, **Then** solo se consideran las transacciones de esa organización (pueda o no ser la de la sesión).
2. **Given** una organización específica elegida y un cliente seleccionado en el formulario, **When** se muestra la tarjeta "Saldo del Cliente", **Then** "Saldo Esta Org." refleja esa organización y "Saldo Global" sigue sumando todas las organizaciones sin cambios.
3. **Given** "Todas" elegido en el filtro, **When** se muestran "Saldos de Clientes" y "Saldo Esta Org.", **Then** ambos reflejan el alcance global (todas las organizaciones), igual que el comportamiento anterior a este cambio; "Saldo Esta Org." coincide con "Saldo Global".
4. **Given** que el usuario cambia el filtro mientras ya hay un cliente elegido, **When** el cambio se aplica, **Then** la tarjeta "Saldo del Cliente" se recalcula automáticamente para la nueva organización elegida, sin que el usuario tenga que volver a elegir el cliente.
5. **Given** una organización específica sin ninguna transacción, **When** se consultan sus saldos, **Then** "Saldos de Clientes" muestra el estado de "sin saldos" para esa organización (no la lista global).

---

### User Story 3 - Registrar un pago contra la organización elegida (Priority: P1)

Con una organización específica elegida en el filtro, el usuario registra un pago para un cliente. La transacción generada queda asociada a esa organización, sea o no la organización activa de la sesión.

**Why this priority**: Es la otra mitad del valor del filtro: permitir registrar pagos "a nombre de" una organización distinta a la activa sin cambiarla para toda la aplicación.

**Independent Test**: Con una organización específica elegida (distinta a la de la sesión), registrar un pago y verificar que la transacción y el nuevo saldo quedan asociados a esa organización, no a la de la sesión.

**Acceptance Scenarios**:

1. **Given** una organización específica elegida en el filtro, **When** el usuario registra un pago válido, **Then** la transacción se crea con esa organización, independientemente de cuál sea la organización activa de la sesión.
2. **Given** que el pago se registró correctamente, **When** la pantalla se actualiza, **Then** "Saldos de Clientes" y la tarjeta "Saldo del Cliente" reflejan el pago dentro del alcance de la organización elegida.
3. **Given** los datos de un pago inválidos (sin cliente o monto ≤ 0), **When** el usuario intenta registrar, **Then** se muestra el mensaje de validación existente y no se crea ninguna transacción (comportamiento actual, sin cambios).

---

### User Story 4 - Bloqueo del registro cuando el filtro está en "Todas" (Priority: P2)

Mientras el filtro está en "Todas", el botón "Registrar Pago" aparece deshabilitado y junto a él se explica por qué, para obligar al usuario a elegir a conciencia la organización a la que se le va a registrar el pago. El resto del formulario (cliente, monto, tarjeta de saldo) sigue disponible.

**Why this priority**: Evita que un pago quede registrado "por defecto" en la organización de la sesión sin que el usuario lo haya decidido explícitamente; depende del filtro de la Historia 1 y complementa la Historia 3.

**Independent Test**: Con "Todas" seleccionado, verificar que el botón "Registrar Pago" está deshabilitado y se ve la leyenda explicativa; elegir una organización específica y verificar que el botón se habilita y la leyenda desaparece; intentar registrar el pago llamando directamente al servidor sin organización específica y verificar que se rechaza igual.

**Acceptance Scenarios**:

1. **Given** que el filtro está en "Todas", **When** el usuario observa el formulario "Registrar Pago", **Then** el botón "Registrar Pago" está deshabilitado y se muestra la leyenda "Seleccione una organización específica para poder registrar un pago.".
2. **Given** que el botón está deshabilitado por "Todas", **When** el usuario elige una organización específica, **Then** el botón se habilita y la leyenda desaparece, sin afectar al cliente o monto ya ingresados.
3. **Given** que el filtro está en "Todas", **When** el usuario elige un cliente en el formulario, **Then** la tarjeta "Saldo del Cliente" se sigue mostrando con normalidad (el bloqueo es solo sobre la acción de registrar).
4. **Given** un intento de registrar un pago sin especificar una organización (por ejemplo, invocando el servidor directamente sin pasar por el botón), **When** el sistema lo procesa, **Then** se rechaza con un mensaje de validación y no se crea ninguna transacción.

---

### Edge Cases

- **Combo sin organizaciones**: si por algún motivo no existiera ninguna organización dada de alta (caso extremo, no debería ocurrir porque la organización de la sesión ya existe), el combo muestra únicamente "Todas".
- **Cambiar el filtro varias veces rápido**: la tabla de saldos y la tarjeta del cliente deben terminar reflejando la última organización elegida, no una intermedia.
- **Cliente sin saldo en la organización elegida**: la tarjeta "Saldo del Cliente" muestra 0 para "Saldo Esta Org." si el cliente no tiene transacciones en esa organización, aunque sí tenga saldo global.
- **La organización activa de la sesión cambia en otra pantalla** mientras Payments sigue abierto en otra pestaña: no afecta la selección ya hecha en el filtro de esa pestaña; solo el próximo `OnGet` (recarga de Payments) vuelve a proponer la organización de sesión vigente en ese momento como valor por defecto.
- **"Todas" y luego elegir de nuevo la organización de la sesión**: el botón se habilita igual que con cualquier otra organización específica; no hay tratamiento especial por coincidir con la de la sesión.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La pantalla Payments MUST mostrar un combo de Organización ubicado arriba de la tarjeta "Registrar Pago".
- **FR-002**: El combo MUST listar "Todas" como primera opción, seguida de todas las organizaciones existentes (sin distinción de activo/inactivo, ya que el sistema no tiene ese concepto).
- **FR-003**: Al cargar la pantalla, el combo MUST tener preseleccionada la organización activa de la sesión del usuario.
- **FR-004**: Cambiar la selección del combo MUST NOT modificar la organización activa de la sesión (usada por el resto de la aplicación); el filtro es exclusivo de esta pantalla y de esta carga de página.
- **FR-005**: Con una organización específica seleccionada, "Saldos de Clientes" MUST calcularse únicamente con las transacciones de esa organización.
- **FR-006**: Con una organización específica seleccionada, "Saldo Esta Org." en la tarjeta "Saldo del Cliente" MUST reflejar esa organización; "Saldo Global" MUST seguir sumando todas las organizaciones sin cambios.
- **FR-007**: Con "Todas" seleccionado, "Saldos de Clientes" MUST calcularse sin distinguir organización (comportamiento existente, sin cambios).
- **FR-008**: Con "Todas" seleccionado, "Saldo Esta Org." en la tarjeta "Saldo del Cliente" MUST coincidir con "Saldo Global".
- **FR-009**: Cambiar el combo MUST volver a calcular "Saldos de Clientes" y, si hay un cliente elegido, la tarjeta "Saldo del Cliente", sin recargar la página completa.
- **FR-010**: Con una organización específica seleccionada, registrar un pago MUST crear la transacción asociada a esa organización, sea o no la organización activa de la sesión.
- **FR-011**: Con "Todas" seleccionado, el botón "Registrar Pago" MUST estar deshabilitado y MUST mostrarse una leyenda indicando que se debe elegir una organización específica para registrar el pago.
- **FR-012**: El bloqueo de FR-011 MUST NOT afectar la posibilidad de elegir un cliente ni de ver su tarjeta de saldo; solo bloquea la acción de registrar el pago.
- **FR-013**: El servidor MUST rechazar el registro de un pago que no incluya una organización específica, incluso si la solicitud no pasa por el botón del formulario.
- **FR-014**: Al elegir una organización específica después de tener "Todas" seleccionado, el botón "Registrar Pago" MUST habilitarse y la leyenda de FR-011 MUST dejar de mostrarse.

### Key Entities

- **Organización (Organization)**: sin cambios en su estructura; se usa como valor de filtro en Payments, independiente de la organización activa de la sesión.
- **Transacción (Transaction)**: ya tiene una organización asociada; con este cambio, esa organización puede ser la elegida en el filtro de Payments (no necesariamente la de la sesión) al momento de registrar un pago.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede consultar los saldos de cualquier organización existente desde Payments sin cambiar la organización activa de la sesión, en un solo cambio de combo.
- **SC-002**: El usuario puede registrar un pago contra cualquier organización existente desde Payments sin cambiar la organización activa de la sesión.
- **SC-003**: El 100% de los pagos registrados mientras el filtro está en "Todas" resulta bloqueado en la interfaz y rechazado por el servidor si se intenta de otra forma; 0 transacciones se crean sin una organización específica elegida.
- **SC-004**: El 100% de las cargas de la pantalla Payments preseleccionan la organización activa de la sesión en el combo, sin intervención del usuario.

## Assumptions

- El modelo `Organization` no tiene ni necesita un campo de activo/inactivo; el combo lista todas las organizaciones existentes.
- El filtro de Payments no se persiste entre cargas de la pantalla; siempre vuelve a proponer la organización de la sesión como valor por defecto.
- "Saldo Global" (todas las organizaciones) no cambia con este requerimiento; solo cambia el alcance de "Saldo Esta Org." y de "Saldos de Clientes" cuando se elige una organización específica.
- El listado de clientes disponibles para registrar un pago no depende de la organización elegida en el filtro (los clientes no están asociados a una organización específica).
- No hay cambios de permisos ni de roles (la autenticación es un pendiente aparte).

## Out of Scope

- Cambiar la organización activa de la sesión desde Payments (eso sigue haciéndose desde la pantalla Organizaciones y afecta a toda la aplicación).
- Agregar un concepto de organizaciones activas/inactivas al modelo `Organization`.
- Cambios en CxP, Órdenes u otras pantallas con lógica por organización.
- Persistir la selección del filtro entre visitas a la pantalla.
- Autenticación o permisos por usuario.
