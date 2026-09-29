# Feature Specification: Cierre de Mes de CxP — Reglas, Vista Previa y Protecciones

**Feature Branch**: `014-cierre-mes-cxp`

**Created**: 2026-09-28

**Status**: Draft

**Input**: Brainstorm `brainstorm/14-cierre-mes-cxp.md`: nuevas reglas del cierre de mes (Saldo anterior = Pendiente de Recoger si es positivo; En Cuenta del mes nuevo = lo que queda después de pagar la deuda), diálogo de vista previa antes de cerrar, tipo de cambio siempre mayor a cero, sin montos negativos, protección contra doble cierre y ajustes visuales del panel de Cuentas por Pagar.

## Clarifications

### Session 2026-09-28

- Q: ¿Qué pasa con En Cuenta del mes nuevo si la Deuda a Pagar es negativa (sobrepago)? → A: En la operación nunca se paga de más: al cerrar, la deuda se paga con lo que hay En Cuenta; lo que sobra queda En Cuenta y lo que no alcanza es el Saldo anterior. Si por error la Deuda a Pagar fuera negativa, se trata como 0 (En Cuenta pasa igual, no se suma).
- Q: ¿La entrada Saldo anterior se puede editar en el diálogo de cierre? → A: No; se muestra en solo lectura (monto calculado o "Sin saldo anterior"). Solo el Tipo de Cambio y En Cuenta del mes nuevo son editables; cualquier ajuste se hace después con una entrada manual en el mes nuevo.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cerrar el mes con las nuevas reglas de arrastre (Priority: P1)

Al cerrar el mes se asume que la deuda del mes se paga con el dinero que hay En Cuenta. Si el dinero no alcanza, lo que falta (Pendiente de Recoger) pasa al mes nuevo como "Saldo anterior". Si alcanza, lo que sobra pasa al mes nuevo como En Cuenta.

**Why this priority**: Es la regla de negocio central; hoy el dinero En Cuenta se pierde y el Saldo anterior arrastra toda la deuda.

**Independent Test**: Con Deuda a Pagar ₡500.000 y En Cuenta ₡200.000, cerrar el mes y verificar que el mes nuevo tiene Saldo anterior ₡300.000 y En Cuenta ₡0; repetir con En Cuenta ₡700.000 y verificar que no hay Saldo anterior y En Cuenta es ₡200.000.

**Acceptance Scenarios**:

1. **Given** Deuda a Pagar ₡500.000 y En Cuenta ₡200.000 (Pendiente de Recoger ₡300.000), **When** se cierra el mes, **Then** el mes nuevo tiene una entrada "Saldo anterior" de ₡300.000 en colones y En Cuenta ₡0.
2. **Given** Deuda a Pagar ₡500.000 y En Cuenta ₡700.000 (Pendiente de Recoger −₡200.000), **When** se cierra el mes, **Then** el mes nuevo no tiene entrada "Saldo anterior" y En Cuenta es ₡200.000.
3. **Given** Deuda a Pagar ₡500.000 y En Cuenta ₡500.000 (Pendiente de Recoger ₡0), **When** se cierra el mes, **Then** no hay "Saldo anterior" y En Cuenta del mes nuevo es ₡0.
4. **Given** Deuda a Pagar ₡0 (o negativa por un error de digitación, que se trata como 0) y En Cuenta ₡100.000, **When** se cierra el mes, **Then** no hay "Saldo anterior" y En Cuenta del mes nuevo es ₡100.000.
5. **Given** cualquier cierre, **When** se crea el mes nuevo, **Then** Pagos Realizados arranca en ₡0 y el mes que se cierra queda en solo lectura.

---

### User Story 2 - Revisar y ajustar en una vista previa antes de cerrar (Priority: P1)

Al presionar "Cerrar Mes" el usuario ve un diálogo con dos columnas: los valores finales del mes que cierra y cómo arrancará el mes nuevo. Puede ajustar el Tipo de Cambio y En Cuenta del mes nuevo, ver cómo cambian los indicadores del mes nuevo y luego confirmar o cancelar. Nada se guarda hasta confirmar.

**Why this priority**: El cierre es irreversible; el usuario necesita ver y corregir el resultado antes de aplicarlo.

**Independent Test**: Abrir el diálogo, cambiar En Cuenta del mes nuevo y verificar que los indicadores del mes nuevo se recalculan; cancelar y verificar que el mes sigue abierto y sin cambios; abrir de nuevo, confirmar y verificar que el mes nuevo quedó con los valores ajustados.

