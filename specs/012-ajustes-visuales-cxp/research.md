# Research: 012-ajustes-visuales-cxp

## R1 — Highlight style
- **Decision**: AdminLTE `info-box` with `bg-danger` (Deuda a Pagar) / `bg-warning` (Pendiente de Recoger), `info-box-icon`, bold number.
- **Rationale**: Chosen in brainstorm (option "Recuadro con color"); same size/structure as other boxes, already styled by AdminLTE.
- **Alternatives**: `small-box` (bigger, inconsistent size), larger number only (weak highlight) — rejected in brainstorm.

## R2 — Responsive columns
- **Decision**: Bootstrap 4 `col-md-4` ×3; stack below 768px.
- **Rationale**: Existing grid system; no custom CSS.

## R3 — Keeping values in sync
- **Decision**: Fill the new heading span in `loadPeriod()` from the same `porPagarEnColones` field.
- **Rationale**: `loadPeriod()` is the single refresh point after every mutation (add/delete entry, save fields, init/close period).

## R4 — Icons
- **Decision**: `fas fa-file-invoice-dollar` (Deuda a Pagar), `fas fa-hand-holding-usd` (Pendiente de Recoger) — Font Awesome 5 free icons bundled with AdminLTE 3.
