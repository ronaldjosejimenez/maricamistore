# Feature Specification: Mejoras Fase 1

**Feature Branch**: `002-mejoras-fase1`

**Created**: 2026-06-03

**Status**: Draft

**Input**: Mejoras, correcciones y nuevas funcionalidades para el sistema MariCamiStore: configuración de organización por defecto, validación de organización en pantallas, corrección de visualización de moneda en catálogos, CRUD de organizaciones, mantenimiento de configuraciones por organización y mejoras en el flujo de creación/edición de órdenes.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organización Cargada Automáticamente (Priority: P1)

Como administrador del sistema, quiero que al iniciar la aplicación se cargue automáticamente la organización definida en la configuración, para no tener que seleccionarla manualmente en cada sesión.

**Why this priority**: Es la base sobre la que dependen múltiples pantallas y flujos del sistema. Sin organización activa, el resto de las funcionalidades de órdenes y catálogos no funcionan correctamente.

**Independent Test**: Puede probarse configurando un `OrganizationId` en `appsettings.json`, reiniciando la aplicación y verificando que la organización queda seleccionada automáticamente sin interacción del usuario.

**Acceptance Scenarios**:

1. **Given** que `appsettings.json` tiene un `OrganizationId` válido configurado, **When** la aplicación inicia, **Then** la organización se carga automáticamente y el usuario ve la organización activa sin necesidad de seleccionarla.
2. **Given** que `appsettings.json` no tiene `OrganizationId` configurado, **When** la aplicación inicia, **Then** la aplicación inicia normalmente sin organización activa, requiriendo selección manual.
3. **Given** que `appsettings.json` tiene un `OrganizationId` inválido o inexistente, **When** la aplicación inicia, **Then** se registra un log del error y la aplicación inicia sin organización activa, mostrando un mensaje claro al usuario.

---

### User Story 2 - Validación de Organización en Pantallas (Priority: P1)

Como usuario del sistema, quiero ver un mensaje claro y orientador cuando intento acceder a una pantalla que requiere una organización y ninguna está seleccionada, para saber exactamente qué acción debo tomar.

**Why this priority**: Previene confusión y errores en tiempo de uso. Sin esta validación, las pantallas dependientes fallan silenciosamente o muestran datos incorrectos.

**Independent Test**: Puede probarse accediendo a cualquier pantalla dependiente de organización sin haber seleccionado una, y verificando que el mensaje instructivo se muestra correctamente.

**Acceptance Scenarios**:

1. **Given** que no hay organización seleccionada, **When** el usuario navega a cualquier pantalla dependiente de organización (órdenes, catálogos, configuraciones), **Then** se muestra un mensaje claro indicando que debe seleccionar una organización, con instrucciones o enlace para hacerlo.
2. **Given** que hay una organización seleccionada, **When** el usuario navega a cualquier pantalla dependiente, **Then** la pantalla carga normalmente con los datos de esa organización.

---

### User Story 3 - CRUD de Organizaciones (Priority: P2)

Como administrador, quiero poder crear, ver, editar y eliminar organizaciones en el sistema, para mantener actualizado el catálogo de organizaciones con las que opera el negocio.

**Why this priority**: Sin este mantenimiento, la gestión de organizaciones depende de acceso directo a la base de datos, lo cual es riesgoso e ineficiente.

**Independent Test**: Puede probarse creando una nueva organización, editando sus datos y luego intentando eliminarla tanto cuando no tiene registros relacionados (debe permitirse) como cuando sí los tiene (debe bloquearse con mensaje claro).

**Acceptance Scenarios**:

1. **Given** el listado de organizaciones, **When** el usuario crea una nueva organización con datos válidos, **Then** la organización se guarda y aparece en el listado.
2. **Given** una organización existente, **When** el usuario edita sus datos y guarda, **Then** los cambios se persisten correctamente.
3. **Given** una organización sin registros relacionados en otras tablas, **When** el usuario la elimina, **Then** la organización se elimina exitosamente.
4. **Given** una organización que está siendo usada en órdenes u otros registros, **When** el usuario intenta eliminarla, **Then** el sistema bloquea la eliminación y muestra un mensaje explicativo amigable.

