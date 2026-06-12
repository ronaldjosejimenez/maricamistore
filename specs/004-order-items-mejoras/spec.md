# Feature Specification: Order Items — Mejoras de UI y Modelo

**Feature Branch**: `004-order-items-mejoras`

**Created**: 2026-06-06

**Status**: Implemented ✓

**Input**: Brainstorm `brainstorm/04-mejoras-fase2.md` — Spec 004: mejoras sobre la pantalla `Orders/Items`: agrupación por cliente con subtotales, ordenamiento, contador, reactive pricing, checklist de recepción (nuevo campo `IsReceived`), y URL más ancho.

**Origen**: `requerimientos/Mejoras varias fase 2.txt` — ítems cubiertos por este spec: **1.1, 1.2, 1.3, 1.4, 1.5, 6**.

**Ítems del mismo archivo NO cubiertos aquí** (tienen sus propios specs):
- Ítem 2 → spec 005 (decimales en Envío Estimado)
- Ítem 3 → spec 005 (unificación Saldos + Pagos)
- Ítem 4, 4.1, 4.2 → spec 006 (Pantalla de Transacciones)
- Ítem 5 → spec 005 (fix Saldos)
- Ítem 7, 7.1 → spec 007 (Signo de Moneda)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Migración: Nuevo Campo `IsReceived` en OrderItem (Priority: P0)

El sistema agrega el campo `IsReceived` (booleano, falso por defecto) a la tabla de `OrderItems` sin pérdida de datos existentes.

**Why this priority**: Es el prerequisito bloqueante para el checklist de recepción (Historia 5). Sin la migración, la historia 5 no puede implementarse.

**Independent Test**: Ejecutar la migración en un entorno de desarrollo — todos los registros existentes de `OrderItem` deben tener `IsReceived = false` y la estructura de la tabla debe ser correcta.

**Acceptance Scenarios**:

1. **Given** una base de datos con ítems de orden existentes, **When** se aplica la migración, **Then** todos los registros existentes tienen `IsReceived = false` y los datos previos se preservan íntegramente.
2. **Given** la migración aplicada, **When** se crea un nuevo `OrderItem`, **Then** el campo `IsReceived` es `false` por defecto.

---

### User Story 2 — Grid Agrupado por Cliente con Subtotales (Priority: P1)

El operador puede visualizar los ítems de una orden agrupados por cliente, con una fila de subtotal de `AgreedPriceInLocal` al cierre de cada grupo, para entender de un vistazo cuánto debe cada cliente en la orden.

**Why this priority**: Es el cambio visual más impactante. Sin agrupación, el operador debe sumar manualmente los ítems por cliente para saber el total acordado.

**Independent Test**: Abrir una orden con ítems de al menos 2 clientes distintos — el grid debe mostrar los ítems agrupados por cliente, con una fila de subtotal al final de cada grupo y un total general al final.

**Acceptance Scenarios**:

1. **Given** una orden con ítems de múltiples clientes, **When** el operador abre la pantalla de ítems, **Then** los ítems se muestran agrupados por cliente (NickName o Nombre si no tiene apodo), con una fila de subtotal de `AgreedPriceInLocal` al final de cada grupo.
2. **Given** el grid con grupos de clientes, **When** el operador visualiza la pantalla, **Then** aparece un total general de `AgreedPriceInLocal` al final del grid (suma de todos los subtotales).
3. **Given** una orden con un solo cliente, **When** el operador abre la pantalla, **Then** el grid muestra todos los ítems sin separación de grupo (o con un único grupo), y el total general coincide con el `TotalAgreedPriceInLocal` de la orden.
4. **Given** una orden sin ítems, **When** el operador abre la pantalla, **Then** el grid está vacío y el total general muestra cero.

---

### User Story 3 — Ordenamiento por Defecto: Cliente → Fecha (Priority: P1)

El grid de ítems se muestra ordenado por defecto primero por nombre/apodo del cliente (A→Z) y luego por fecha de creación del ítem (más reciente primero dentro de cada grupo).

**Why this priority**: Sin ordenamiento coherente, los ítems del mismo cliente aparecen mezclados y el agrupado visual no funciona correctamente. El ordenamiento es prerequisito funcional del agrupado.

**Independent Test**: Agregar ítems para al menos 2 clientes distintos en distinto orden temporal — el grid debe mostrarlos ordenados por cliente A→Z y dentro de cada cliente por fecha más reciente primero.

