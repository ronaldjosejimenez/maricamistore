# Tasks: Quick Fixes — Decimales en Envío Estimado y SourceId Null

**Input**: Design documents from `/specs/005-quick-fixes/`

**Prerequisites**: plan.md ✓, spec.md ✓

---

## Phase 3: User Story 1 — Decimales en Envío Estimado (Priority: P1) 🎯

**Goal**: El campo "Envío Estimado" en Tipos de Producto acepta valores decimales.

**Independent Test**: Abrir `/ProductTypes`, editar cualquier registro, ingresar `12.50` en "Envío Estimado" → campo acepta y persiste el valor decimal.

### Implementación

- [X] T001 [US1] Agregar `step: 0.01` al field `estimateShipping` en `MariCamiStore/wwwroot/js/pages/product-types/index.js:44`

**Checkpoint**: Campo de envío estimado acepta decimales en el browser.

---

## Phase 4: User Story 2 — SourceId null en pagos manuales (Priority: P1)

**Goal**: Los pagos manuales registran `SourceId = null` en lugar de `Guid.Empty`.

**Independent Test**: Registrar un pago en `/Payments` → verificar en BD que `SourceId IS NULL` para la transacción creada.

### Implementación

- [X] T002 [US2] Cambiar `SourceId = Guid.Empty` → `SourceId = null` en `MariCamiStore/Services/PaymentService.cs:36`

**Checkpoint**: Nuevos pagos manuales tienen `SourceId = NULL` en la base de datos.

---

## Dependencies & Execution Order

- T001 y T002 son completamente independientes — pueden ejecutarse en cualquier orden o en paralelo (archivos distintos sin dependencias entre sí).

---

## Implementation Strategy

### Ejecución (2 minutos)

1. Modificar `product-types/index.js` — línea 44: agregar `step: 0.01`
2. Modificar `PaymentService.cs` — línea 36: cambiar `Guid.Empty` a `null`
3. Verificar en navegador y BD

---

## Notes

- No hay tests automatizados para estos cambios (verificación manual)
- No se requieren migraciones de BD
- Total: 2 tareas, 2 archivos, 2 líneas modificadas
