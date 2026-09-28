---
description: "Task list for 012-ajustes-visuales-cxp"
---

# Tasks: Ajustes Visuales de la Pantalla Cuentas por Pagar (CxP)

**Input**: `specs/012-ajustes-visuales-cxp/` (plan.md, spec.md, research.md, quickstart.md)

**Tests**: No automated test project; verification = `dotnet build` + quickstart.md (manual).

**Paths**: relative to repository root; web project in `MariCamiStore/`.

## Format: `[ID] [P?] [Story] Description`

US1 = labels, US2 = 3-column layout + highlight, US3 = total next to Entradas.

---

## Phase 1: Setup

- [ ] T001 Run `dotnet build MariCamiStore/MariCamiStore.csproj` to confirm a clean baseline.

## Phase 2: Foundational

None — all changes are in two presentation files.

---

## Phase 3: User Story 1 - Nombres sin ambigüedad (P1)

**Independent Test**: quickstart.md §1.

- [ ] T002 [US1] In `MariCamiStore/Pages/CxP/Index.cshtml` change the label of the box containing `#por-pagar-colones` from "Por pagar en Colones" to "Total por pagar colonizado", and the label of the box containing `#saldos-cobrar` from "Saldos por Cobrar" to "Saldos por Cobrar a Clientes". Keep element IDs. Per-currency labels in `MariCamiStore/wwwroot/js/pages/cxp/index.js` ("Por pagar en " + currencyName) stay unchanged (FR-001..003).

---

## Phase 4: User Story 2 - 3 columnas y resaltado (P1)

**Independent Test**: quickstart.md §2–4, §6, §8. Depends on T002 (same markup block).

- [ ] T003 [US2] In `MariCamiStore/Pages/CxP/Index.cshtml`, replace the two indicator rows (the `.row` with Por pagar/Saldos/Deuda/Pendiente and the `.row` with Shipping + `#por-pagar-monedas-container`) with ONE `<div class="row">` containing three `<div class="col-md-4">` columns, boxes stacked full width inside each (plan.md D1):
  - Column 1: `<div class="info-box bg-danger"><span class="info-box-icon"><i class="fas fa-file-invoice-dollar"></i></span><div class="info-box-content"><span class="info-box-text">Deuda a Pagar</span><span class="info-box-number font-weight-bold" id="deuda-pagar">—</span></div></div>` then the same structure with `bg-warning`, `fas fa-hand-holding-usd`, "Pendiente de Recoger", `id="pendiente-recoger"`.
  - Column 2: existing `bg-light` boxes "Shipping CR Pendientes" (`#shipping-pendientes`) and "Saldos por Cobrar a Clientes" (`#saldos-cobrar`).
  - Column 3: `bg-light` box "Total por pagar colonizado" (`#por-pagar-colones`) followed by `<div id="por-pagar-monedas-container"></div>` (no grid classes on the container).
  - Do not add equal-height utilities; leave Posición, `<hr />`, editable fields and buttons below untouched (FR-004..006, FR-009).
- [ ] T004 [US2] In `MariCamiStore/wwwroot/js/pages/cxp/index.js` `loadPeriod()`, change the per-currency markup to append each box without the `col-md-4 col-sm-6` wrapper: `'<div class="info-box bg-light"><div class="info-box-content"><span class="info-box-text">Por pagar en ' + escHtml(bal.currencyName) + '</span><span class="info-box-number">' + formatMoney(bal.amount, bal.sign) + '</span></div></div>'` so they stack full width in column 3 (plan.md D3).

---

## Phase 5: User Story 3 - Total junto a Entradas (P2)

**Independent Test**: quickstart.md §5, §7.

- [ ] T005 [US3] In `MariCamiStore/Pages/CxP/Index.cshtml` "Entries section" header: add `flex-wrap` to the `d-flex justify-content-between align-items-center` container and change the heading to `<h5 class="mb-0">Entradas del Período <small class="text-muted ml-2">Total por pagar colonizado: <span id="entradas-total-colonizado">—</span></small></h5>`, keeping `#btn-add-entry` as is (FR-007).
- [ ] T006 [US3] In `MariCamiStore/wwwroot/js/pages/cxp/index.js` `loadPeriod()`, right after filling `#por-pagar-colones`, set `$('#entradas-total-colonizado').text(formatMoney(data.porPagarEnColones, '₡'));` (FR-008) (depends on T005).

---

## Phase 6: Polish

- [ ] T007 Run `dotnet build MariCamiStore/MariCamiStore.csproj`; confirm no errors.
- [ ] T008 Verify by reading the final markup/JS that every ID used in `index.js` (`#por-pagar-colones`, `#saldos-cobrar`, `#deuda-pagar`, `#pendiente-recoger`, `#shipping-pendientes`, `#por-pagar-monedas-container`, `#entradas-total-colonizado`, `#posicion-value`, inputs and buttons) exists exactly once in `Index.cshtml`, and walk through quickstart.md scenarios by code reading.

---

## Dependencies

- T001 → all. T002 → T003 (same block). T003 ∥ T004 (different files). T005 after T003 (same file, different block). T006 after T005. Polish last.

## Implementation Strategy

Single small increment: US1 + US2 + US3 together, then build and manual verification.
