# Feature Specification: Pantalla de Transacciones

**Feature Branch**: `007-transactions-screen`

**Created**: 2026-06-09

**Status**: Draft

**Input**: Nueva pantalla de historial de transacciones con filtros, total condicional y modal de entrada manual

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver Historial de Transacciones con Filtros (Priority: P1)

El usuario navega a la nueva pantalla "Transacciones" y puede consultar el historial completo de transacciones de la organización, aplicando filtros por rango de fecha, cliente y tipo.

**Why this priority**: Es el núcleo de la feature. Sin esta historia, las demás no tienen contexto.

**Independent Test**: Navegar a `/Transactions` y confirmar que la tabla de transacciones carga con todos los registros. Aplicar cada filtro individualmente y verificar que los resultados se reducen correctamente.

**Acceptance Scenarios**:

1. **Given** que el usuario navega a `/Transactions`, **When** la página carga, **Then** se muestra la tabla con todas las transacciones de la organización (sin filtros aplicados), con columnas: Orden, Cliente, Tipo, Descripción, Monto, Fecha.
2. **Given** que la tabla está cargada, **When** el usuario selecciona un rango de fechas (Desde/Hasta) y hace clic en "Filtrar", **Then** solo se muestran transacciones cuya fecha cae en el rango.
3. **Given** que la tabla está cargada, **When** el usuario selecciona un cliente del dropdown y filtra, **Then** solo se muestran las transacciones de ese cliente.
4. **Given** que la tabla está cargada, **When** el usuario selecciona un tipo (Cargo / Pago / Anulación) y filtra, **Then** solo se muestran las transacciones de ese tipo Y se muestra una fila "Total" con la suma de montos.
5. **Given** que no hay filtro de tipo activo, **When** la tabla se muestra, **Then** NO aparece fila de Total (sumar tipos diferentes no tiene sentido semántico).
6. **Given** que la columna Orden, **When** la transacción tiene un `SourceId` vinculado a una orden, **Then** muestra el nombre de la orden; si no, muestra "—".

---

### User Story 2 - Registrar Transacción Manual desde Modal (Priority: P2)

El usuario puede abrir un modal desde la pantalla de Transacciones para registrar una transacción manual (pago, cargo u anulación) sin navegar a otra pantalla.

**Why this priority**: Complementa US1; permite accionar desde el historial.

**Independent Test**: Hacer clic en "Nueva Transacción", completar el modal y guardar. Verificar que la transacción aparece en la tabla con los datos correctos.

**Acceptance Scenarios**:

1. **Given** que el usuario está en `/Transactions`, **When** hace clic en "Nueva Transacción", **Then** se abre un modal con campos: Cliente (requerido), Tipo (requerido), Monto (requerido, > 0), Descripción (opcional).
2. **Given** que el modal está abierto con todos los campos requeridos completados, **When** el usuario hace clic en "Guardar", **Then** la transacción se guarda con `Source = Manual`, `SourceId = null`, `Status = Applied`, y el modal se cierra.
3. **Given** que el campo Descripción está vacío al guardar, **When** se procesa el guardado, **Then** se auto-genera la descripción como `"{Tipo} manual – {NombreCliente}"`.
4. **Given** que la transacción se guarda exitosamente, **When** el modal se cierra, **Then** la tabla se recarga con los filtros activos vigentes.
5. **Given** que el usuario hace clic en "Cancelar" o cierra el modal, **When** el modal se cierra, **Then** no se crea ninguna transacción.

---

### User Story 3 - Acceso desde Menú de Navegación (Priority: P3)

La nueva pantalla de Transacciones es accesible desde el menú lateral de la aplicación.

**Why this priority**: Prerequisito de usabilidad; sin ítem de menú la pantalla no es descubrible.

**Independent Test**: Verificar que el menú lateral contiene un ítem "Transacciones" que navega a `/Transactions`.

**Acceptance Scenarios**:

1. **Given** que el usuario está en cualquier pantalla, **When** ve el menú lateral, **Then** hay un ítem "Transacciones" bajo la sección de Finanzas (o donde aplique).
2. **Given** que el usuario hace clic en "Transacciones" del menú, **When** navega, **Then** llega a `/Transactions/Index`.

---

### Edge Cases

