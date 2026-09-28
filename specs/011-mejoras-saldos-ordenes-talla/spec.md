# Feature Specification: Total de Saldos, Filtro de Estados en Órdenes y Talla en Ítems

**Feature Branch**: `011-mejoras-saldos-ordenes-talla`

**Created**: 2026-09-28

**Status**: Draft

**Input**: Brainstorm `brainstorm/11-mejoras-saldos-ordenes-talla.md`: total neto en la tabla "Saldos de Clientes" (pantalla de Pagos), reemplazo del combo de filtro de estados en Órdenes por una lista de casillas con recuerdo de selección al usar "Volver", y nuevo dato "Talla" en los ítems de orden.

## Clarifications

### Session 2026-09-28

- Q: ¿Qué se muestra cuando el filtro de clientes no coincide con ninguna fila? → A: Se mantiene la tabla con una fila "Ningún cliente coincide con el filtro" y la fila Total muestra 0 (suma de un conjunto vacío).
- Q: ¿Cómo se hace cumplir el máximo de 20 caracteres de la talla? → A: En ambos lados: el campo del formulario no permite escribir más de 20 caracteres y el servidor rechaza con un mensaje de validación cualquier talla que, tras recortar espacios, supere 20 caracteres.
- Q: ¿La fila Total incluye clientes especiales como "Sin Cliente" (Especulativo)? → A: Sí, suma todas las filas visibles sin excepciones; no se excluye ningún cliente.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver el saldo neto total de clientes (Priority: P1)

En la pantalla de Pagos, bajo la tabla "Saldos de Clientes", el usuario ve una fila "Total" con la suma neta de los saldos listados. Los saldos a favor (negativos) restan. Si el usuario escribe en "Filtrar por cliente...", el total se recalcula considerando solo los clientes visibles.

**Why this priority**: Da de inmediato la cifra de cuánto dinero neto está pendiente de cobro, que hoy se calcula a mano. Es el cambio de mayor valor diario y el más sencillo.

**Independent Test**: Con clientes que tienen saldos positivos y negativos, abrir la pantalla de Pagos y comprobar que el total coincide con la suma aritmética de los saldos visibles; luego filtrar por un nombre y comprobar que el total cambia a la suma de las filas restantes.

**Acceptance Scenarios**:

1. **Given** clientes con saldos 10 000, 5 000 y −2 000, **When** el usuario abre la pantalla de Pagos, **Then** la tabla "Saldos de Clientes" muestra al pie una fila "Total" con 13 000, con el mismo formato de moneda que las filas.
2. **Given** la tabla con varios clientes, **When** el usuario escribe un texto en "Filtrar por cliente...", **Then** el total muestra la suma neta solo de los clientes que quedan visibles.
3. **Given** un filtro que no coincide con ningún cliente, **When** la tabla queda sin filas, **Then** la tabla muestra una fila "Ningún cliente coincide con el filtro" y la fila Total muestra 0.
4. **Given** la tabla con su total visible, **When** el usuario registra un pago y la tabla se recarga, **Then** el total refleja el nuevo saldo del cliente.
5. **Given** que la suma neta es negativa, **When** se muestra el total, **Then** se presenta con la misma convención visual de saldo negativo (a favor) que usan las filas.

---

### User Story 2 - Filtrar órdenes por cualquier combinación de estados (Priority: P2)

En la pantalla de Órdenes, el combo de filtro de estado se reemplaza por una lista de casillas con los seis estados posibles: Pendiente, Activa, Entregando, Entregada, Completada y Anulada. Al entrar a la pantalla, solo Pendiente y Activa están marcadas. Cada vez que el usuario marca o desmarca un estado, la lista de órdenes se actualiza automáticamente.

**Why this priority**: Permite combinaciones que hoy son imposibles (por ejemplo, Pendiente + Entregada) sin cambiar el comportamiento por defecto al que el usuario ya está acostumbrado.

