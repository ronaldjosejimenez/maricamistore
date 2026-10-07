# Feature Specification: Control de Metas de Ventas

**Feature Branch**: `016-control-metas-ventas`

**Created**: 2026-10-06

**Status**: Draft

**Input**: Brainstorm `brainstorm/16-control-metas-ventas.md` (requerimiento original en `requerimientos/control de metas.txt`).

---

## Clarifications

### Session 2026-10-06

- Q: ¿Se pueden digitar montos reales de cualquier día del mes actual, incluidos días futuros? → A: Sí, cualquier día del mes en curso es editable (el usuario puede registrar con atraso o adelantado); solo los meses anteriores son de solo lectura.
- Q: ¿Con qué precisión se muestran los porcentajes? → A: Un decimal (ej. 87.5%), tanto en pantalla como al guardarse.
- Q: ¿Cómo se marca que un día fue "ajustado manualmente"? → A: Un día se considera ajustado cuando su Monto Meta es distinto de su Monto Meta Propuesto; no se requiere un indicador separado visible. Si el usuario deja el Monto Meta igual al propuesto, el día vuelve a seguir los recálculos.
- Q: ¿Qué granularidad de redondeo usa el algoritmo de distribución? → A: Múltiplos de 1 000 unidades de moneda (para la meta de 4 000 000 reproduce el estilo del Excel); el residuo se asigna al día con mayor peso. El valor es una constante del componente del algoritmo.
- Q: ¿Qué moneda y formato tienen los montos del detalle? → A: La moneda de la meta (encabezado), con el mismo formato de moneda ya usado en el resto de la aplicación.
- Q: ¿El "Monto real" del encabezado puede editarse directamente? → A: No; siempre es la suma de los reales del detalle.
- Q: ¿Qué pasa si el usuario cambia de vendedor con cambios sin guardar? → A: Cada edición de monto se guarda de inmediato al salir del campo, por lo que no hay cambios pendientes al cambiar de vendedor.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dar seguimiento diario a la meta del mes de un vendedor (Priority: P1)

En el menú Ventas → Metas → "Mes Actual", el usuario escoge un vendedor y ve la meta del mes en curso: un encabezado con la meta, el monto real acumulado y los porcentajes de cumplimiento, y debajo una tabla con un renglón por cada día del mes (meta propuesta, meta ajustable, monto real). Conforme el usuario digita el monto real vendido cada día, el encabezado se actualiza solo y un indicador de semáforo le dice si el vendedor va bien, regular o mal respecto a lo que debería llevar a la fecha de hoy.

**Why this priority**: Es el corazón del requerimiento; reemplaza el Excel que hoy se usa para controlar la meta (meta mensual repartida por día, columna de real y de cumplimiento).

**Independent Test**: Con al menos un vendedor registrado, abrir "Mes Actual", digitar montos reales en algunos días y verificar que el monto real total, el % de cumplimiento y el % al día de hoy (con su color) se actualizan y que, al recargar la pantalla, los valores persisten.

**Acceptance Scenarios**:

1. **Given** que existe al menos un vendedor y la pantalla "Mes Actual" se abre, **When** carga, **Then** el combo de vendedores aparece con el primer vendedor preseleccionado y se muestran los datos de su meta del mes actual para la organización de la sesión.
2. **Given** un vendedor con meta del mes, **When** el usuario cambia el monto real de un día en el detalle, **Then** el "Monto real" del encabezado (suma de los reales del detalle), el "% de cumplimiento" (real ÷ meta) y el "% de cumplimiento al día de hoy" se recalculan de inmediato en pantalla y quedan guardados (excepto el último, que no se guarda).
3. **Given** un vendedor con meta del mes, **When** se muestra el encabezado, **Then** "Monto real" y "% de cumplimiento" aparecen resaltados, y "Mes" y "Año" aparecen no editables.
4. **Given** el "% de cumplimiento al día de hoy" (suma de reales del día 1 a hoy ÷ suma del Monto Meta diario del día 1 a hoy, con "hoy" según hora de Costa Rica), **When** el valor es menor a 70%, **Then** se muestra en rojo; **When** está entre 70% y 90% (inclusive), **Then** en amarillo; **When** es mayor a 90%, **Then** en verde.
5. **Given** el detalle del mes, **When** se muestra la tabla, **Then** cada renglón muestra: número de día, nombre del día de la semana, fecha dd/mm/yyyy, Monto Meta Control (no editable), Monto Meta (editable), Monto real (editable) y % de cumplimiento del día (real ÷ Monto Meta del día).
6. **Given** que el usuario selecciona otro vendedor en el combo, **When** cambia la selección, **Then** la pantalla muestra los datos del mes actual de ese otro vendedor sin recargar la página completa.