---

### User Story 4 - Configuración Única por Organización (Priority: P2)

Como administrador, quiero que el sistema me permita configurar exactamente una configuración por organización y me guíe a editarla si ya existe, para evitar duplicados que causen comportamiento inconsistente.

**Why this priority**: Las configuraciones duplicadas por organización generan inconsistencias en el tipo de cambio, impuesto por defecto y otros parámetros críticos del sistema.

**Independent Test**: Puede probarse intentando crear una segunda configuración para una organización que ya tiene una, verificando que el sistema impide la creación y redirige a la edición de la existente.

**Acceptance Scenarios**:

1. **Given** una organización sin configuración, **When** el usuario crea una configuración para ella, **Then** la configuración se guarda correctamente.
2. **Given** una organización que ya tiene una configuración, **When** el usuario intenta crear otra configuración para la misma organización, **Then** el sistema bloquea la creación y ofrece la opción de editar la configuración existente.
3. **Given** una organización con configuración existente, **When** el usuario accede a la pantalla de configuraciones y selecciona esa organización, **Then** se abre directamente el formulario de edición de la configuración existente.

---

### User Story 5 - Corrección de Visualización de Moneda (Priority: P2)

Como usuario del sistema, quiero ver la abreviación de la moneda (ej. "USD", "CRC") en lugar del ID numérico en las vistas de catálogos, para entender claramente con qué moneda estoy trabajando.

**Why this priority**: Actualmente los IDs numéricos en la interfaz son confusos y no aportan información útil al usuario. Es una corrección de usabilidad con alto impacto visual.

**Independent Test**: Puede probarse navegando a las vistas que muestran moneda (catálogos, formularios de orden) y verificando que la abreviación de la moneda aparece en lugar del ID.

**Acceptance Scenarios**:

1. **Given** el listado de catálogos o formulario de orden, **When** el usuario visualiza el campo de moneda, **Then** ve la abreviación (ej. "USD", "CRC") en lugar del ID numérico o alfanumérico.
2. **Given** que existen múltiples monedas en el sistema, **When** el usuario selecciona una moneda en cualquier combo/dropdown, **Then** las opciones muestran la abreviación correspondiente.

---

### User Story 6 - Mejoras en Creación y Edición de Órdenes (Priority: P1)

Como usuario que crea órdenes, quiero que el formulario de orden cargue automáticamente el tipo de cambio e impuesto de la organización activa, sugiera un nombre basado en el proveedor y la fecha, y me permita gestionar ítems de manera intuitiva con las restricciones de estado adecuadas, para agilizar el proceso de captura de órdenes y reducir errores de entrada.

**Why this priority**: Es el flujo principal del sistema (gestión de órdenes). Las mejoras impactan directamente la eficiencia operativa diaria del negocio.

**Independent Test**: Puede probarse creando una nueva orden: seleccionando un proveedor, verificando el nombre sugerido, confirmando que tipo de cambio e impuesto se cargaron automáticamente, y luego agregando, editando y eliminando ítems en estado "Pendiente". Luego cambiar el estado y verificar que los ítems quedan bloqueados.

**Acceptance Scenarios**:

1. **Given** el formulario de nueva orden, **When** el formulario carga, **Then** el campo Proveedor aparece primero y sin selección por defecto.
2. **Given** el formulario de nueva orden con Proveedor vacío, **When** el usuario selecciona un proveedor, **Then** el campo Nombre de la orden se pre-llena automáticamente con el formato `{NombreProveedor}-{DD}-{MM}-{YYYY}` basado en la fecha actual.
3. **Given** el formulario de nueva orden con una organización activa configurada, **When** el formulario carga, **Then** el campo Tipo de Cambio se carga automáticamente con el valor configurado en la organización activa.
4. **Given** el formulario de nueva orden con una organización activa configurada, **When** el formulario carga, **Then** el campo Impuesto se carga automáticamente con el porcentaje por defecto de la configuración de la organización activa.
5. **Given** una orden en estado "Pendiente", **When** el usuario intenta agregar, editar o eliminar ítems, **Then** todas estas acciones están habilitadas y funcionan correctamente.
6. **Given** una orden en cualquier estado diferente a "Pendiente", **When** el usuario intenta agregar, editar o eliminar ítems, **Then** estas acciones están completamente bloqueadas y se muestra un mensaje indicando que la orden no está en estado modificable.
7. **Given** que ocurre un error durante la creación de la orden, **When** el proceso falla, **Then** el sistema registra el log técnico detallado y muestra al usuario un mensaje amigable sin exponer información técnica.