**Independent Test**: Abrir Órdenes desde el menú, confirmar que solo Pendiente y Activa están marcadas y la lista coincide; marcar Entregada y confirmar que aparecen también las órdenes entregadas sin presionar ningún botón; desmarcar todo y confirmar que la lista queda vacía.

**Acceptance Scenarios**:

1. **Given** que el usuario entra a Órdenes desde el menú, **When** la pantalla carga, **Then** se ven seis casillas con las etiquetas en español de los estados, solo Pendiente y Activa están marcadas, y la lista muestra solo órdenes en esos estados.
2. **Given** la pantalla de Órdenes, **When** el usuario marca o desmarca cualquier casilla, **Then** la lista se recarga automáticamente mostrando solo las órdenes cuyos estados están marcados.
3. **Given** que el usuario desmarca todas las casillas, **When** la lista se recarga, **Then** no se muestra ninguna orden (lista vacía con su mensaje habitual), no todas las órdenes.
4. **Given** la pantalla de Órdenes, **When** el usuario la observa, **Then** el antiguo combo de filtro de estado ya no existe.

---

### User Story 3 - Conservar la selección de estados al volver de los ítems (Priority: P2)

Cuando el usuario abre los ítems de una orden desde la lista y luego presiona "Volver", regresa a Órdenes con las mismas casillas de estado que tenía marcadas. Si entra a Órdenes por cualquier otra vía (menú, enlace directo), se usan los estados por defecto.

**Why this priority**: Sin esto, el nuevo filtro se pierde en el flujo más común (revisar ítems de varias órdenes seguidas), obligando a remarcar las casillas cada vez.

**Independent Test**: Marcar Entregada y Completada (desmarcando las demás), entrar a los ítems de una orden, presionar "Volver" y confirmar que siguen marcadas exactamente Entregada y Completada; luego entrar a Órdenes desde el menú y confirmar que vuelven Pendiente y Activa.

**Acceptance Scenarios**:

1. **Given** una selección de estados distinta a la por defecto, **When** el usuario abre los ítems de una orden y presiona "Volver", **Then** la pantalla de Órdenes muestra marcados exactamente los mismos estados y la lista filtrada correspondiente.
2. **Given** cualquier selección previa, **When** el usuario entra a Órdenes desde el menú de navegación, **Then** solo Pendiente y Activa están marcadas.
3. **Given** que la dirección de regreso contiene estados desconocidos o mal escritos, **When** la pantalla de Órdenes carga, **Then** los valores desconocidos se ignoran; si no queda ningún estado válido y se indicaron valores, se usan los estados por defecto.

---

### User Story 4 - Registrar la talla de una prenda en el ítem (Priority: P3)

Al crear o editar un ítem de una orden, el usuario puede escribir la talla de la prenda en un campo de texto libre "Talla" (por ejemplo "M", "38", "10-12 años"). El campo es opcional y admite hasta 20 caracteres.

**Why this priority**: Evita mezclar la talla dentro de la descripción del producto. Es útil pero no bloquea la operación diaria.

**Independent Test**: Crear un ítem con talla "XL", reabrirlo en edición y comprobar que conserva "XL"; editarlo dejando la talla vacía y comprobar que se guarda sin error.

**Acceptance Scenarios**:

1. **Given** el formulario de crear ítem, **When** el usuario escribe "XL" en Talla y guarda, **Then** el ítem se guarda y al abrirlo en edición el campo Talla muestra "XL".
2. **Given** el formulario de crear o editar ítem, **When** el usuario deja Talla vacía y guarda, **Then** el ítem se guarda sin error con talla vacía.
3. **Given** el formulario de ítem, **When** el usuario intenta ingresar más de 20 caracteres en Talla, **Then** el campo no permite escribir más de 20 caracteres; y si llegara al servidor una talla de más de 20 caracteres (tras recortar espacios), se rechaza con un mensaje de validación y no se guarda.
4. **Given** ítems creados antes de esta funcionalidad, **When** el usuario los abre en edición, **Then** el campo Talla aparece vacío y el ítem puede guardarse normalmente.
5. **Given** la tabla de ítems de una orden, **When** el usuario la observa, **Then** no se agrega ninguna columna de talla (la talla solo se ve en el formulario).
6. **Given** el usuario escribe " M " con espacios al inicio o final, **When** guarda, **Then** la talla se almacena como "M".