---

### User Story 2 - Creación automática de la meta del mes bajo demanda (Priority: P1)

Cuando el usuario consulta un vendedor que todavía no tiene registro de meta para el mes actual (por ejemplo, la primera vez que se entra en un mes nuevo), el sistema crea automáticamente la meta del mes con sus 28 a 31 renglones diarios. La meta se reparte entre los días del mes siguiendo un patrón típico de ventas (sábados más fuertes, lunes más débiles, mayor peso hacia el final del mes), de forma que la suma de los días sea exactamente la meta mensual.

**Why this priority**: Sin esto la pantalla no tiene datos con los que trabajar y no se puede depender de un proceso programado a medianoche (el servicio se duerme cuando no se usa).

**Independent Test**: Seleccionar un vendedor sin meta del mes actual y verificar que aparecen todos los días del mes con montos propuestos cuya suma es exactamente la meta por defecto de la configuración, y que consultar de nuevo no duplica registros.

**Acceptance Scenarios**:

1. **Given** un vendedor sin registro del mes actual para la organización de la sesión, **When** el usuario lo consulta, **Then** se crea un encabezado con: mes y año actuales según hora de Costa Rica, Monto de la meta = "Meta mensual por defecto" de la configuración, Monto real 0, % de cumplimiento 0, Moneda = moneda local por defecto de la configuración y la organización de la sesión.
2. **Given** la creación del encabezado, **When** se crea el detalle, **Then** se crea un renglón por cada día natural del mes (sin importar qué día del mes ocurra la creación), con Monto Meta Propuesto calculado por el algoritmo de distribución, Monto Meta igual al propuesto, Monto real 0 y % 0.
3. **Given** el detalle recién creado, **When** se suman los Montos Meta Propuestos de todos los días, **Then** el total es exactamente igual al Monto de la meta del encabezado.
4. **Given** un vendedor que ya tiene registro del mes actual, **When** el usuario lo consulta de nuevo (o dos usuarios lo consultan a la vez), **Then** no se crean registros duplicados.
5. **Given** que el cliente (navegador) tiene otra zona horaria o fecha distinta, **When** se determina el mes/año/día actual, **Then** siempre se usa la hora de Costa Rica, no la del cliente.
6. **Given** el algoritmo de distribución, **When** se generan montos para cualquier mes del año (28, 29, 30 o 31 días) y para cualquier meta mensual, **Then** la suma de los días es siempre exactamente la meta y ningún día queda con monto negativo.

---

### User Story 3 - Ajustar la meta del mes o de un día (Priority: P2)

El usuario puede cambiar el "Monto de la meta" del encabezado (por ejemplo, una meta distinta a la de por defecto para ese vendedor) y también el "Monto Meta" de días específicos (por ejemplo, un día con menos horario). Si cambia la meta del encabezado o la moneda, la meta diaria propuesta se recalcula respetando los ajustes manuales que el usuario ya hizo en días concretos y sin tocar los montos reales ya digitados.

**Why this priority**: Es necesario para que el control sea realista, pero la pantalla ya aporta valor con los valores por defecto.

**Independent Test**: Cambiar el Monto de la meta de un vendedor y verificar que el "Monto Meta Control" de todos los días se recalcula y suma la nueva meta, que los días con "Monto Meta" editado manualmente conservan su valor, y que los montos reales no cambian.

**Acceptance Scenarios**:

