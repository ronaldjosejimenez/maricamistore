# Feature Specification: Ajustes en Órdenes, Metas y Clientes

**Feature Branch**: `017-ajustes-ordenes-metas-clientes`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Brainstorm `brainstorm/17-ajustes-ordenes-metas-clientes.md`.

---

## Clarifications

### Session 2026-10-19

- Q: ¿Con qué precisión se compara el porcentaje base contra los rangos de comisión? → A: Con el porcentaje tal como se muestra en pantalla, redondeado a 1 decimal (85.0 → 2.5%, 110.0 → 5%, 110.1 → 6%).
- Q: ¿La migración de órdenes Entregada → Completada deja rastro en el historial? → A: Sí. Se agrega un registro de historial por cada orden migrada (Entregada → Completada) con una nota que identifica la migración; los registros existentes no se modifican.
- Q: ¿Qué "Real" se usa para el monto de comisión? → A: El Real acumulado del mes de la meta, el mismo valor que muestra el indicador "Real".
- Q: ¿Cuándo se aplica la búsqueda de clientes? → A: Mientras el usuario escribe, sin botón de buscar.
- Q: ¿Cómo se presenta la comisión en Histórico? → A: Como una columna nueva "% Comisión" después de "% Cumplimiento", con el porcentaje coloreado y el monto entre paréntesis en letra más pequeña; con más de 110% la celda va resaltada con marco.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Flujo de entrega de órdenes simplificado y ítems consultables (Priority: P1)

El estado "Entregando" representa la entrega del negocio a sus clientes: la orden entra en él cuando llega el primer paquete y se empieza a entregar, y termina cuando todos los clientes recibieron lo suyo. El paso intermedio "Entregada" no aporta nada para el negocio, así que se elimina: al terminar de entregar, la orden pasa directo a "Completada". El botón que inicia la entrega dice "Iniciar entrega" en lugar de "Enviar", que daba a entender lo contrario. Además, fuera de Pending el usuario puede abrir cualquier ítem para ver su detalle en solo lectura.

**Why this priority**: corrige un flujo de estados confuso que el usuario recorre en cada orden y elimina un clic innecesario por orden. Es el único cambio que afecta datos existentes.

**Independent Test**: tomar una orden Activa, iniciar la entrega, completarla y verificar que nunca aparece "Entregada". Abrir un ítem de una orden no-Pending con "Ver" y comprobar que no se puede modificar.

**Acceptance Scenarios**:

1. **Given** una orden en estado Activa, **When** el usuario ve sus acciones en el listado de órdenes, **Then** aparece el botón "Iniciar entrega" (no "Enviar") junto a "Anular".
2. **Given** una orden en estado Entregando, **When** el usuario ve sus acciones, **Then** aparecen "Completar" y "Anular", y no existe ninguna opción "Entregada".
3. **Given** una orden en estado Entregando, **When** el usuario la completa, **Then** la orden queda en Completada y no admite más transiciones.
4. **Given** órdenes que antes de este cambio estaban en estado Entregada, **When** se aplica el cambio, **Then** esas órdenes quedan en Completada, su historial conserva la entrada anterior con el texto "Entregada" y se agrega un registro Entregada → Completada que identifica la migración.
5. **Given** el filtro de estados del listado de órdenes, **When** el usuario lo abre, **Then** "Entregada" ya no aparece como opción.
6. **Given** una orden en Pending, **When** el usuario ve sus ítems, **Then** cada ítem conserva el botón "Editar" y no muestra "Ver".
7. **Given** una orden en cualquier estado distinto de Pending (incluida Anulada), **When** el usuario pulsa "Ver" en un ítem, **Then** se abre el detalle del ítem con todos sus datos visibles, ningún campo editable y sin opción de guardar.
8. **Given** una orden no-Pending, **When** el usuario ve sus ítems, **Then** "Ver Img" y "Reasignar" siguen disponibles bajo las mismas condiciones de antes.
9. **Given** una orden en Entregando, **When** el usuario marca un ítem como recibido, **Then** la marcación se guarda. **Given** una orden en Completada, **Then** la marcación de recepción no está disponible.

---

### User Story 2 - % de Comisión en Metas (Priority: P2)

El vendedor y el administrador quieren ver, junto al avance de la meta, qué porcentaje de comisión corresponde a ese desempeño y cuánto dinero representa.

**Why this priority**: da visibilidad inmediata del incentivo sobre información que ya existe; no cambia datos.

**Independent Test**: con una meta del mes en curso cuyo "% al día de hoy" esté en cada rango, verificar el porcentaje, el color y el monto mostrado. En Histórico, verificar la columna de comisión para un mes cerrado.

**Acceptance Scenarios**:

