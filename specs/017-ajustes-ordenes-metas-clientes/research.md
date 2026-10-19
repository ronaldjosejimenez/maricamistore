# Research: Ajustes en Órdenes, Metas y Clientes

## R1 — How to move existing Delivered orders
- **Decision**: An EF Core data-only migration with raw SQL (`INSERT` history + `UPDATE` status). `Down` is empty.
- **Rationale**: It runs once, with the deploy, in every environment, and the change is versioned with the code. Raw SQL skips the org query filter, so it covers all organizations, and it creates no Transactions or CxP rows.
- **Alternatives**: A one-off SQL script run manually (easy to forget, not versioned with the deploy). A startup data fixer in code (runs on every boot and adds noise).

## R2 — History record for migrated orders
- **Decision**: One row `Delivered → Completed`, with `TransitionDate = CreatedAt = SYSUTCDATETIME()` and `Notes = "Migración automática: el estado Entregada fue eliminado (spec 017)."`.
- **Rationale**: It gives traceability, as the clarification asked. The timestamps are UTC, consistent with `CreatedAt` elsewhere.

## R3 — Showing legacy "Delivered" in history
- **Decision**: A client-side key→label map in `items.js` for the history table, including `Delivered: 'Entregada'`.
- **Rationale**: History already renders raw keys. A map fixes legibility for all states without bringing `Delivered` back into `OrderStatus.List()`.
- **Alternatives**: Keep `Delivered` as a hidden legacy static on `OrderStatus` (it risks reappearing in lists, and `FromKey` semantics get murky).

## R4 — Read-only item view
- **Decision**: Reuse the existing item modal: populate it via `openEditItem`, then disable the controls, hide Save, and restore on close.
- **Rationale**: One form to maintain, and the fields and layout stay identical (FR-011).
- **Alternatives**: A separate read-only modal or details panel (duplicated markup).

## R5 — Where the commission rules live
- **Decision**: Server side, in a pure static helper used by `SalesGoalService`. Commission fields go on the DTOs.
- **Rationale**: This follows the 016 decision that all percentage and status rules live server-side and the JS only renders. One rule set serves both screens.
- **Alternatives**: Compute in JS for each page (duplicated in two files and drifts from the server rounding).

## R6 — Customer search
- **Decision**: Client-side: load the catalog once into a cache and filter in `loadData(filter)` with NFD accent stripping and lowercase.
- **Rationale**: Customers number in the hundreds, so results are instant, which meets SC-004. jsGrid's filter row works without server changes.
- **Alternatives**: Server-side filtering with a `Load` handler that takes params (needs SQL collation tricks for accents and adds round-trips).
