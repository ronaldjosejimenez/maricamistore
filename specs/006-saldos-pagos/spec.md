# Feature Specification: Pantalla Unificada Saldos + Pagos

**Feature Branch**: `006-saldos-pagos`

**Created**: 2026-06-08

**Status**: Draft

**Input**: Unificación de pantalla de Pagos con tabla de Saldos de clientes

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver Saldos Pendientes en Pantalla de Pagos (Priority: P1)

El usuario abre la pantalla de Pagos y ve inmediatamente la tabla de clientes con saldo pendiente, sin necesidad de navegar a otra pantalla.

**Why this priority**: Es el núcleo del cambio; elimina la fricción de tener dos pantallas separadas para una tarea relacionada. Sin esta historia no hay valor entregado.

**Independent Test**: Puede verificarse navegando a `/Payments` y confirmando que la tabla de saldos se muestra con datos correctos.

**Acceptance Scenarios**:

1. **Given** que el usuario está en `/Payments`, **When** la página carga, **Then** aparece una sección "Clientes con Saldo Pendiente" debajo del formulario de pago, mostrando clientes con balance > 0 ordenados por nombre/apodo ASC.
2. **Given** que la tabla está visible, **When** el usuario escribe en el campo de filtro, **Then** las filas se filtran en tiempo real (sin llamada al servidor) mostrando solo los clientes cuyo nombre contiene el texto ingresado.
3. **Given** que hay clientes con saldo, **When** la tabla se muestra, **Then** aparece una fila "Total" al final con la suma de todos los saldos visibles.
4. **Given** que no hay clientes con saldo pendiente, **When** la página carga, **Then** la tabla muestra un mensaje "No hay saldos pendientes".

---

### User Story 2 - Registrar Pago y Actualizar Saldos Automáticamente (Priority: P2)

El usuario registra un pago desde el formulario y la tabla de saldos se actualiza automáticamente para reflejar el nuevo balance sin recargar la página.

**Why this priority**: Cierra el ciclo de trabajo: registrar pago → ver impacto inmediato en saldos. Depende de US1.

**Independent Test**: Puede verificarse registrando un pago y confirmando que la fila del cliente desaparece o su saldo disminuye en la tabla.

**Acceptance Scenarios**:

1. **Given** que hay un cliente con saldo visible en la tabla, **When** el usuario registra un pago completo para ese cliente, **Then** la fila del cliente desaparece de la tabla de saldos (saldo resultante = 0).
2. **Given** que hay un cliente con saldo visible, **When** el usuario registra un pago parcial, **Then** la fila del cliente muestra el saldo reducido.
3. **Given** que el filtro tiene texto activo, **When** se actualiza la tabla tras un pago, **Then** el filtro se mantiene aplicado al resultado actualizado.

---

### User Story 3 - Eliminar Pantalla Redundante de Saldos (Priority: P3)

La pantalla `/Reports/Saldos` es eliminada y el ítem de menú correspondiente desaparece, ya que su funcionalidad está disponible en `/Payments`.

**Why this priority**: Limpieza necesaria para evitar confusión y duplicación, pero no bloquea el valor principal.

**Independent Test**: Puede verificarse confirmando que `/Reports/Saldos` devuelve 404 y el menú no muestra el ítem.

**Acceptance Scenarios**:

1. **Given** que el usuario accede a `/Reports/Saldos`, **When** la página carga, **Then** recibe error 404 (página no encontrada).
2. **Given** que el usuario ve el menú de navegación, **When** busca "Saldos" o "Cuentas por Cobrar", **Then** ese ítem ya no aparece en el menú.

---

### Edge Cases

- ¿Qué pasa si la llamada AJAX para cargar saldos falla? → Mostrar mensaje de error en el contenedor de la tabla.
- ¿Qué pasa si el filtro está activo y el pago actualiza la tabla? → El filtro se reaplicará sobre los datos nuevos.
- ¿Qué pasa si no hay clientes con saldo? → La tabla muestra mensaje vacío apropiado.
- ¿Qué pasa con el Total cuando el filtro está activo? → El Total refleja solo los saldos visibles (filtrados).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La página `/Payments` DEBE mostrar la tabla de "Clientes con Saldo Pendiente" debajo del formulario de registro de pago.
- **FR-002**: La tabla de saldos DEBE cargarse mediante AJAX (`GET ?handler=Saldos`) al cargar la página.
- **FR-003**: La tabla DEBE mostrar columnas "Cliente" y "Saldo Pendiente", ordenada por `CustomerName` ASC (nombre de display que ya incluye apodo si aplica).
- **FR-004**: La tabla DEBE incluir una fila "Total" al final con la suma de los saldos mostrados.
- **FR-005**: El campo de filtro (`#saldos-filter`) DEBE filtrar filas en tiempo real sin llamadas adicionales al servidor.
- **FR-006**: Tras registrar un pago exitosamente, el sistema DEBE refrescar la tabla de saldos automáticamente volviendo a llamar al endpoint `?handler=Saldos` (misma llamada AJAX que la carga inicial).
- **FR-007**: El backend DEBE exponer un handler `OnGetSaldosAsync` que retorne `List<SaldoReportRow>` como JSON.
- **FR-008**: `GetSaldosReportAsync` DEBE ordenar resultados por nombre de cliente ASC.
- **FR-009**: Las páginas `Reports/Saldos.cshtml` y `Reports/Saldos.cshtml.cs` DEBEN ser eliminadas.
- **FR-010**: El ítem de menú "Saldos" (bajo "Cuentas por Cobrar") DEBE ser eliminado de la navegación.

### Key Entities

- **SaldoReportRow**: Fila de saldo (ya existe) — CustomerId, CustomerName, Balance. Se reutiliza sin cambios.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede ver saldos de clientes sin salir de la pantalla de pagos (0 navegaciones adicionales necesarias).
- **SC-002**: La tabla de saldos se actualiza en menos de 2 segundos después de registrar un pago exitoso.
- **SC-003**: El filtro de clientes responde visualmente en menos de 100ms (operación JS local).
- **SC-004**: La pantalla `/Reports/Saldos` deja de existir y el menú no contiene ítems redundantes.

## Out of Scope

- Paginación o búsqueda server-side de saldos (el filtro JS opera sobre datos ya cargados).
- Modificaciones al modelo `SaldoReportRow` (se reutiliza sin cambios).
- Cambios en la URL `/Payments` o en su configuración de autenticación/autorización.
- Nuevas rutas o redirecciones desde `/Reports/Saldos` (simplemente devuelve 404).

## Assumptions

- `SaldoReportRow` ya existe en `IPaymentService.cs` y no requiere modificaciones de modelo.
- La lógica de cálculo de saldos en `GetSaldosReportAsync` ya es correcta; solo se agrega ordenamiento.
- El filtro JS opera sobre los datos ya cargados (no requiere soporte de búsqueda server-side).
- La URL `/Payments` se mantiene sin cambios; no se crea nueva ruta.
- La autenticación y autorización de la página de Pagos ya está configurada correctamente.