**Acceptance Scenarios**:

1. **Given** ítems de clientes "Marta" y "Ana" en la orden, **When** el operador carga el grid, **Then** los ítems de "Ana" aparecen antes que los de "Marta" (orden alfabético A→Z por NickName/Name).
2. **Given** dos ítems del mismo cliente creados en momentos distintos, **When** el operador visualiza el grupo de ese cliente, **Then** el ítem más reciente aparece primero dentro del grupo.
3. **Given** un cliente sin NickName, **When** el grid se ordena, **Then** se usa el campo `Name` como criterio de ordenamiento en su lugar.

---

### User Story 4 — Reactive Pricing: RealPrice sigue a ListPrice (Priority: P1)

En el modal de agregar/editar ítem, mientras el operador escribe en el campo `ListPrice`, el campo `RealPrice` se actualiza automáticamente con el mismo valor (comportamiento por defecto). Si el operador edita `RealPrice` manualmente, deja de seguir a `ListPrice`.

**Why this priority**: Evita que el operador tenga que ingresar el mismo valor dos veces en el caso más común (RealPrice = ListPrice). Reduce errores de digitación.

**Independent Test**: Abrir el modal de nuevo ítem, escribir "25.50" en `ListPrice` — `RealPrice` debe mostrar "25.50" automáticamente. Luego cambiar manualmente `RealPrice` a "20.00" — al modificar `ListPrice` nuevamente, `RealPrice` debe mantener "20.00".

**Acceptance Scenarios**:

1. **Given** el modal de ítem abierto (nuevo o edición), **When** el operador escribe un valor en `ListPrice`, **Then** `RealPrice` se actualiza automáticamente para igualar `ListPrice` en tiempo real.
2. **Given** el operador escribe en `ListPrice` y el campo `RealPrice` sigue automáticamente, **When** el operador edita manualmente `RealPrice`, **Then** el campo `RealPrice` deja de seguir a `ListPrice` (la sincronización automática se desactiva para ese ítem).
3. **Given** el modal de edición de un ítem existente donde `RealPrice == ListPrice`, **When** el operador escribe en `ListPrice`, **Then** `RealPrice` se actualiza automáticamente (el valor igual indica que nunca fue editado manualmente).
4. **Given** el modal de edición de un ítem existente donde `RealPrice ≠ ListPrice`, **When** se abre el modal, **Then** la sincronización NO está activa — `RealPrice` mantiene su valor guardado aunque el operador cambie `ListPrice`.
5. **Given** el operador edita `ListPrice` después de haber tocado manualmente `RealPrice`, **When** `ListPrice` cambia, **Then** `RealPrice` permanece con el valor que el operador asignó manualmente.

---

### User Story 5 — Checklist de Recepción por Ítem (Priority: P1)

Cuando el estado de la orden es `Delivering` o `Delivered`, el operador puede marcar cada ítem individualmente como recibido. Al marcarlo, la fila cambia de color (verde) para distinguir los ítems verificados de los pendientes.

**Why this priority**: Permite al operador confirmar la recepción ítem por ítem al momento de la entrega, con evidencia visual clara de qué ya llegó y qué falta.

**Independent Test**: Transicionar una orden a estado `Delivering`, abrir la pantalla de ítems — debe aparecer un checkbox en cada fila. Hacer clic en el checkbox de un ítem — la fila debe cambiar a color verde. Recargar la página — el ítem debe seguir marcado (persistido).

**Acceptance Scenarios**:

1. **Given** una orden en estado `Delivering` o `Delivered`, **When** el operador abre la pantalla de ítems, **Then** cada ítem muestra un checkbox de "Recibido" habilitado.
2. **Given** el checkbox visible de un ítem no marcado, **When** el operador lo marca, **Then** la fila del ítem cambia inmediatamente a color verde (`table-success`) y el cambio se persiste en la base de datos.
3. **Given** un ítem marcado como recibido (fila verde), **When** el operador desmarca el checkbox, **Then** la fila vuelve a su color original y el cambio se persiste.
4. **Given** una orden en estado distinto de `Delivering` y `Delivered` (ej. `Pending`, `Active`, `Completed`, `Voided`), **When** el operador abre la pantalla de ítems, **Then** el checkbox de "Recibido" no aparece (el campo es solo lectura o invisible).
5. **Given** una orden con ítems parcialmente marcados como recibidos, **When** el operador recarga la página, **Then** los ítems marcados siguen mostrando fila verde y los no marcados siguen sin color.