1. **Given** un vendedor con meta del mes, **When** el usuario modifica el "Monto de la meta" del encabezado, **Then** el "Monto Meta Control" (propuesto) de todos los días se recalcula con el algoritmo para sumar la nueva meta, y el "Monto Meta" de cada día se actualiza al nuevo propuesto únicamente en los días cuyo "Monto Meta" no haya sido modificado manualmente.
2. **Given** un día cuyo "Monto Meta" el usuario modificó manualmente, **When** se recalcula la meta del encabezado, **Then** ese día conserva su valor editado.
3. **Given** cualquier cambio de meta, **When** se recalcula, **Then** los montos reales digitados no se modifican, y los % de cumplimiento (del encabezado y de cada día) se recalculan con la nueva meta.
4. **Given** un vendedor con meta del mes, **When** el usuario cambia la moneda en el combo de monedas del encabezado, **Then** la nueva moneda queda guardada para esa meta (no se convierten montos automáticamente) y se aplica la misma regla de recálculo del escenario 1.
5. **Given** que el usuario edita el "Monto Meta" de un día, **When** guarda el valor, **Then** el "% de cumplimiento" de ese día y el "% de cumplimiento al día de hoy" del encabezado se actualizan.

---

### User Story 4 - Mantener el catálogo de vendedores (Priority: P1)

En el menú Ventas → Catálogos aparece una nueva opción "Vendedores" para dar de alta, modificar, desactivar y consultar vendedores. Un vendedor tiene nombre, apodo, teléfono y correo electrónico (los campos de cliente que aplican), más un indicador activo/inactivo.

**Why this priority**: Prerrequisito de la pantalla de metas: sin vendedores no hay a quién asignarle una meta.

**Independent Test**: Crear un vendedor desde el catálogo y verificar que aparece en el combo de "Mes Actual"; desactivarlo y verificar que ya no aparece en el combo pero sus metas históricas se conservan.

**Acceptance Scenarios**:

1. **Given** el menú Ventas → Catálogos, **When** el usuario abre "Vendedores", **Then** ve la lista de vendedores con opciones de crear, editar y activar/desactivar, siguiendo el mismo estilo de los demás catálogos.
2. **Given** el formulario de vendedor, **When** el usuario captura Nombre, Apodo, Teléfono y Correo, **Then** el vendedor se guarda; el Nombre es obligatorio.
3. **Given** un vendedor desactivado, **When** el usuario abre "Mes Actual", **Then** el vendedor no aparece en el combo, pero sus metas de meses anteriores siguen visibles en el Histórico.
4. **Given** un vendedor que ya tiene metas registradas, **When** se desactiva, **Then** no se eliminan sus metas ni su detalle.

---

### User Story 5 - Configurar la meta mensual por defecto (Priority: P2)

En la pantalla de Configuración aparece un nuevo campo "Meta mensual por defecto", con valor inicial 4 000 000, que se usa como Monto de la meta al crear automáticamente la meta de un mes.

**Why this priority**: Parametriza el valor con el que se crean las metas nuevas; sin él se usaría el valor por defecto 4 000 000 fijo.

**Independent Test**: Cambiar el valor en Configuración, consultar un vendedor sin meta del mes y verificar que la meta creada usa el nuevo valor.

**Acceptance Scenarios**:

1. **Given** la pantalla de Configuración, **When** el usuario la abre, **Then** ve el campo "Meta mensual por defecto" con el valor actual (4 000 000 si nunca se ha cambiado).
2. **Given** un valor modificado y guardado, **When** se crea la meta de un mes para un vendedor, **Then** el Monto de la meta es el valor configurado.
3. **Given** un valor negativo o vacío, **When** el usuario intenta guardar, **Then** se rechaza con un mensaje claro; el valor 0 se rechaza también (la meta debe ser mayor que cero).

---

### User Story 6 - Consultar el histórico de metas (Priority: P3)

En el menú Ventas → Metas hay una segunda opción, "Histórico de metas", de solo lectura: permite consultar, por vendedor, las metas de meses anteriores (mes, año, meta, monto real, % de cumplimiento, moneda) y abrir el detalle diario de cada una.

**Why this priority**: Complementa el seguimiento del mes en curso; el valor principal está en Mes Actual.

**Independent Test**: Con metas de meses anteriores registradas, abrir el Histórico, filtrar por vendedor y verificar que se listan correctamente y que el detalle diario se muestra sin posibilidad de edición.