---

### Edge Cases

- ¿Qué sucede si el tipo de cambio o impuesto no están configurados en la organización activa al crear una orden? → El campo queda vacío/en cero y el usuario puede ingresarlo manualmente; se muestra un aviso informativo.
- ¿Qué ocurre si el usuario modifica manualmente el nombre sugerido de la orden después de que fue auto-generado? → El nombre modificado manualmente se respeta sin sobrescribirse.
- ¿Qué pasa si la organización por defecto en `appsettings.json` fue eliminada de la base de datos? → Se registra log, se inicia sin organización activa y se muestra mensaje al usuario.
- ¿Qué ocurre si el usuario intenta eliminar la organización actualmente activa? → El sistema permite la eliminación solo si no tiene registros relacionados; en ese caso, la organización activa se limpia de la sesión.

---

## Requirements *(mandatory)*

### Functional Requirements

**Arquitectura y Manejo de Errores (aplicable a todos los cambios)**

- **FR-001**: Todo acceso a base de datos mediante Entity Framework Core y todas las reglas de negocio DEBEN residir exclusivamente en la capa de Services. Los controladores, Minimal APIs y componentes de UI solo invocan servicios.
- **FR-002**: Cualquier excepción en pantallas o procesos DEBE capturarse, registrarse en el sistema de logging con detalle técnico, y retornar/mostrar un mensaje amigable al usuario sin exponer stack traces.

**Organización por Defecto (Appsettings)**

- **FR-003**: El sistema DEBE soportar la configuración de un `OrganizationId` por defecto en `appsettings.json`.
- **FR-004**: Al iniciar la aplicación, si `OrganizationId` está configurado en `appsettings.json`, el sistema DEBE cargar automáticamente esa organización como organización activa.

**Validación de Organización en Pantallas**

- **FR-005**: El sistema DEBE implementar un mecanismo de validación (middleware, guardia o equivalente) que verifique si hay una organización activa antes de permitir el acceso a pantallas dependientes de organización.
- **FR-006**: Si no hay organización activa al acceder a una pantalla dependiente, el sistema DEBE mostrar un mensaje claro e instructivo al usuario indicando cómo seleccionar una organización.

**Corrección de Visualización de Moneda**

- **FR-007**: En todas las vistas donde se muestre el campo Moneda, el sistema DEBE mostrar la abreviación de la moneda (ej. "USD", "CRC") en lugar del ID numérico o alfanumérico.

**CRUD de Organizaciones**

- **FR-008**: El sistema DEBE proveer una pantalla de mantenimiento para Crear, Leer, Actualizar y Eliminar organizaciones.
- **FR-009**: El servicio de eliminación de organizaciones DEBE verificar que la organización no esté referenciada en otras tablas antes de permitir la eliminación. Si tiene registros relacionados, DEBE rechazar la operación con un mensaje amigable al usuario.

**Configuración Única por Organización**

- **FR-010**: El sistema DEBE restringir la creación de configuraciones a una sola configuración por organización.
- **FR-011**: Si una organización ya tiene una configuración existente, la interfaz y el servicio DEBEN impedir la creación de un nuevo registro y redirigir al usuario a editar la configuración existente.

**Mejoras en Creación y Edición de Órdenes**