---

### Edge Cases

- **Total con filtro sin coincidencias**: la tabla muestra la fila "Ningún cliente coincide con el filtro" y el Total en 0; nunca queda mostrando la suma anterior.
- **Total cero**: si los saldos visibles se compensan exactamente, se muestra 0 con el formato de moneda.
- **Total incluye clientes especiales**: el total suma todas las filas visibles de la tabla tal como se listan hoy (incluido el cliente genérico "Sin Cliente" si aparece en la tabla).
- **Ninguna casilla marcada**: lista de órdenes vacía (no hay órdenes desde las cuales navegar a ítems, así que no se hereda una selección vacía por el flujo normal). Si la dirección de Órdenes indica explícitamente una selección vacía, se respeta y la lista queda vacía.
- **Estados inválidos en la dirección de regreso**: se ignoran; si todos son inválidos, se usan los estados por defecto.
- **Recargas rápidas**: si el usuario marca/desmarca varias casillas rápidamente, la lista final debe corresponder a la última selección, no a una respuesta anterior.
- **Talla solo con espacios**: se guarda como talla vacía.
- **Reasignación de ítems (feature 010)**: reasignar cliente o ajustar precio de un ítem no debe borrar ni alterar su talla.

## Requirements *(mandatory)*

### Functional Requirements

**Total de Saldos de Clientes (pantalla de Pagos)**

- **FR-001**: La tabla "Saldos de Clientes" MUST mostrar al pie una fila "Total" con la suma neta de los saldos de las filas visibles, donde los saldos negativos (a favor) restan.
- **FR-002**: El total MUST recalcularse cada vez que cambia el texto de "Filtrar por cliente...", considerando solo los clientes visibles.
- **FR-003**: El total MUST recalcularse cuando la tabla se recarga (por ejemplo, tras registrar un pago).
- **FR-004**: El total MUST usar el mismo formato de moneda local y la misma convención visual para montos negativos que las filas de la tabla.
- **FR-005**: Cuando el filtro no deja filas visibles, la tabla MUST mostrar una fila "Ningún cliente coincide con el filtro" y el Total MUST mostrar 0.

**Filtro de estados en Órdenes**

- **FR-006**: La pantalla de Órdenes MUST reemplazar el combo de filtro de estado por una lista de casillas, una por cada uno de los seis estados de orden, con sus etiquetas en español (Pendiente, Activa, Entregando, Entregada, Completada, Anulada).
- **FR-007**: Al entrar a Órdenes sin selección heredada, solo Pendiente y Activa MUST estar marcadas.
- **FR-008**: Marcar o desmarcar una casilla MUST recargar automáticamente la lista de órdenes, mostrando únicamente órdenes cuyo estado está marcado.
- **FR-009**: Si no hay ninguna casilla marcada, la lista MUST quedar vacía.
- **FR-010**: El acceso a los ítems de una orden desde la lista MUST llevar consigo la selección de estados vigente, y el botón "Volver" de la pantalla de ítems MUST regresar a Órdenes restaurando exactamente esa selección.
- **FR-011**: Entrar a Órdenes por cualquier vía distinta de "Volver" (menú, dirección sin selección) MUST usar la selección por defecto (Pendiente y Activa).
- **FR-012**: Valores de estado desconocidos en la selección heredada MUST ignorarse; si se indicaron valores pero ninguno es válido, MUST usarse la selección por defecto.
- **FR-013**: Cuando varias recargas se solapan, la lista mostrada MUST corresponder a la selección más reciente.

