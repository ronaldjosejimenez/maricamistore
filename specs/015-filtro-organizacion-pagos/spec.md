# Feature Specification: Ver Saldo Completo del Cliente en Payments

**Feature Branch**: `015-filtro-organizacion-pagos`

**Created**: 2026-09-29

**Status**: Implemented

**Input**: Brainstorm `brainstorm/15-filtro-organizacion-pagos.md`. **Revisado el 2026-09-29** después del primer despliegue: la versión original agregaba un combo para elegir *cualquier* organización (no solo la de la sesión), tanto para ver saldos como para registrar pagos, con un bloqueo del botón cuando el combo estaba en "Todas". Tras probarlo en producción, se identificó un problema de diseño: mezclar en un mismo control una decisión de solo lectura ("qué estoy consultando") con una de escritura ("a qué organización se registra el pago") es una fuente de error silencioso — el usuario podría dejar el filtro en otra organización y registrar sin querer un pago ahí. El usuario confirmó que **nunca** se registran pagos a una organización distinta de la sesión; el único objetivo real era poder consultar el saldo total del cliente (todas las organizaciones) además del de su organización. Esta versión de la spec reemplaza el diseño original con esa necesidad, mucho más simple.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver el saldo completo de un cliente en la tabla (Priority: P1)

En la tabla "Saldos de Clientes" el usuario tiene una casilla "Ver saldo completo del cliente", junto al filtro de texto por nombre. Desmarcada (por defecto), la tabla solo considera transacciones de la organización de la sesión. Marcada, la tabla considera todas las organizaciones.

**Why this priority**: Es el único valor real que se necesitaba: saber si un cliente le debe (o le sobra saldo) considerando todo el negocio, no solo la organización activa.

**Independent Test**: Con un cliente que tiene transacciones en dos organizaciones, desmarcar la casilla y ver que su saldo en la tabla es solo el de la organización de la sesión; marcarla y ver que el saldo pasa a ser la suma de todas las organizaciones.

**Acceptance Scenarios**:

1. **Given** que el usuario abre Payments, **When** la pantalla carga, **Then** la casilla "Ver saldo completo del cliente" aparece junto al filtro de texto de "Saldos de Clientes", desmarcada.
2. **Given** la casilla desmarcada, **When** se calcula "Saldos de Clientes", **Then** solo se consideran las transacciones de la organización de la sesión (mismo resultado que antes de que existiera este requerimiento).
3. **Given** el usuario marca la casilla, **When** la tabla se recalcula, **Then** se consideran las transacciones de todas las organizaciones, sin recargar la página completa.
4. **Given** la casilla marcada, **When** el usuario la desmarca, **Then** la tabla vuelve a mostrar solo la organización de la sesión.
5. **Given** cualquier estado de la casilla, **When** el usuario escribe en el filtro de texto, **Then** el filtro de texto sigue operando sobre las filas ya cargadas (sin cambios respecto al comportamiento existente).

---

### Edge Cases

- **Cliente sin transacciones en la organización de la sesión pero sí en otra**: con la casilla desmarcada no aparece en la tabla (o aparece con saldo 0); al marcarla sí aparece con su saldo completo.
- **Cambiar la casilla varias veces rápido**: la tabla debe terminar reflejando el último estado de la casilla, no uno intermedio (se descartan las respuestas de peticiones anteriores en curso).
- **La tarjeta "Saldo del Cliente"** (al elegir un cliente en el formulario de "Registrar Pago") **no depende de esta casilla**: sigue mostrando siempre "Saldo Global" (todas las organizaciones) y "Saldo Esta Org." (la organización de la sesión), exactamente como antes de este requerimiento.
- **Registrar un pago no depende de esta casilla en ningún caso**: la transacción siempre se crea en la organización de la sesión.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La pantalla Payments MUST mostrar una casilla "Ver saldo completo del cliente" junto al filtro de texto de la tabla "Saldos de Clientes".
- **FR-002**: Por defecto (al cargar la pantalla) la casilla MUST estar desmarcada.
- **FR-003**: Con la casilla desmarcada, "Saldos de Clientes" MUST calcularse únicamente con transacciones de la organización activa de la sesión (comportamiento idéntico al que existía antes de este requerimiento).
- **FR-004**: Con la casilla marcada, "Saldos de Clientes" MUST calcularse con transacciones de todas las organizaciones.
- **FR-005**: Cambiar la casilla MUST volver a calcular "Saldos de Clientes" sin recargar la página completa.
- **FR-006**: Esta casilla MUST NOT afectar la tarjeta "Saldo del Cliente" (Saldo Global / Saldo Esta Org.) ni la organización a la que se asocia un pago registrado.
- **FR-007**: Registrar un pago MUST seguir creando la transacción con la organización activa de la sesión, en todos los casos, sin excepción.
- **FR-008**: El filtro de texto "Filtrar por cliente..." de "Saldos de Clientes" MUST ser visualmente más notorio que antes de este requerimiento (por ejemplo, con un ícono de búsqueda).

### Key Entities

- **Organización (Organization)**: sin cambios. Ya no se usa como valor elegible en Payments; la única fuente de organización sigue siendo la de la sesión.
- **Transacción (Transaction)**: sin cambios; siempre se crea con la organización de la sesión al registrar un pago.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede ver el saldo completo (todas las organizaciones) o el de su organización de un vistazo, con un solo clic en la casilla.
- **SC-002**: El 100% de los pagos registrados quedan asociados a la organización de la sesión, sin ninguna forma de registrar a otra organización desde esta pantalla.
- **SC-003**: El filtro de texto por cliente es identificable sin esfuerzo (con ícono) en la tabla de saldos.

## Assumptions

- No existe ni se necesita ninguna forma de registrar un pago a una organización distinta de la de la sesión.
- La tarjeta "Saldo del Cliente" ya resuelve por sí sola la necesidad de ver ambos alcances (global y de la organización) para un cliente puntual; no necesita ningún control adicional.
- No hay cambios de permisos ni de roles (la autenticación es un pendiente aparte).

## Out of Scope

- Elegir una organización específica distinta a la de la sesión, para ver saldos o para registrar pagos (se descarta por completo del diseño original).
- Cambiar la organización activa de la sesión desde Payments.
- Agregar un concepto de organizaciones activas/inactivas al modelo `Organization`.
- Cambios en CxP, Órdenes u otras pantallas con lógica por organización.
- Autenticación o permisos por usuario.

## Revision History

- **2026-09-29 (versión original, ya reemplazada)**: combo "Todas" + lista de organizaciones; saldos y registro de pagos según la organización elegida; bloqueo del botón "Registrar Pago" con "Todas" seleccionado. Implementado, revisado y desplegado (PR #27, release a `main`).
- **2026-09-29 (esta versión)**: reemplaza el combo por una casilla de dos estados, limitada a la tabla "Saldos de Clientes"; se elimina por completo la posibilidad de registrar pagos a una organización distinta de la sesión y, con ella, el bloqueo del botón.