1. **Given** el dashboard de Mes Actual del mes en curso con "% al día de hoy" = 84.9%, **When** se carga, **Then** "% de Comisión" muestra 0% en rojo, con el monto (0) entre paréntesis.
2. **Given** "% al día de hoy" = 85.0% o 85.9%, **Then** muestra 2.5% en amarillo.
3. **Given** "% al día de hoy" = 86.0%, 100.0% o 110.0%, **Then** muestra 5% en verde.
4. **Given** "% al día de hoy" = 110.1%, **Then** muestra 6% en verde y la caja tiene un marco resaltado.
5. **Given** Real = 1,000,000 y comisión 5%, **Then** al lado del porcentaje se muestra, entre paréntesis y en letra más pequeña, el monto 50,000 con el signo de moneda de la meta.
6. **Given** el usuario cambia la meta o los montos reales del mes, **When** el dashboard se recalcula, **Then** "% de Comisión" y su monto se actualizan junto con los demás indicadores.
7. **Given** el listado de Histórico de un vendedor, **When** se carga, **Then** cada mes muestra, en una columna "% Comisión" después de "% Cumplimiento", su % de comisión y el monto equivalente con los mismos rangos y colores, calculados con el % de cumplimiento final del mes; para el mes en curso se usa el "% al día de hoy".

---

### User Story 3 - Buscador de Clientes (Priority: P3)

El usuario necesita encontrar rápido un cliente en el catálogo: por apodo, nombre, teléfono, email o dirección.

**Why this priority**: mejora la usabilidad del catálogo; la fila de filtros que se muestra hoy no funciona.

**Independent Test**: escribir parte de un teléfono en la búsqueda global y verificar que solo quedan los clientes que coinciden; combinar con un filtro de columna.

**Acceptance Scenarios**:

1. **Given** el catálogo de clientes, **When** el usuario escribe un texto en la búsqueda global, **Then** el grid filtra mientras escribe (sin botón) y muestra solo los clientes cuyo Apodo, Nombre Completo, Teléfono, Email o Dirección contiene ese texto.
2. **Given** la búsqueda global "MARIA", **Then** se encuentran clientes con "María" o "maria" (sin distinguir mayúsculas ni tildes).
3. **Given** un texto en el filtro de la columna Teléfono, **Then** el grid muestra solo los clientes cuyo teléfono contiene ese texto.
4. **Given** una búsqueda global y uno o más filtros de columna, **Then** el grid muestra solo los clientes que cumplen todas las condiciones a la vez.
5. **Given** la búsqueda o los filtros se vacían, **Then** el grid vuelve a mostrar todos los clientes.
6. **Given** filtros activos, **When** el usuario inserta, edita o elimina un cliente, **Then** la operación funciona y el grid sigue respetando los filtros activos.
7. **Given** ningún cliente coincide, **Then** el grid indica que no hay resultados.

---

### Edge Cases

- Orden en Entregada con ítems sin marcar como recibidos al momento de la migración: pasa a Completada igual. La marcación de recepción ya no estará disponible, lo cual se acepta.
- Orden en Entregada que hubiera sido necesario anular: tras la migración ya no se puede anular, porque Completada es final. Se acepta (ver Assumptions).
- "% al día de hoy" sin meta acumulada (meta hasta hoy = 0) o meta del mes no definida: el % es 0, así que la comisión es 0% en rojo y el monto es 0.
- Real negativo o igual a 0: el monto de comisión se muestra como 0, nunca negativo.
- Valor exacto en un límite (85.0, 86.0, 110.0): se aplican los rangos inclusivos de FR-014 sobre el porcentaje redondeado a 1 decimal, tal como se muestra.
- Búsqueda con espacios al inicio o al final: se ignoran.
- Clientes con campos vacíos (sin email o dirección): no rompen la búsqueda; simplemente no coinciden por ese campo.
- Ítem con imagen visto en modo "Ver": la imagen se puede consultar como hoy (vista previa en el detalle o "Ver Img").

## Requirements *(mandatory)*

### Functional Requirements

**Órdenes**

- **FR-001**: El sistema MUST dejar de ofrecer el estado "Entregada" (Delivered) para órdenes. Los estados de orden disponibles son: Pending, Activa, Entregando, Completada y Anulada.
- **FR-002**: Las transiciones permitidas MUST ser: Pending → Activa; Activa → Entregando o Anulada; Entregando → Completada o Anulada; Completada sin transiciones (final).
- **FR-003**: El sistema MUST rechazar cualquier intento de transicionar una orden hacia o desde "Entregada".
- **FR-004**: Todas las órdenes existentes en estado "Entregada" MUST pasar a "Completada" como parte de este cambio, sin crear transacciones ni entradas de cuentas por pagar.
- **FR-005**: El historial de estados existente MUST conservarse sin modificaciones, y los registros con estado "Entregada" MUST seguir mostrándose de forma legible. Por cada orden migrada MUST agregarse un registro de historial Entregada → Completada con una nota que identifique la migración.
- **FR-006**: La marcación de recepción de ítems MUST permitirse solo en órdenes en estado Entregando.
- **FR-007**: La reasignación de ítems MUST seguir permitida en órdenes Activas, Entregando y Completadas.
- **FR-008**: El filtro de estados del listado de órdenes MUST dejar de mostrar "Entregada".
- **FR-009**: El botón de transición hacia Entregando MUST decir "Iniciar entrega". El nombre del estado se sigue mostrando como "Entregando".
- **FR-010**: En órdenes Pending, cada ítem MUST conservar la acción "Editar" (y "Eliminar") como hoy.
- **FR-011**: En órdenes en cualquier otro estado, cada ítem MUST ofrecer la acción "Ver", que muestra el mismo detalle del ítem con todos los campos en solo lectura y sin opción de guardar.
- **FR-012**: Las acciones "Ver Img" y "Reasignar" MUST mantener su comportamiento y condiciones actuales.

