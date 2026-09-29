# Research: 014-cierre-mes-cxp

## R1 — Where to compute the preview
- **Decision**: Server endpoint (`ClosePreview`) returns closing and new-month values; JS re-requests on edits (debounced).
- **Rationale**: Same formulas as the panel (FR-010), including multi-currency conversion and Shipping CR Pendientes (which needs order data). Avoids duplicating math in JS.
- **Alternatives**: JS-side recalculation — rejected (duplicated formulas, needs shipping raw data).

## R2 — Double-close protection
- **Decision**: (1) disable button, (2) close by explicit period id, (3) SQL Server filtered unique index `OrganizationId WHERE IsClosed = 0`.
- **Rationale**: Chosen in brainstorm (approach A). The existing unique index on (Org, Month, Year) already blocks two concurrent closes of the *same* period creating the same next month; the filtered index additionally guarantees a single open period regardless of path.
- **Note**: EF Core `HasFilter` is SQL-Server specific syntax (`[IsClosed] = 0`), fine for this project.

## R3 — Carry-over math
- **Decision**: `debt = max(0, Deuda)`; `Saldo = max(0, debt − EnCuenta)`; `EnCuentaNuevo = max(0, EnCuenta − debt)`.
- **Rationale**: User clarification — the debt is paid from En Cuenta; negative debt treated as 0. Equals max(0, Pendiente) for non-negative debt.

## R4 — Pastel colors
- **Decision**: Bootstrap 4 alert palette (danger/success/warning backgrounds with their dark text colors).
- **Rationale**: Already-tuned contrast (WCAG AA for text), consistent with the app's Bootstrap/AdminLTE look.

## R5 — Validation layering
- **Decision**: HTML `min` + JS pre-check + handler check + service check.
- **Rationale**: FR-013/014 require server enforcement even when bypassing the form.