**Acceptance Scenarios**:

1. **Given** un mes abierto, **When** el usuario presiona "Cerrar Mes", **Then** se abre el diálogo y no se modifica ningún dato.
2. **Given** el diálogo abierto, **When** el usuario lo revisa, **Then** la columna "Mes que cierra ({mes/año})" muestra en solo lectura: Total por pagar colonizado, Por pagar en {Moneda}, Saldos por Cobrar a Clientes, Shipping CR Pendientes, Deuda a Pagar, En Cuenta, Pendiente de Recoger, Posición, Tipo de Cambio y Pagos Realizados.
3. **Given** el diálogo abierto, **When** el usuario lo revisa, **Then** la columna "Mes nuevo ({mes/año siguiente})" muestra: Tipo de Cambio propuesto (editable), Pagos Realizados ₡0 (solo lectura), En Cuenta propuesto con la regla de arrastre (editable), las entradas con que arranca en solo lectura ("Saldo anterior" con su monto, o "Sin saldo anterior") y sus indicadores iniciales.
4. **Given** el diálogo abierto, **When** el usuario cambia el Tipo de Cambio o En Cuenta del mes nuevo, **Then** los indicadores del mes nuevo se actualizan.
5. **Given** el diálogo abierto, **When** el usuario presiona "Cancelar", **Then** el diálogo se cierra y el mes sigue abierto sin cambios.
6. **Given** el diálogo con valores válidos, **When** el usuario confirma, **Then** el mes se cierra y el mes nuevo se crea con el Tipo de Cambio y En Cuenta del diálogo y con el Saldo anterior calculado por el sistema al momento de confirmar.
7. **Given** que entre abrir el diálogo y confirmar cambiaron los datos del mes (por ejemplo, se agregó una entrada), **When** el usuario confirma, **Then** el cierre usa los valores actuales del mes, no los mostrados.

---

### User Story 3 - Evitar tipos de cambio en cero y montos negativos (Priority: P2)

El sistema no permite guardar un Tipo de Cambio igual a cero o negativo, ni montos negativos en Pagos Realizados o En Cuenta, en ningún formulario del módulo (campos del mes, inicialización del primer mes y diálogo de cierre).

**Why this priority**: Un tipo de cambio en cero hace desaparecer la deuda en otras monedas al cerrar; los negativos no tienen sentido de negocio.

**Independent Test**: Intentar guardar Tipo de Cambio 0, −1, Pagos −100 o En Cuenta −100 en los campos del mes y en el diálogo de cierre; verificar que se rechaza con un mensaje y nada se guarda.

**Acceptance Scenarios**:

1. **Given** los campos del mes, **When** el usuario guarda Tipo de Cambio 0 o negativo, **Then** se rechaza con "El tipo de cambio debe ser mayor a cero." y no se guarda nada.
2. **Given** los campos del mes, **When** el usuario guarda Pagos Realizados o En Cuenta negativos, **Then** se rechaza con "El valor no puede ser negativo." y no se guarda nada.
3. **Given** el diálogo de cierre, **When** el Tipo de Cambio del mes nuevo es 0 o negativo, o En Cuenta es negativo, **Then** no se puede confirmar y se muestra el mensaje correspondiente.
4. **Given** la inicialización del primer mes, **When** el Tipo de Cambio es 0 o negativo, **Then** se rechaza (comportamiento actual, se mantiene).
5. **Given** cualquiera de estos formularios, **When** alguien envía valores inválidos sin pasar por el formulario, **Then** el sistema también los rechaza.

---

### User Story 4 - Cerrar el mes una sola vez aunque se confirme dos veces (Priority: P2)

Si el usuario confirma el cierre dos veces rápido, desde dos pestañas o si la petición se reintenta, solo se cierra el mes mostrado y solo existe un mes abierto.

**Why this priority**: Un doble cierre cerraría también el mes recién creado y dejaría la contabilidad desfasada un mes, sin forma de revertirlo.

**Independent Test**: Abrir el diálogo en dos pestañas y confirmar en ambas; verificar que el mes se cerró una vez, que existe exactamente un mes abierto (el siguiente) y que la segunda pestaña muestra "Este mes ya fue cerrado.".

**Acceptance Scenarios**:

