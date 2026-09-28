# Feature Specification: Ajustes Visuales de la Pantalla Cuentas por Pagar (CxP)

**Feature Branch**: `012-ajustes-visuales-cxp`

**Created**: 2026-09-28

**Status**: Draft

**Input**: Brainstorm `brainstorm/12-ajustes-visuales-cxp.md`: renombrar indicadores confusos del panel "Control del Mes", mostrar el total colonizado junto a "Entradas del Período" y reorganizar los indicadores en 3 columnas resaltando Deuda a Pagar y Pendiente de Recoger. Solo presentación; no cambian cálculos ni datos.

## Clarifications

### Session 2026-09-28

- Q: ¿El color de fondo de Deuda a Pagar / Pendiente de Recoger cambia si el valor es negativo o cero? → A: No; el color es fijo (rojo y amarillo respectivamente), independiente del signo o del valor.
- Q: ¿Qué íconos usan los recuadros resaltados? → A: A criterio de la implementación, de la librería de íconos ya usada en la app (p. ej. un ícono de factura/dinero para Deuda a Pagar y uno de cartera/monedas para Pendiente de Recoger).
- Q: ¿El texto junto a "Entradas del Período" se muestra también cuando el período está cerrado? → A: Sí; es informativo y se muestra siempre que el panel del período es visible.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Nombres de indicadores sin ambigüedad (Priority: P1)

Al abrir Cuentas por Pagar, el usuario ve el indicador del total de todas las entradas convertido a colones con el nombre "Total por pagar colonizado" (antes "Por pagar en Colones"), y el total de saldos de clientes como "Saldos por Cobrar a Clientes" (antes "Saldos por Cobrar"). Ya no hay dos recuadros con el mismo nombre "Por pagar en Colones" que signifiquen cosas distintas.

**Why this priority**: Es la causa directa de la confusión reportada; sin esto el resto de la reorganización sigue siendo ambiguo.

**Independent Test**: Abrir CxP con un período abierto que tenga entradas en colones y en otra moneda; verificar que existe exactamente un recuadro "Total por pagar colonizado" y que los recuadros por moneda siguen llamándose "Por pagar en {Moneda}"; verificar el texto "Saldos por Cobrar a Clientes".

**Acceptance Scenarios**:

1. **Given** un período abierto con entradas, **When** el usuario abre CxP, **Then** el indicador del total convertido a colones se titula "Total por pagar colonizado" y muestra el mismo valor que antes mostraba "Por pagar en Colones".
2. **Given** un período con entradas en colones y en dólares, **When** el usuario abre CxP, **Then** los recuadros "Por pagar en Colones" y "Por pagar en Dólares" (por moneda, sin conversión) siguen apareciendo con esos nombres y valores.
3. **Given** el panel de indicadores, **When** el usuario lo observa, **Then** el indicador de saldos de clientes se titula "Saldos por Cobrar a Clientes" y su valor no cambia.

---

### User Story 2 - Valores clave resaltados y agrupados en 3 columnas (Priority: P1)

El panel "Control del Mes" organiza sus indicadores en tres columnas. La primera contiene "Deuda a Pagar" (fondo rojo) y "Pendiente de Recoger" (fondo amarillo), resaltados con fondo de color, ícono y número en negrita. La segunda contiene "Shipping CR Pendientes" y "Saldos por Cobrar a Clientes". La tercera contiene "Total por pagar colonizado" y los recuadros "Por pagar en {Moneda}".

**Why this priority**: Deuda a Pagar y Pendiente de Recoger son los valores con los que el usuario opera día a día; deben verse primero.

**Independent Test**: Abrir CxP en pantalla de escritorio y verificar la distribución en 3 columnas y los colores; reducir el ancho de la ventana a tamaño de teléfono y verificar que las columnas se apilan sin desbordes horizontales.

**Acceptance Scenarios**:

1. **Given** una pantalla de escritorio, **When** el usuario abre CxP, **Then** ve tres columnas: (1) Deuda a Pagar y Pendiente de Recoger, (2) Shipping CR Pendientes y Saldos por Cobrar a Clientes, (3) Total por pagar colonizado seguido de los recuadros "Por pagar en {Moneda}".
2. **Given** el panel, **When** el usuario lo observa, **Then** "Deuda a Pagar" tiene fondo rojo y "Pendiente de Recoger" fondo amarillo, ambos con ícono y número en negrita, y se distinguen claramente de los recuadros grises del resto.
3. **Given** un período con entradas en tres monedas, **When** el usuario abre CxP, **Then** la tercera columna muestra "Total por pagar colonizado" y un recuadro por cada moneda, apilados.
4. **Given** una pantalla angosta (teléfono), **When** el usuario abre CxP, **Then** las columnas se muestran una debajo de otra, en el orden 1, 2, 3, sin desplazamiento horizontal.
5. **Given** el panel reorganizado, **When** el usuario lo observa, **Then** "Posición", los campos editables (Tipo de Cambio, Pagos Realizados, En Cuenta), el botón Guardar y el botón "Cerrar Mes" siguen en su lugar y funcionando igual.

---

### User Story 3 - Total colonizado junto a las entradas (Priority: P2)

Junto al título "Entradas del Período" el usuario ve, en la misma línea, el texto "Total por pagar colonizado: ₡…", con el mismo valor del indicador del panel, cerca del botón "Agregar entrada".

**Why this priority**: Relaciona visualmente la lista de entradas con su total; útil pero secundario respecto a la claridad del panel.

