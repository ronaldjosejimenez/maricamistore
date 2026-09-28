# Implementation Plan: Ajustes Visuales de la Pantalla Cuentas por Pagar (CxP)

**Branch**: `012-ajustes-visuales-cxp` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/012-ajustes-visuales-cxp/spec.md`

## Summary

Presentation-only changes to the CxP page: rename two indicators, restructure the "Control del Mes" indicator area into three equal Bootstrap columns (column 1 highlighted with AdminLTE colored `info-box`es), and show the "Total por pagar colonizado" value next to the "Entradas del Período" heading. No server, service, DTO or data changes: every value already arrives in the `Period` handler response and is rendered in `loadPeriod()`.

## Technical Context

**Language/Version**: C# / .NET 10 Razor Pages (markup only), JavaScript (jQuery)

**Primary Dependencies**: AdminLTE 3 (Bootstrap 4 grid, `info-box`, `bg-danger`/`bg-warning`), Font Awesome (already loaded)

**Storage**: N/A (no data changes)

**Testing**: No automated test project; `dotnet build` + manual verification ([quickstart.md](quickstart.md))

**Target Platform**: Web app (desktop + phone widths)

**Project Type**: Single web application (`MariCamiStore/`)

**Constraints**: Values must be identical to before (FR-010); Posición, editable fields, buttons, TC warning, init section and entries table unchanged (FR-009)

**Scale/Scope**: 2 files

## Constitution Check

`.specify/memory/constitution.md` is the unfilled template — no ratified principles. Gate: **PASS**. Post-design: **PASS** (no new dependencies, layers or files).

## Project Structure

### Documentation (this feature)

```text
specs/012-ajustes-visuales-cxp/
├── plan.md          ← this file
├── research.md      ← Phase 0
├── quickstart.md    ← Phase 1 (manual verification)
└── tasks.md         ← Phase 2
```

No `data-model.md` or `contracts/`: no entities or interfaces change.

### Source Code

```text
MariCamiStore/
├── Pages/CxP/Index.cshtml              [MODIFY] indicator layout (3 columns), labels, Entradas heading total span
└── wwwroot/js/pages/cxp/index.js       [MODIFY] loadPeriod(): fill new total span; per-currency boxes full width in column 3
```

## Design Decisions

### D1 — Three-column layout (FR-004, FR-006)

Replace the two indicator `.row`s in the period card body with one row:

```html
<div class="row">
  <div class="col-md-4"> <!-- col 1: highlighted -->
    info-box bg-danger  (icon fas fa-file-invoice-dollar)  "Deuda a Pagar"        #deuda-pagar
    info-box bg-warning (icon fas fa-hand-holding-usd)     "Pendiente de Recoger" #pendiente-recoger
  </div>
  <div class="col-md-4"> <!-- col 2 -->
    info-box bg-light "Shipping CR Pendientes"        #shipping-pendientes
    info-box bg-light "Saldos por Cobrar a Clientes"  #saldos-cobrar
  </div>
  <div class="col-md-4"> <!-- col 3 -->
    info-box bg-light "Total por pagar colonizado"    #por-pagar-colones
    <div id="por-pagar-monedas-container"></div>      (per-currency boxes, full width)
  </div>
</div>
```

- Bootstrap `col-md-4` → equal widths ≥768px; below that each column is full width and stacks in order 1, 2, 3 (FR-006).
- Columns keep natural height (no `h-100`/equal-height utilities), aligned to top (edge case "Muchas monedas").
- Highlighted boxes: AdminLTE `info-box bg-danger` / `bg-warning` colors the whole box; `<span class="info-box-icon"><i class="fas …"></i></span>` on the left; number wrapped in `font-weight-bold` (FR-005). Color does not depend on value sign (clarification).
- Element IDs are kept, so existing JS fills continue to work.

### D2 — Labels (FR-001..003)

- `#por-pagar-colones` label → "Total por pagar colonizado".
- `#saldos-cobrar` label → "Saldos por Cobrar a Clientes".
- Per-currency boxes keep `'Por pagar en ' + currencyName`.

### D3 — Per-currency boxes in column 3

In `loadPeriod()`, the per-currency markup drops the `col-md-4 col-sm-6` wrapper (previously a grid inside `col-md-9`); each box is appended directly as `<div class="info-box bg-light">…</div>` so they stack full width under "Total por pagar colonizado".

### D4 — Total next to "Entradas del Período" (FR-007, FR-008)

- Heading row becomes: `<h5 class="mb-0">Entradas del Período <small class="text-muted ml-2">Total por pagar colonizado: <span id="entradas-total-colonizado">—</span></small></h5>` plus the existing button; the flex container gets `flex-wrap` so the button can wrap on phones.
- `loadPeriod()` sets `#entradas-total-colonizado` with the same `formatMoney(data.porPagarEnColones, '₡')` used for `#por-pagar-colones` → always in sync; `loadPeriod()` already runs on load, add/delete entry, save fields, close and init period (FR-008).
- The span is outside `#btn-add-entry`, so hiding the button on closed periods leaves it visible.

## Complexity Tracking

Not applicable.