---

### User Story 6 — Contador de Ítems (Priority: P2)

El operador puede ver el número total de ítems en la orden directamente en la pantalla de ítems, sin necesidad de contarlos manualmente.

**Why this priority**: Información secundaria de utilidad rápida. No bloquea ninguna otra funcionalidad.

**Independent Test**: Abrir una orden con 5 ítems — la pantalla debe mostrar "5 ítems" (o similar). Agregar un ítem — el contador debe actualizarse a "6 ítems" sin recargar.

**Acceptance Scenarios**:

1. **Given** una orden con N ítems, **When** el operador carga la pantalla de ítems, **Then** se muestra el conteo total de ítems ("N ítem(s)" o similar) visible en la pantalla.
2. **Given** la pantalla de ítems con el contador visible, **When** el operador agrega un nuevo ítem, **Then** el contador se actualiza para reflejar el nuevo total.
3. **Given** la pantalla de ítems con el contador visible, **When** el operador elimina un ítem, **Then** el contador se actualiza para reflejar el nuevo total.
4. **Given** una orden sin ítems, **When** el operador carga la pantalla, **Then** el contador muestra "0 ítems".

---

### User Story 7 — Campo URL más Ancho (Priority: P2)

El campo de enlace de producto (`ProductLink`) en la grilla de ítems y en el modal tiene suficiente espacio visual para mostrar URLs largas sin truncamiento excesivo.

**Why this priority**: Mejora de usabilidad menor. Las URLs de productos en plataformas como Shein o Amazon son largas y actualmente se ven cortadas.

**Independent Test**: Ingresar una URL de 150 caracteres en `ProductLink` — el campo debe mostrar la URL de forma legible sin desbordarse del layout.

**Acceptance Scenarios**:

1. **Given** un ítem con una URL de producto larga (>100 caracteres), **When** el operador visualiza la grilla de ítems, **Then** el campo `ProductLink` tiene un ancho suficiente para mostrar el enlace de forma legible (o como enlace clickeable con texto truncado pero URL completa al hacer clic).
2. **Given** el modal de edición de ítem, **When** el operador visualiza el campo `ProductLink`, **Then** el campo de texto tiene un ancho razonable para ingresar y revisar URLs largas cómodamente.

---

### Edge Cases

- ¿Qué pasa si el campo `NickName` y `Name` de un cliente están vacíos? El sistema debe mostrar el ID del cliente como fallback (ya implementado en otras partes del sistema).
- ¿Qué pasa si la sincronización reactiva (RealPrice ← ListPrice) falla al cargar el modal de edición con `RealPrice` distinto de `ListPrice`? El valor guardado de `RealPrice` prevalece y la sincronización no se activa automáticamente al abrir.
- ¿Qué pasa si el usuario marca un ítem como recibido y la orden transiciona a `Completed`? El valor de `IsReceived` se preserva (solo lectura desde ese estado).
- ¿Qué pasa si hay un error al persistir `IsReceived`? El checkbox debe volver a su estado anterior y mostrar un mensaje de error.
- ¿Qué pasa si la orden tiene 0 ítems y se intenta calcular un subtotal? No debe mostrar filas de subtotal; el total general es cero.

---

## Requirements *(mandatory)*

### Functional Requirements

**Migración de Base de Datos**

- **FR-001**: El sistema DEBE agregar el campo `IsReceived` (booleano, default `false`, no nulo) a la tabla de `OrderItems` mediante una migración de base de datos.

**Grid: Agrupación y Ordenamiento**

- **FR-002**: El grid de ítems de la orden DEBE mostrar los ítems agrupados por cliente, con una fila de encabezado de grupo (nombre/apodo del cliente) y una fila de subtotal de `AgreedPriceInLocal` al final de cada grupo.
- **FR-003**: El grid DEBE incluir una fila de total general de `AgreedPriceInLocal` al final de todos los grupos.
- **FR-004**: El grid DEBE ordenarse por defecto: primero por NickName del cliente (A→Z, usando `Name` si `NickName` es nulo), luego por `CreatedAt` descendente dentro de cada grupo.

**Contador de Ítems**

- **FR-005**: La pantalla de ítems DEBE mostrar el conteo total de ítems de la orden de forma visible.
- **FR-006**: El contador DEBE actualizarse automáticamente al agregar o eliminar ítems sin recargar la página.

**Reactive Pricing**

