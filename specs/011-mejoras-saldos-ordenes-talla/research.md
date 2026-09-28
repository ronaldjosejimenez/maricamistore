# Research: 011-mejoras-saldos-ordenes-talla

## R1 — Where to compute the Saldos total

- **Decision**: Client-side in `renderSaldos` (payments/index.js) from the filtered array.
- **Rationale**: The text filter is already client-side over `allSaldosData`; the total must follow the visible rows (FR-002). No server round-trip needed.
- **Alternatives**: Server-computed total in the `Saldos` handler — rejected, it cannot reflect the client-side filter.

## R2 — Distinguishing "absent" vs "empty" `statuses`

- **Decision**: In `OnGetAsync`, use `Request.Query.ContainsKey("statuses")` plus the raw value; share one parsing helper for page and load handler.
- **Rationale**: ASP.NET Core model binding maps `?statuses=` to `null` for `string?`, the same as absence, so binding alone cannot implement FR-011 vs explicit empty selection.
- **Alternatives**: Sentinel value like `statuses=none` — rejected, less readable and leaks into URLs.

## R3 — Empty selection → empty list

- **Decision**: Client short-circuits empty selection (no AJAX); service `GetOrdersAsync` treats an empty collection as "no results".
- **Rationale**: Current service treats empty filter as "all", which contradicts FR-009. Changing the signature to a collection makes intent explicit; there is a single caller.
- **Alternatives**: Keep string signature and special-case in the page — rejected, keeps a misleading "empty = all" contract.

## R4 — Stale responses when toggling quickly

- **Decision**: Abort the previous in-flight `jqXHR` before starting a new load.
- **Rationale**: Simple and guarantees the grid shows the last selection (FR-013).
- **Alternatives**: Sequence counter ignoring late responses — equivalent but needs a custom deferred for jsGrid; debounce — adds latency, not needed.

## R5 — Carrying selection through Items → Volver

- **Decision**: Query string (`statuses`) on the Items link and on Volver (agreed in brainstorm, approach A).
- **Rationale**: Stateless, per-tab, explicit.
- **Alternatives**: sessionStorage flag (B), browser back (C) — rejected in brainstorm.

## R6 — Size column migration

- **Decision**: `nvarchar(20) NOT NULL` with SQL default `''` via `HasDefaultValue(string.Empty)`; migration generated with `dotnet ef migrations add AddOrderItemSize` (dotnet-ef 10.0.9 available).
- **Rationale**: Existing rows get `''` automatically (FR-017); matches other required string columns (e.g. `ProductSourceCode`).
- **Alternatives**: Nullable column — rejected by requirement (non-nullable).

## R7 — Size validation

- **Decision**: `maxlength="20"` on the input + server trim and length check returning `{ error }` JSON (existing error convention in Items handlers, e.g. image size).
- **Rationale**: Clarification Q2; consistent with current error handling in `items.js`.
