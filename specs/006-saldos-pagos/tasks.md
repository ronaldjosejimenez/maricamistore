# Tasks: Pantalla Unificada Saldos + Pagos

**Feature**: 006-saldos-pagos | **Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

## Phase 1 — Backend (US1 + US3 setup)

- [X] T001 [US1] Agregar `OnGetSaldosAsync()` en `MariCamiStore/Pages/Payments/Index.cshtml.cs` después de `OnGetBalanceAsync`
- [X] T002 [P] [US1] Agregar `.OrderBy(r => r.CustomerName)` antes de `.ToList()` en `MariCamiStore/Services/PaymentService.cs` (~línea 79, método `GetSaldosReportAsync`)

## Phase 2 — Frontend (US1)

- [X] T003 [US1] Agregar sección HTML de tabla de saldos con filtro en `MariCamiStore/Pages/Payments/Index.cshtml` (insertar antes de `<script src="/js/pages/payments/index.js">`)
- [X] T004 [US1] Agregar funciones `loadSaldos()` y `renderSaldos()`, listener del filtro, y llamada inicial en `MariCamiStore/wwwroot/js/pages/payments/index.js`

## Phase 3 — Refresco tras pago (US2)

- [X] T005 [US2] En `MariCamiStore/wwwroot/js/pages/payments/index.js`, dentro del callback `if (r.success)` del botón de pago, agregar llamada `loadSaldos()`

## Phase 4 — Limpieza (US3)

- [X] T006 [P] [US3] Eliminar `MariCamiStore/Pages/Reports/Saldos.cshtml`
- [X] T007 [P] [US3] Eliminar `MariCamiStore/Pages/Reports/Saldos.cshtml.cs`
- [X] T008 [US3] Eliminar ítem de menú Saldos (líneas 243-247) en `MariCamiStore/Pages/Shared/_Layout.cshtml`

## Dependencies

- T003 depende de T001 (el handler debe existir antes de testear la llamada AJAX)
- T004 depende de T003 (el DOM del contenedor debe existir)
- T005 depende de T004 (`loadSaldos` debe estar definida)
- T006, T007, T008 son independientes entre sí y del resto

## Implementation Strategy

MVP (P1): T001 → T002 → T003 → T004 — entrega la tabla de saldos visible
P2: T005 — cierra el ciclo de refresco tras pago
P3: T006 → T007 → T008 — limpia la pantalla redundante