**Acceptance Scenarios**:

1. **Given** el menú Ventas → Metas, **When** el usuario lo abre, **Then** ve dos opciones: "Mes Actual" e "Histórico de metas".
2. **Given** la pantalla de Histórico, **When** el usuario selecciona un vendedor, **Then** ve la lista de sus metas (de la organización de la sesión) ordenada de la más reciente a la más antigua, con meta, monto real y % de cumplimiento.
3. **Given** una meta del histórico, **When** el usuario la abre, **Then** ve el detalle diario en modo solo lectura.
4. **Given** un vendedor sin metas anteriores, **When** se consulta, **Then** se muestra un mensaje indicando que no hay registros.

---

### Edge Cases

- **No existe ningún vendedor**: la pantalla "Mes Actual" muestra un mensaje que invita a crear vendedores en el catálogo, sin errores.
- **Meta mensual = 0 o ausente**: no se permite; si falta el valor de configuración, FR-009 usa 4 000 000 como valor de respaldo.
- **% de cumplimiento con meta cero** (por ejemplo, meta diaria acumulada igual a 0 al inicio, o un día con Monto Meta 0): se muestra 0% en lugar de producir error o infinito.
- **Cambio de mes mientras la pantalla está abierta** (pasada la medianoche de Costa Rica del último día del mes): al recargar o cambiar de vendedor se muestra/crea el mes nuevo; la pantalla no debe seguir editando el mes anterior como si fuera el actual.
- **Meta de un vendedor en dos organizaciones el mismo mes**: son registros independientes (una por vendedor + mes + año + organización).
- **Monto real negativo**: no se permite; se rechaza con mensaje claro. Montos reales con decimales según la precisión de la moneda.
- **Montos de meta diarios editados que ya no suman la meta del encabezado**: se permite (son ajustes del usuario); el sistema no obliga a que sumen igual, pero la diferencia es visible al usuario [asunción documentada].
- **Consulta simultánea del mismo vendedor/mes por dos usuarios**: la creación bajo demanda debe ser idempotente (no duplicar).
- **Mes con 28/29/30/31 días y año bisiesto**: el detalle siempre tiene exactamente un renglón por día natural del mes.
- **El mismo mes se consulta con la moneda local cambiada en la configuración después de crear la meta**: la meta ya creada conserva su moneda; solo las nuevas usan la nueva moneda.
- **Vendedor desactivado con meta del mes actual en curso**: sigue existiendo y es consultable desde el Histórico; deja de ser seleccionable en "Mes Actual".

## Requirements *(mandatory)*

### Functional Requirements

**Catálogo de vendedores**

- **FR-001**: El sistema MUST permitir crear, editar y activar/desactivar vendedores con los campos Nombre (obligatorio), Apodo, Teléfono y Correo electrónico, más un indicador activo/inactivo.
- **FR-002**: El sistema MUST ofrecer el mantenimiento de vendedores como opción "Vendedores" dentro del menú Ventas → Catálogos.
- **FR-003**: El sistema MUST conservar las metas históricas de un vendedor al desactivarlo, y MUST NOT eliminar físicamente vendedores que ya tengan metas registradas.

**Configuración**

- **FR-004**: El sistema MUST incluir en la configuración el campo "Meta mensual por defecto" con valor inicial 4 000 000, editable en la pantalla de Configuración, validando que sea mayor que cero.

**Menú**

- **FR-005**: El sistema MUST agregar al menú Ventas un submenú "Metas" con las opciones "Mes Actual" e "Histórico de metas".

**Meta mensual y detalle diario (datos)**

- **FR-006**: El sistema MUST guardar una meta mensual por vendedor + mes + año + organización, con: vendedor, mes, año, monto de la meta, monto real, porcentaje de cumplimiento, moneda y organización.
- **FR-007**: El sistema MUST guardar para cada meta un detalle diario con un renglón por día natural del mes, con: día del mes, monto meta propuesto, monto meta, monto real y porcentaje de cumplimiento.
- **FR-008**: El sistema MUST garantizar que no existan dos metas para el mismo vendedor, mes, año y organización.