- **FR-012**: En el formulario de creación/edición de órdenes, el campo Proveedor DEBE ser el primero en el formulario y no tener ningún valor seleccionado por defecto.
- **FR-013**: Al seleccionar un proveedor en el formulario de orden, el sistema DEBE sugerir automáticamente un nombre para la orden con el formato `{NombreProveedor}-{DD}-{MM}-{YYYY}` usando la fecha actual.
- **FR-014**: Al cargar el formulario de creación de orden, el campo Tipo de Cambio DEBE pre-cargarse con el valor configurado en la organización activa.
- **FR-015**: Al cargar el formulario de creación de orden, el campo Impuesto DEBE pre-cargarse con el porcentaje por defecto de la configuración de la organización activa.
- **FR-016**: El formulario de orden DEBE incluir funcionalidad para Agregar, Editar y Eliminar ítems (líneas) de la orden.
- **FR-017**: Las acciones de Agregar, Editar y Eliminar ítems de una orden DEBEN estar habilitadas únicamente cuando el estado de la orden sea "Pendiente". En cualquier otro estado, estas acciones DEBEN estar completamente bloqueadas en la interfaz y en el servicio.

### Key Entities

- **Organization**: Representa una organización del negocio. Atributos clave: identificador, nombre, estado activo/inactivo. Tiene una configuración asociada y puede estar referenciada en órdenes, catálogos y otros registros.
- **OrganizationConfiguration**: Configuración de parámetros operativos de una organización (tipo de cambio, impuesto por defecto, etc.). Relación uno-a-uno con Organization.
- **Currency (Moneda)**: Entidad de catálogo que representa una moneda. Atributos: identificador, nombre, abreviación (ej. "USD", "CRC").
- **Order (Orden)**: Orden de compra con proveedor, nombre, tipo de cambio, impuesto y estado. El estado "Pendiente" es el único que permite modificación de ítems.
- **OrderItem (Ítem de Orden)**: Línea de detalle de una orden con producto, cantidad, precio y otros atributos.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: La organización definida en la configuración se carga automáticamente al iniciar la aplicación en el 100% de los casos cuando el ID es válido.
- **SC-002**: Los usuarios que acceden a pantallas dependientes sin organización activa reciben un mensaje claro en menos de 1 segundo, sin que la pantalla falle o muestre datos incorrectos.
- **SC-003**: El 100% de las vistas que muestran moneda despliegan la abreviación en lugar del ID numérico.
- **SC-004**: No es posible crear más de una configuración por organización; el sistema redirige a edición en el 100% de los intentos de duplicación.
- **SC-005**: No es posible eliminar una organización con registros relacionados; el sistema bloquea y muestra mensaje amigable en el 100% de los intentos.
- **SC-006**: Al seleccionar un proveedor en el formulario de orden, el nombre sugerido se genera automáticamente en menos de 500 milisegundos.
- **SC-007**: El tipo de cambio y el impuesto por defecto se pre-cargan automáticamente en el formulario de orden en el 100% de los casos cuando la organización activa tiene configuración.
- **SC-008**: Las acciones de modificación de ítems en órdenes que no están en estado "Pendiente" están bloqueadas en el 100% de los casos, tanto en interfaz como en la capa de servicio.
- **SC-009**: Ningún error técnico (stack trace, mensaje de excepción interno) es visible al usuario final. Todos los errores muestran mensajes amigables.

---

## Assumptions

- El sistema tiene una base de datos relacional existente con las entidades Order, Organization, Currency y OrderItem ya definidas (al menos parcialmente) de la Fase 1.
- El sistema de logging/telemetría ya está configurado en la aplicación; estos cambios lo utilizan sin necesidad de configurarlo desde cero.
- La interfaz de usuario es una aplicación Blazor (según contexto del proyecto) con componentes existentes que serán modificados.
- "Estado Pendiente" es el único estado que permite edición de ítems; los demás estados (Aprobada, Cancelada, etc.) bloquean la edición.
- La abreviación de moneda (ej. "USD", "CRC") ya existe como campo en la entidad Currency en la base de datos.
- Los usuarios del sistema tienen acceso a la pantalla de configuración de `appsettings.json` o un administrador lo hace durante el despliegue.
- No se requiere soporte multi-tenant simultáneo; la organización activa es una por sesión de usuario.