1. **Given** el diálogo de cierre, **When** el usuario presiona "Confirmar cierre", **Then** el botón se deshabilita hasta que termina la operación.
2. **Given** que el mes mostrado en el diálogo ya fue cerrado (por otra pestaña o petición), **When** se confirma, **Then** se rechaza con "Este mes ya fue cerrado." y la pantalla se recarga mostrando el mes abierto actual.
3. **Given** dos confirmaciones simultáneas, **When** ambas se procesan, **Then** solo una tiene efecto y nunca existe más de un mes abierto por organización.

---

### User Story 5 - Panel más claro: En Cuenta visible y colores suaves (Priority: P3)

En la primera columna del panel el usuario ve, en este orden, Deuda a Pagar (rojo pastel), En Cuenta (₡) (verde pastel, nuevo) y Pendiente de Recoger (amarillo pastel), de modo que la relación "Deuda − En Cuenta = Pendiente" se lee de arriba hacia abajo.

**Why this priority**: Mejora la lectura del panel; no cambia datos.

**Independent Test**: Abrir CxP y verificar la columna 1 con los tres recuadros en ese orden y colores; guardar un nuevo valor de En Cuenta y verificar que el recuadro se actualiza.

**Acceptance Scenarios**:

1. **Given** el panel CxP, **When** se muestra, **Then** la columna 1 contiene en orden: Deuda a Pagar en rojo pastel, En Cuenta (₡) en verde pastel y Pendiente de Recoger en amarillo pastel, con texto legible (oscuro sobre fondo claro), ícono y número en negrita.
2. **Given** el recuadro En Cuenta (₡), **When** se muestra, **Then** tiene el mismo valor que el campo editable En Cuenta del mes.
3. **Given** que el usuario guarda un nuevo En Cuenta, **When** se recargan los indicadores, **Then** el recuadro muestra el nuevo valor.

---

### Edge Cases