**Creación bajo demanda**

- **FR-009**: Al consultar un vendedor sin meta para el mes actual en la organización de la sesión, el sistema MUST crear automáticamente la meta (monto de meta = valor por defecto de configuración, monto real 0, % 0, moneda = moneda local por defecto de la configuración, organización de la sesión) y todos sus renglones diarios.
- **FR-010**: El sistema MUST determinar el mes, año y día actuales siempre con la zona horaria de Costa Rica, independientemente de la zona horaria o fecha del cliente.
- **FR-011**: El sistema MUST crear el detalle completo del mes (todos los días naturales) sin importar en qué día del mes ocurra la creación; el cálculo de monto meta propuesto cubre el mes completo.
- **FR-012**: El sistema MUST calcular el Monto Meta Propuesto de cada día mediante un algoritmo de distribución, aislado en un componente único y reemplazable, que: (a) pondera cada día según su día de la semana (sábado más alto, luego viernes/domingo/jueves; lunes el más bajo; martes y miércoles intermedios) y según su posición en el mes (peso creciente hacia el final), (b) redondea a múltiplos de 1 000 unidades de moneda y (c) ajusta el residuo de redondeo en un día para que la suma sea exactamente igual al Monto de la meta, sin montos negativos.
- **FR-013**: La creación bajo demanda MUST ser segura ante consultas simultáneas (no duplicar encabezado ni detalle).

**Pantalla Mes Actual**

- **FR-014**: La pantalla MUST mostrar un combo de vendedores activos con el primero preseleccionado; al seleccionar un vendedor MUST cargar (o crear, según FR-009) su meta del mes actual sin recargar toda la página.
- **FR-015**: El encabezado MUST mostrar: Moneda (editable, combo de monedas), Mes y Año (no editables), Monto de la meta (editable), Monto real (calculado, suma de reales del detalle, resaltado, guardado), % de cumplimiento (calculado = monto real ÷ monto de la meta, resaltado, guardado) y % de cumplimiento al día de hoy (calculado, no guardado, resaltado con semáforo).
- **FR-016**: El % de cumplimiento al día de hoy MUST calcularse como la suma de los montos reales de los días 1 al día de hoy (hora de Costa Rica, inclusive) dividida entre la suma de los Montos Meta diarios de esos mismos días, y MUST refrescarse cada vez que cambie cualquier monto real o monto meta del detalle.
- **FR-017**: El % de cumplimiento al día de hoy MUST mostrarse en rojo si es menor a 70%, amarillo si está entre 70% y 90% (ambos inclusive), y verde si es mayor a 90%.
- **FR-018**: El detalle MUST mostrarse como una tabla con: número de día, nombre del día de la semana, fecha (dd/mm/yyyy), Monto Meta Control (propuesto, no editable), Monto Meta (editable), Monto real (editable) y % de cumplimiento del día (monto real ÷ monto meta del día).
- **FR-019**: Al modificar un monto real, el sistema MUST recalcular y guardar el monto real y el % de cumplimiento del día y del encabezado, y actualizar la pantalla inmediatamente.
- **FR-020**: Al modificar el Monto Meta de un día, el sistema MUST guardar el cambio, recalcular el % de cumplimiento del día y del encabezado y el % al día de hoy, y considerar ese día como ajustado mientras su Monto Meta sea distinto de su Monto Meta Propuesto (si el usuario vuelve a dejarlo igual al propuesto, el día vuelve a seguir los recálculos).
- **FR-021**: Al modificar el Monto de la meta del encabezado o la moneda, el sistema MUST recalcular el Monto Meta Propuesto de todos los días con el algoritmo, actualizar el Monto Meta únicamente en los días no ajustados manualmente, y MUST NOT modificar los montos reales; los porcentajes se recalculan.
- **FR-026**: La pantalla "Mes Actual" MUST permitir editar únicamente el mes en curso (hora de Costa Rica); si el mes ya cambió, las ediciones sobre el mes anterior MUST rechazarse con un mensaje claro y la pantalla MUST mostrar el mes vigente.
- **FR-027**: Si el guardado de una edición falla o el valor es inválido (por ejemplo, negativo), la pantalla MUST mostrar un error junto al campo y restaurar el valor anterior.
- **FR-022**: Todos los cálculos de porcentaje MUST devolver 0 cuando el divisor sea 0.
- **FR-023**: El sistema MUST rechazar montos reales o montos meta negativos, así como un Monto de la meta menor o igual a cero.