**Independent Test**: Abrir CxP, comparar el valor junto a "Entradas del Período" con el indicador "Total por pagar colonizado" del panel; agregar una entrada manual y verificar que ambos valores se actualizan igual.

**Acceptance Scenarios**:

1. **Given** un período abierto, **When** el usuario abre CxP, **Then** junto al título "Entradas del Período" aparece "Total por pagar colonizado: ₡X", donde X es igual al indicador del panel.
2. **Given** la pantalla de CxP, **When** el usuario agrega o elimina una entrada, o guarda un nuevo Tipo de Cambio, **Then** el valor junto a "Entradas del Período" se actualiza al mismo tiempo y con el mismo valor que el indicador del panel.
3. **Given** un período sin entradas o con Tipo de Cambio 0, **When** el usuario abre CxP, **Then** el texto muestra el mismo valor que el indicador del panel (₡0,00 en esos casos).
4. **Given** el indicador del panel, **When** el usuario lo busca, **Then** "Total por pagar colonizado" sigue presente en el panel (no se movió, se duplicó).

---

### Edge Cases

- **Sin entradas en el período**: no hay recuadros "Por pagar en {Moneda}"; la columna 3 muestra solo "Total por pagar colonizado" (₡0,00); el texto junto a Entradas muestra ₡0,00.
- **Tipo de Cambio 0**: se mantiene el aviso amarillo existente; los indicadores convertidos (incluido el texto junto a Entradas) muestran 0 igual que hoy.
- **Muchas monedas**: la columna 3 crece hacia abajo; las columnas 1 y 2 no se estiran de forma extraña.
- **Valores negativos** (p. ej. Pendiente de Recoger negativo porque En Cuenta supera la deuda): se muestran con el formato actual; el color de fondo del recuadro no cambia con el signo.
- **Período cerrado**: la reorganización es igual; los controles de edición se deshabilitan/ocultan como hoy.
- **Sin período (pantalla de inicialización)**: no cambia.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El indicador del panel que muestra la suma de todas las entradas del período convertida a colones MUST titularse "Total por pagar colonizado".
- **FR-002**: Los recuadros dinámicos por moneda MUST mantener su título "Por pagar en {Moneda}" y su valor sin conversión.
- **FR-003**: El indicador de la suma de saldos de clientes MUST titularse "Saldos por Cobrar a Clientes".
- **FR-004**: El panel "Control del Mes" MUST mostrar los indicadores en tres columnas con recuadros apilados verticalmente dentro de cada columna: columna 1 = Deuda a Pagar, Pendiente de Recoger; columna 2 = Shipping CR Pendientes, Saldos por Cobrar a Clientes; columna 3 = Total por pagar colonizado seguido de los recuadros "Por pagar en {Moneda}".
- **FR-005**: "Deuda a Pagar" MUST mostrarse con fondo rojo y "Pendiente de Recoger" con fondo amarillo, ambos con ícono y número en negrita; los demás recuadros MUST conservar el estilo neutro actual.
- **FR-006**: En pantallas angostas las tres columnas MUST apilarse (columna 1 arriba) sin provocar desplazamiento horizontal.
- **FR-007**: Junto al título "Entradas del Período", en la misma línea, MUST mostrarse el texto "Total por pagar colonizado: " seguido del mismo valor y formato de moneda del indicador del panel.
- **FR-008**: El valor junto a "Entradas del Período" MUST actualizarse cada vez que se actualiza el indicador del panel (carga inicial, agregar/eliminar entrada, guardar campos, cerrar mes).
- **FR-009**: "Posición", los campos editables (Tipo de Cambio, Pagos Realizados, En Cuenta), el botón Guardar, el botón "Cerrar Mes", el aviso de Tipo de Cambio en 0, la sección de inicialización y la tabla de entradas MUST NOT cambiar de ubicación ni de comportamiento.
- **FR-010**: Los valores de todos los indicadores MUST NOT cambiar: la funcionalidad es exclusivamente de presentación.

### Key Entities

- **Indicadores del período (CxP)**: valores calculados por período (Total por pagar colonizado, por moneda, Saldos por Cobrar a Clientes, Deuda a Pagar, Pendiente de Recoger, Shipping CR Pendientes, Posición). Sin cambios en su cálculo ni almacenamiento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En la pantalla no existen dos indicadores con el mismo título (0 títulos duplicados con distinto significado).
- **SC-002**: Un usuario identifica "Deuda a Pagar" y "Pendiente de Recoger" como los valores destacados en menos de 3 segundos al abrir la pantalla.
- **SC-003**: El valor junto a "Entradas del Período" coincide con el indicador "Total por pagar colonizado" el 100 % de las veces, incluso después de agregar/eliminar entradas o cambiar el Tipo de Cambio.
- **SC-004**: El 100 % de los valores mostrados es idéntico al de antes del cambio para el mismo período.
- **SC-005**: La pantalla se usa en ancho de teléfono sin desplazamiento horizontal.

## Assumptions

- Se conserva el signo "₡" tal como hoy se muestra en la pantalla; usar el signo de moneda local configurado queda fuera de alcance.
- Los íconos concretos de los recuadros resaltados quedan a criterio de la implementación, usando la librería de íconos ya presente en la aplicación.
- El texto junto a "Entradas del Período" usa el mismo formato de moneda que los indicadores.
- No hay cambios de permisos ni de roles.

## Out of Scope

- Cambios en los cálculos o datos de los indicadores.
- Usar el signo de moneda local configurado en lugar del "₡" fijo.
- Cambios en "Posición", en los campos editables o en la tabla de entradas.