- **Mes sin entradas**: Deuda a Pagar = −Pagos Realizados (≤ 0) → sin Saldo anterior; En Cuenta pasa igual.
- **Tipo de cambio de la Configuración en 0 o vacío**: el diálogo propone el Tipo de Cambio del mes que cierra y exige un valor mayor a cero para confirmar.
- **Mes abierto existente con Tipo de Cambio 0** (datos anteriores a este cambio): se sigue mostrando el aviso actual; al abrir el diálogo no se puede confirmar con Tipo de Cambio 0 para el mes nuevo, y el mes que cierra usa sus valores tal como están (sin conversión, como hoy).
- **Saldo anterior borrado** en el mes nuevo: se permite como cualquier entrada (sin cambio).
- **Otro usuario/pestaña cambia datos** entre abrir el diálogo y confirmar: se cierra con los valores actuales (Story 2, escenario 7).
- **Cierre de diciembre**: el mes nuevo es enero del año siguiente (sin cambio).
- **Más de un mes abierto existente** en los datos antes de aplicar la protección: debe corregirse manualmente antes de desplegar (ver Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

**Reglas del cierre**

- **FR-001**: Las fórmulas de los indicadores MUST NOT cambiar (Deuda a Pagar = Total por pagar colonizado − Pagos Realizados; Pendiente de Recoger = Deuda a Pagar − En Cuenta).
- **FR-002**: Al cerrar, el sistema MUST crear en el mes nuevo una entrada "Saldo anterior" en colones con el valor de Pendiente de Recoger del mes cerrado únicamente si ese valor es mayor que 0; si es 0 o negativo MUST NOT crearla.
- **FR-003**: El En Cuenta propuesto para el mes nuevo MUST ser máx(0, En Cuenta − máx(0, Deuda a Pagar)) del mes cerrado.
- **FR-004**: Pagos Realizados del mes nuevo MUST ser 0.
- **FR-005**: El Tipo de Cambio propuesto para el mes nuevo MUST ser el de la Configuración; si no es mayor que 0, MUST proponerse el del mes que se cierra.
- **FR-006**: El cierre MUST ser atómico: marcar el mes como cerrado, crear el mes nuevo con sus campos y crear el Saldo anterior (si aplica) ocurren juntos o no ocurre nada.

**Vista previa**

- **FR-007**: Presionar "Cerrar Mes" MUST abrir un diálogo de vista previa sin modificar ningún dato.
- **FR-008**: El diálogo MUST mostrar la columna "Mes que cierra" con los valores listados en Story 2 (escenario 2), en solo lectura.
- **FR-009**: El diálogo MUST mostrar la columna "Mes nuevo" con Tipo de Cambio (editable), Pagos Realizados 0 (solo lectura), En Cuenta (editable), las entradas iniciales (Saldo anterior en solo lectura; no se pueden agregar ni editar entradas desde el diálogo) y los indicadores iniciales del mes nuevo.
- **FR-010**: Los indicadores del mes nuevo en el diálogo MUST recalcularse cuando cambian el Tipo de Cambio o En Cuenta del mes nuevo, usando las mismas fórmulas que el panel.
- **FR-011**: "Cancelar" MUST cerrar el diálogo sin cambios; "Confirmar cierre" MUST ejecutar el cierre con el Tipo de Cambio y En Cuenta editados.
- **FR-012**: Al confirmar, el sistema MUST recalcular el Saldo anterior con los datos actuales del mes, sin usar valores calculados en el navegador.

**Validaciones**

- **FR-013**: El Tipo de Cambio MUST ser mayor que 0 al guardar los campos del mes, al inicializar el primer mes y al confirmar el cierre, tanto en el formulario como en el servidor.
- **FR-014**: Tipo de Cambio, Pagos Realizados y En Cuenta MUST NOT aceptar valores negativos en ningún formulario del módulo, tanto en el formulario como en el servidor.
- **FR-015**: Los rechazos MUST mostrar mensajes claros: "El tipo de cambio debe ser mayor a cero." y "El valor no puede ser negativo.".

**Protección contra doble cierre**

- **FR-016**: El botón "Confirmar cierre" MUST deshabilitarse mientras la operación está en curso.
- **FR-017**: La confirmación MUST identificar el mes mostrado en el diálogo; si ese mes ya no está abierto, el sistema MUST rechazar con "Este mes ya fue cerrado." sin cerrar ningún otro mes, y la pantalla MUST recargarse.
- **FR-018**: El sistema MUST garantizar que nunca exista más de un mes abierto por organización, incluso ante confirmaciones simultáneas.

**Panel**

- **FR-019**: La columna 1 del panel MUST mostrar en orden: Deuda a Pagar (fondo rojo pastel), En Cuenta (₡) (fondo verde pastel, nuevo) y Pendiente de Recoger (fondo amarillo pastel), con texto oscuro legible, ícono y número en negrita.
- **FR-020**: El recuadro En Cuenta (₡) MUST mostrar el mismo valor que el campo editable En Cuenta y actualizarse junto con los demás indicadores.

### Key Entities

- **Mes de control (PeriodControl)**: mes/año, Tipo de Cambio, Pagos Realizados, En Cuenta, indicador de cerrado. Regla nueva: a lo sumo uno abierto por organización. Sin campos nuevos.
- **Entrada CxP "Saldo anterior"**: ahora con valor = Pendiente de Recoger (solo si es positivo) en lugar de Deuda a Pagar.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En el 100 % de los cierres, el mes nuevo arranca con Saldo anterior = máx(0, Pendiente de Recoger) y En Cuenta = máx(0, En Cuenta − máx(0, Deuda a Pagar)) del mes cerrado (o el En Cuenta ajustado en el diálogo).
- **SC-002**: 0 cierres modifican datos sin que el usuario haya visto la vista previa y confirmado.
- **SC-003**: 0 meses se guardan con Tipo de Cambio menor o igual a 0 a partir de este cambio, y 0 con Pagos Realizados o En Cuenta negativos.
- **SC-004**: Ante confirmaciones repetidas o simultáneas, el número de meses abiertos por organización es siempre exactamente 1.
- **SC-005**: El usuario lee Deuda a Pagar, En Cuenta y Pendiente de Recoger juntos en la primera columna del panel, sin desplazarse.

## Assumptions

- Las reglas de este requerimiento reemplazan las reglas de arrastre del cierre del requerimiento 009 (Saldo anterior = Deuda a Pagar; En Cuenta = 0).
- Los meses ya cerrados no se recalculan.
- Antes de aplicar la protección de "un solo mes abierto por organización" se verifica que los datos existentes cumplan la regla; si no, se corrigen manualmente. La actualización de base de datos se aplica manualmente antes de desplegar.
- Los tonos pastel usan el estilo visual existente del sitio; los tonos exactos quedan a criterio de la implementación mientras cumplan legibilidad.
- No hay cambios de permisos ni de roles (la autenticación es un pendiente aparte).

## Out of Scope

- Cambiar las fórmulas de los indicadores.
- Reabrir meses cerrados o consultar meses anteriores.
- Validar la fecha en que se cierra el mes.
- Cambios en paquetes de envío (requerimiento 013).
- Autenticación.