**Histórico**

- **FR-024**: La pantalla "Histórico de metas" MUST permitir seleccionar un vendedor y listar sus metas de la organización de la sesión (más reciente primero) con mes, año, meta, monto real, % de cumplimiento y moneda, y abrir el detalle diario en modo solo lectura.

**Organización**

- **FR-025**: Todas las consultas de metas MUST limitarse a la organización de la sesión, y la creación MUST asociar la organización de la sesión.

### Key Entities *(include if feature involves data)*

- **Vendedor**: persona a la que se le asigna una meta de ventas. Atributos: nombre, apodo, teléfono, correo, activo/inactivo. Catálogo global (no pertenece a una organización).
- **Meta mensual (control de meta)**: meta de un vendedor para un mes/año en una organización. Atributos: vendedor, mes, año, monto de la meta, monto real, % de cumplimiento, moneda, organización. Una por vendedor + mes + año + organización.
- **Meta diaria (detalle)**: un renglón por día natural del mes de una meta mensual. Atributos: día del mes, monto meta propuesto, monto meta (ajustable), monto real, % de cumplimiento. Un día está "ajustado" cuando su monto meta difiere del propuesto (no hay indicador separado).
- **Configuración** (existente): se amplía con "Meta mensual por defecto".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un usuario puede abrir "Mes Actual", escoger un vendedor y ver su meta del mes con detalle diario completo en menos de 3 segundos, incluso cuando la meta del mes se crea en ese momento.
- **SC-002**: Para cualquier mes y cualquier meta mensual mayor a cero, la suma de los montos meta propuestos diarios es exactamente igual a la meta mensual en el 100% de los casos.
- **SC-003**: El usuario puede registrar el monto real de un día y ver reflejados el monto real total, el % de cumplimiento y el % al día de hoy en menos de 1 segundo, y los valores persisten tras recargar.
- **SC-004**: El color del semáforo del % al día de hoy coincide con los umbrales definidos (rojo < 70%, amarillo 70–90%, verde > 90%) en el 100% de los casos, incluyendo los valores límite 70% y 90%.
- **SC-005**: Para el mismo vendedor, mes, año y organización nunca existe más de una meta, incluso con consultas simultáneas.
- **SC-006**: El mes, año y día utilizados son siempre los de Costa Rica; consultar la pantalla desde un navegador con otra zona horaria produce exactamente los mismos resultados.
- **SC-007**: La distribución de octubre 2026 con meta 4 000 000 presenta el patrón del Excel de referencia: sábado como el día más alto de cada semana y lunes como el más bajo.
- **SC-008**: El usuario puede reemplazar el control mensual en Excel por esta pantalla sin perder información (meta diaria, real diario, cumplimiento diario y total del mes).

## Assumptions

- Los vendedores son un catálogo global (como los clientes), sin organización; las metas sí son por organización.
- Montos en la moneda de la meta, sin conversión automática al cambiar la moneda.
- El algoritmo de distribución es una primera aproximación del patrón observado en un único mes de referencia (octubre 2026); sus pesos exactos y su granularidad de redondeo se fijan en el plan y se ajustarán con datos de más meses. Debe estar aislado para poder reemplazarse sin tocar el resto del sistema.
- La autenticación sigue pendiente a nivel del sistema (ver notas del proyecto); esta funcionalidad no agrega control de acceso propio.
- Los montos meta diarios editados manualmente no obligan a que la suma coincida con la meta del encabezado.
- Las filas "Libres", "Vacaciones" y "Meta Final" del Excel de referencia están fuera de alcance.
- No se implementa un proceso programado que cree metas a medianoche; la creación es solo bajo demanda.
- La pantalla "Mes Actual" solo edita el mes en curso; los meses anteriores se consultan en modo solo lectura desde el Histórico.
- Usa la zona horaria de Costa Rica (UTC-6, sin horario de verano) fija en el código.