- Si los filtros no devuelven ninguna transacción → mostrar mensaje "No hay transacciones para los filtros seleccionados."
- Si la llamada AJAX falla → mostrar mensaje de error en el contenedor de la tabla.
- Si el Monto en el modal es 0 o negativo → mostrar error de validación y no guardar.
- Si Cliente o Tipo no se seleccionan en el modal → mostrar error y no guardar.
- Filtros de fecha vacíos → se ignoran (retorna todos los registros sin restricción de fecha).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE proveer una página en `/Transactions` con tabla de transacciones de la organización actual.
- **FR-002**: La tabla DEBE mostrar columnas: Orden (nombre o "—"), Cliente, Tipo, Descripción, Monto, Fecha (dd/MM/yyyy).
- **FR-003**: La tabla DEBE soportar filtros server-side por: rango de fechas (Desde/Hasta), Cliente (opcional), Tipo (opcional).
- **FR-004**: El total de montos DEBE mostrarse SOLO cuando hay un tipo de transacción seleccionado en el filtro.
- **FR-005**: La columna Orden DEBE resolver `Transaction.SourceId → OrderItem.Id → OrderItem.OrderId → Order.NameOfOrder`; si `SourceId` es null, mostrar "—".
- **FR-006**: El backend DEBE exponer `OnGetLoadAsync([FromQuery] TransactionFilterDto filter)` retornando `List<TransactionDto>` como JSON.
- **FR-007**: El sistema DEBE proveer un modal "Nueva Transacción" con campos: Cliente, Tipo, Monto (> 0), Descripción (opcional).
- **FR-008**: Al guardar transacción manual: `Source = "Manual"`, `SourceId = null`, `Status = "Applied"`, descripción auto-generada si vacía.
- **FR-009**: El backend DEBE exponer `OnPostCreateManualAsync([FromBody] ManualTransactionRequest)` para crear la transacción.
- **FR-010**: Tras guardar en el modal, la tabla DEBE recargarse con los filtros actuales vigentes.
- **FR-011**: El menú lateral DEBE incluir un ítem "Transacciones" que enlaza a `/Transactions/Index`.
- **FR-012**: Se DEBE crear `ITransactionService` con métodos `GetTransactionsAsync` y `CreateManualTransactionAsync`.

### Key Entities

- **TransactionDto**: Id, OrderName (string?), CustomerName (string?, null si CustomerId es null), TransactionType, TransactionDescription, TransactionAmount, TransactionDate
- **TransactionFilterDto**: DateFrom (DateTime?), DateTo (DateTime?), CustomerId (Guid?), TransactionType (string?)
- **ManualTransactionRequest**: CustomerId (Guid), TransactionType (string), Amount (decimal), Description (string?)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede consultar el historial completo de transacciones con 0 llamadas de API adicionales al cargar (tabla se carga con AJAX al iniciar).
- **SC-002**: Los filtros reducen los resultados correctamente; el Total solo aparece cuando hay tipo seleccionado.
- **SC-003**: Una transacción manual creada desde el modal aparece en la tabla en la siguiente recarga (< 3 segundos).
- **SC-004**: La pantalla es accesible en menos de 2 clics desde el menú lateral.

## Out of Scope

- Paginación de la tabla de transacciones.
- Edición o eliminación de transacciones existentes.
- Exportación a Excel/CSV.
- Filtro por organización (el global query filter de EF Core ya aplica el filtro por `OrganizationId`).

## Assumptions

- `Transaction`, `Order`, `OrderItem`, `Customer` ya existen en el modelo de datos.
- El join `SourceId → OrderItem.Id → OrderItem.OrderId → Order.NameOfOrder` es válido con las entidades actuales.
- `TransactionSource.Manual` ya existe como campo estático con Key `"Manual"` en el proyecto; no está incluido en `TransactionSource.List()`, por lo que la implementación DEBE asignar `Source = TransactionSource.Manual.Key` directamente (no via `FromKey()`).
- `TransactionType` acepta valores "Charge", "Payment", "Void" (strings existentes).
- La autenticación y el filtro por organización están configurados en el pipeline de ASP.NET Core.
- `OrganizationPageModel` es la clase base correcta para la nueva página.
- Para transacciones manuales, el `CurrencyId` DEBE tomarse de la organización activa del usuario (disponible vía `OrganizationPageModel`).
- `Transaction.CustomerId` es nullable en el modelo; para transacciones manuales siempre se provee y `CustomerName` se resuelve con join a `Customer.NickName` (o `Customer.Name` si NickName vacío).