**Talla en ítems de orden**

- **FR-014**: Cada ítem de orden MUST tener un dato "Talla" de texto que nunca es nulo; su valor por defecto es vacío.
- **FR-015**: Los formularios de crear y editar ítem MUST incluir un campo de texto libre "Talla", opcional, con un máximo de 20 caracteres.
- **FR-016**: El campo Talla MUST limitar la escritura a 20 caracteres; el servidor MUST eliminar espacios al inicio y al final y MUST rechazar con un mensaje de validación cualquier talla resultante de más de 20 caracteres.
- **FR-017**: Los ítems existentes MUST quedar con talla vacía tras la actualización, sin pérdida de otros datos.
- **FR-018**: La talla MUST NOT mostrarse en la tabla de ítems ni en otras pantallas; solo en el formulario de crear/editar ítem.
- **FR-019**: Las operaciones existentes sobre ítems que no editan la talla (por ejemplo, reasignación de cliente o ajuste de precio) MUST conservar la talla almacenada.

### Key Entities

- **Ítem de orden (OrderItem)**: prenda o producto dentro de una orden, asignado a un cliente. Se agrega el atributo **Talla**: texto libre, no nulo, vacío por defecto, máximo 20 caracteres. En el futuro podría relacionarse con un catálogo de tallas por tipo de producto (fuera de alcance).
- **Estado de orden (OrderStatus)**: catálogo fijo de seis estados (Pendiente, Activa, Entregando, Entregada, Completada, Anulada) usado para filtrar la lista de órdenes. Sin cambios en su definición.
- **Saldo de cliente**: saldo por cliente ya calculado y mostrado en la pantalla de Pagos. Sin cambios en su cálculo; solo se agrega su suma neta visible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario conoce el saldo neto pendiente de todos (o de un grupo filtrado de) clientes en 0 cálculos manuales: la cifra se lee directamente en la pantalla, y coincide al 100 % con la suma aritmética de los saldos visibles.
- **SC-002**: El usuario puede ver en la lista cualquier combinación de los seis estados de orden (63 combinaciones no vacías) con a lo sumo un clic por estado a cambiar, sin presionar botones adicionales.
- **SC-003**: Tras revisar los ítems de una orden y presionar "Volver", el 100 % de las veces la selección de estados es idéntica a la que el usuario tenía antes.
- **SC-004**: El usuario registra la talla de una prenda en el mismo formulario del ítem, sin pasos adicionales, y el 100 % de los ítems existentes siguen abriéndose y guardándose sin error tras la actualización.

## Assumptions

- La pantalla de Pagos ya calcula y muestra los saldos por cliente; esta funcionalidad solo suma lo que la tabla ya muestra y no cambia el cálculo de cada saldo.
- El total neto incluye todas las filas visibles de la tabla tal como se listan hoy, sin excluir clientes especiales.
- La selección de estados se recuerda únicamente a través del flujo "lista → ítems → Volver" usando la dirección de la página; no se guarda en el navegador ni entre sesiones.
- Una selección heredada presente pero vacía significa "ninguna casilla marcada" (se respeta la elección del usuario); la ausencia de selección heredada significa "usar los estados por defecto".
- La talla es independiente del tipo de producto; no se valida contra ninguna lista de valores.
- El campo "Talla" aparece en el mismo formulario/modal de crear y editar ítem que ya existe; no hay otros puntos de creación de ítems que requieran el campo.
- Los usuarios y permisos son los mismos que ya acceden a Pagos y Órdenes; no hay cambios de seguridad.

## Out of Scope

- Totales separados de "por cobrar" y "a favor" en la pantalla de Pagos.
- Guardar la selección de estados entre sesiones o al entrar desde el menú.
- Catálogo de tallas, validación de valores de talla o relación con el tipo de producto.
- Mostrar la talla en la tabla de ítems, reportes, transacciones u otras pantallas.