- **FR-007**: En el modal de un **nuevo ítem**, mientras el operador escribe en `ListPrice`, el campo `RealPrice` DEBE actualizarse automáticamente con el mismo valor.
- **FR-007b**: En el modal de **edición de un ítem existente**, la sincronización automática aplica únicamente si `RealPrice` es igual a `ListPrice` al momento de abrir el modal (indicando que nunca fue editado manualmente). Si `RealPrice ≠ ListPrice` al abrir, el campo se trata como editado manualmente desde el inicio y NO se sincroniza.
- **FR-008**: Si el operador modifica `RealPrice` manualmente durante una sesión de edición, el campo DEBE dejar de seguir automáticamente a `ListPrice` por el resto de esa sesión.

**Checklist de Recepción**

- **FR-009**: Cuando el estado de la orden sea `Delivering` o `Delivered`, cada fila del grid de ítems DEBE mostrar un checkbox de "Recibido".
- **FR-010**: Al marcar el checkbox de un ítem, el sistema DEBE persistir `IsReceived = true` en la base de datos y cambiar el color de la fila a `table-success` (verde AdminLTE) de forma inmediata.
- **FR-011**: El checkbox de "Recibido" DEBE ser un toggle: marcar y desmarcar deben persistirse correctamente.
- **FR-012**: En estados de orden distintos de `Delivering` y `Delivered`, el checkbox de "Recibido" NO DEBE mostrarse ni ser editable.

**URL Más Ancho**

- **FR-013**: El campo `ProductLink` en la grilla de ítems DEBE tener un ancho visual mayor para acomodar URLs largas (mínimo 250px de ancho o equivalente proporcional).
- **FR-014**: El campo `ProductLink` en el modal de ítem DEBE ocupar el ancho completo de su contenedor.

### Key Entities

- **OrderItem**: Registro de un producto en una orden. Se agrega el campo `IsReceived` (boolean, default false). Campos de vista relevantes: `CustomerId`, `AgreedPriceInLocal`, `ListPrice`, `RealPrice`, `ProductLink`, `CreatedAt`.
- **Customer**: Referenciado por `OrderItem.CustomerId`. Campo `NickName` (nullable) o `Name` como etiqueta de grupo en el grid.
- **Order**: Contiene el estado (`Status`) que determina si el checklist de recepción está activo. Campos `Delivering` y `Delivered` activan los checkboxes.

---

## Error Handling

- Al fallar la persistencia de `IsReceived` (error de servidor), el checkbox debe revertirse a su estado anterior y mostrar un mensaje de error amigable sin exponer detalles técnicos.
- Si el backend retorna error al cargar los ítems, la pantalla debe mostrar un mensaje descriptivo en lugar de dejar el grid vacío sin contexto.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El operador puede identificar el total acordado por cliente sin realizar ningún cálculo manual (el subtotal por cliente es visible directamente en la pantalla).
- **SC-002**: Los campos calculados reactivos (`RealPrice` desde `ListPrice`) se actualizan en menos de 200 ms tras cada tecla.
- **SC-003**: El operador puede marcar un ítem como recibido en un solo clic, con confirmación visual inmediata (cambio de color de fila).
- **SC-004**: Al marcar o desmarcar un ítem como recibido, el cambio persiste correctamente al recargar la página (0% de pérdida de estado).
- **SC-005**: El grid carga, ordena y agrupa correctamente hasta 200 ítems en menos de 1 segundo desde la respuesta del servidor.

---

## Assumptions

- La pantalla de referencia es `Orders/Items.cshtml` — no se crea ninguna página nueva ni ruta nueva.
- Los clientes (`Customer`) ya existen en el sistema; no se agrega ni modifica el modelo `Customer` en este spec.
- El agrupado visual del grid se implementa en el frontend (JS) sobre los datos ya cargados; el backend retorna los ítems ordenados.
- El contador de ítems puede ser un simple texto o badge visible en el encabezado de la card o en la barra de herramientas de la pantalla.
- La fila de "Recibido" no requiere confirmación adicional (modal de confirmación) al hacer clic — el cambio es inmediato.
- No se requieren pruebas automatizadas (fuera de alcance según política actual del proyecto).
- La UI está en español; el código en inglés (consistente con el resto del proyecto).
- El campo `IsReceived` es editable toggle (puede marcarse y desmarcarse) mientras el estado de la orden sea `Delivering` o `Delivered`. En `Completed` o `Voided` es solo lectura (no visible).
