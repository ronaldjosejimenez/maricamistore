# Feature Specification: Quick Fixes — Decimales en Envío Estimado y SourceId Null

**Feature Branch**: `005-quick-fixes`

**Created**: 2026-06-08

**Status**: Draft

**Input**: Quick fixes — (1) agregar `step: 0.01` al field `estimateShipping` en jsGrid de product-types/index.js para permitir decimales; (2) cambiar `SourceId = Guid.Empty` a `SourceId = null` en PaymentService.RegisterPaymentAsync porque SourceId es Guid? y debe ser null cuando no hay origen de orden.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Ingresar precio de envío estimado con decimales (Priority: P1)

El usuario está gestionando tipos de producto y quiere registrar un costo de envío estimado con centavos (ej: $12.50). Actualmente el campo no acepta decimales y redondea al entero más cercano.

**Why this priority**: Es un defecto directo que impide ingresar datos correctos. Afecta la precisión de los cálculos de costo.

**Independent Test**: Puede probarse independientemente abriendo `/ProductTypes`, editando cualquier tipo de producto e ingresando un valor decimal en "Envío Estimado".

**Acceptance Scenarios**:

1. **Given** el usuario está en la pantalla de Tipos de Producto, **When** intenta ingresar `12.50` en el campo "Envío Estimado", **Then** el campo acepta el valor y lo almacena como `12.50`.
2. **Given** el usuario ingresa `0.99` en "Envío Estimado", **When** guarda el registro, **Then** el valor se persiste correctamente con dos decimales.
3. **Given** el usuario navega con el teclado en el campo numérico, **When** usa las flechas arriba/abajo, **Then** el incremento/decremento es de `0.01` (no de `1`).

---

### User Story 2 — Registro de pago manual con SourceId correcto (Priority: P1)

Al registrar un pago manual (sin orden asociada), el sistema debe registrar `SourceId` como nulo, no como un GUID vacío. Esto es importante para la correcta identificación de transacciones manuales en la pantalla de Transacciones.

**Why this priority**: Corrección semántica que afecta la integridad de los datos y futuras pantallas de consulta de transacciones.

**Independent Test**: Puede probarse registrando un pago desde `/Payments` y verificando en la base de datos que `SourceId IS NULL`.

**Acceptance Scenarios**:

1. **Given** un pago manual es registrado desde la pantalla de Pagos, **When** se crea la transacción, **Then** el campo `SourceId` queda como `NULL` en la base de datos (no como `00000000-0000-0000-0000-000000000000`).
2. **Given** una transacción con `SourceId = NULL`, **When** se consulta en la pantalla de Transacciones (futura), **Then** la columna "Orden" muestra "—" en lugar de buscar una orden inexistente.

---

### Edge Cases

- ¿Qué pasa si el usuario ingresa más de 2 decimales en Envío Estimado? → El campo acepta el valor pero la BD lo almacena con precisión `decimal(18,2)`, redondeando si aplica.
- ¿Los pagos existentes con `SourceId = Guid.Empty` se ven afectados? → No, este fix solo aplica a nuevos pagos; los existentes no se migran.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El campo "Envío Estimado" en la pantalla de Tipos de Producto DEBE aceptar valores con hasta dos decimales.
- **FR-002**: Al navegar el campo "Envío Estimado" con teclado, el incremento DEBE ser de `0.01`.
- **FR-003**: Al registrar un pago manual (sin orden asociada), el campo `SourceId` de la transacción DEBE quedar como `NULL`.
- **FR-004**: Los pagos manuales existentes con `SourceId = Guid.Empty` NO DEBEN modificarse — solo los nuevos pagos son afectados.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El campo "Envío Estimado" acepta y persiste valores decimales (ej: `12.50`) sin error ni truncamiento.
- **SC-002**: El 100% de los nuevos pagos manuales registrados tienen `SourceId = NULL` en la base de datos.
- **SC-003**: No se requieren migraciones de base de datos para implementar estos cambios.

## Assumptions

- El modelo `decimal(18,2)` en la BD para `EstimateShipping` ya soporta decimales — solo el frontend limitaba el ingreso.
- La columna `SourceId` en la tabla `Transactions` ya admite `NULL` — no requiere migración.
- Los pagos existentes con `SourceId = Guid.Empty` (si los hay) quedan como están; la corrección es solo hacia adelante.
- El cambio de `SourceId` no afecta la lógica de cálculo de saldos ni otros procesos que lean transacciones.