**Metas**

- **FR-013**: El dashboard de Mes Actual MUST mostrar un indicador "% de Comisión" junto a Real, % de cumplimiento y % al día de hoy.
- **FR-014**: El % de comisión MUST calcularse a partir del porcentaje base P, redondeado a 1 decimal tal como se muestra en pantalla, con estos rangos:
  - P < 85 → 0%, color rojo
  - 85 ≤ P < 86 → 2.5%, color amarillo
  - 86 ≤ P ≤ 110 → 5%, color verde
  - P > 110 → 6%, color verde y marco resaltado del indicador
- **FR-015**: El porcentaje base P MUST ser el "% al día de hoy" cuando la meta corresponde al mes en curso, y el "% de cumplimiento" del mes en cualquier otro caso.
- **FR-016**: Junto al % de comisión, entre paréntesis y en letra más pequeña, MUST mostrarse el monto de comisión = Real acumulado del mes (el valor del indicador "Real") × % de comisión, con el signo de moneda de la meta y el mismo formato numérico que los otros montos. Si el resultado es negativo, se muestra 0.
- **FR-017**: El indicador MUST recalcularse cada vez que se recalculan los demás indicadores del dashboard (al cambiar la meta o los montos reales).
- **FR-018**: El listado de Histórico MUST mostrar, en una columna "% Comisión" ubicada después de "% Cumplimiento", para cada mes el % de comisión con su color (y el resaltado cuando corresponda) y el monto equivalente, aplicando FR-014 a FR-016.

**Clientes**

- **FR-019**: El catálogo de clientes MUST ofrecer una búsqueda global que filtre mientras el usuario escribe (sin botón de buscar), por coincidencia parcial en Apodo, Nombre Completo, Teléfono, Email y Dirección.
- **FR-020**: Los filtros por columna del grid de clientes MUST filtrar por coincidencia parcial en su columna.
- **FR-021**: La búsqueda global y los filtros de columna MUST combinarse (deben cumplirse todos) y no distinguir mayúsculas ni tildes; los espacios al inicio y al final se ignoran.
- **FR-022**: Con búsqueda o filtros activos, insertar, editar y eliminar clientes MUST seguir funcionando, y el resultado MUST seguir respetando los criterios activos.

### Key Entities

- **Orden**: su estado deja de admitir "Entregada". Las órdenes en ese estado pasan a "Completada".
- **Historial de estado de orden**: se conservan intactos los registros existentes, incluidos los "Entregada". Se agrega un registro de migración por cada orden migrada.
- **Meta mensual**: aporta Real, % de cumplimiento y % al día de hoy. La comisión es un valor derivado y no se persiste.
- **Cliente**: sin cambios de datos; solo cambia cómo se busca.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Después del cambio, 0 órdenes quedan en estado "Entregada", y completar una entrega requiere una acción menos que antes.
- **SC-002**: El 100% de los ítems de órdenes no-Pending se pueden consultar en detalle sin posibilidad de modificarlos.
- **SC-003**: Para cualquier valor del porcentaje base, el % de comisión, su color y su monto coinciden con la tabla de FR-014 en el 100% de los casos de prueba, incluidos los límites 84.9, 85.0, 85.9, 86.0, 110.0 y 110.1.
- **SC-004**: Un usuario encuentra un cliente escribiendo parte de cualquiera de sus cinco datos visibles y ve el resultado al instante (menos de 1 segundo con el volumen actual de clientes).
- **SC-005**: Las funciones existentes de órdenes (activar, anular, reasignar, marcar recepción, paquetes), metas y CRUD de clientes siguen funcionando sin regresiones.

## Assumptions

- El negocio acepta que las órdenes hoy en "Entregada" pasen a "Completada" y, por lo tanto, ya no puedan anularse ni marcarse como recibidas. Antes de aplicar el cambio en producción se revisa cuántas órdenes están en ese estado.
- "Completada" no tiene efectos secundarios (transacciones o cuentas por pagar), así que migrar a ese estado no genera movimientos contables.
- El estado de los ítems (`OrderItemStatus`, que tiene su propio "Entregada") queda fuera de alcance.
- El tipo de entrada de cuentas por pagar "Auto-Entregada", que hoy no se genera, queda fuera de alcance.
- La comisión es solo informativa: no se guarda ni genera pagos.
- El volumen de clientes es pequeño (cientos), así que la búsqueda puede operar sobre el catálogo completo cargado en pantalla.
