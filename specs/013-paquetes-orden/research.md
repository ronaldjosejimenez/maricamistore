# Research: 013-paquetes-orden

## R1 — Atomicity
- **Decision**: Add package and CxP entry (or reversal + removal) to the same `DbContext` and call `SaveChangesAsync` once.
- **Rationale**: EF Core wraps a single `SaveChanges` in a transaction; no explicit transaction needed. `CxPService.CreateAutoEntryAsync` saves on its own, so it is not reused here.

## R2 — Link entry ↔ package
- **Decision**: Nullable `CxPEntries.OrderPackageId` with `ON DELETE SET NULL`.
- **Rationale**: FR-009a; keeps the Auto-Paquete entry (possibly in a closed period) after the package is deleted. Allows CxP-side deletion of entries without touching packages (FR-009b).

## R3 — Separate service
- **Decision**: `OrderPackageService` instead of extending `OrderService`.
- **Rationale**: Cohesive rules (state checks, open period, reversal); `OrderService` is already large.

## R4 — Pending shipping query
- **Decision**: Single query with correlated subquery `Sum(packages)` per Active/Delivering order, compute `max(0, …)` in memory.
- **Rationale**: Small data; per-order clamp is required before summing (FR-011, clarification "aporta 0").

## R5 — Dropping ActualShippingAmountToCR
- **Decision**: Remove property/config; EF migration drops column (and its default constraint).
- **Rationale**: User decision (brainstorm). Production has 0 AutoDelivered entries; values unused.

## R6 — Negative amounts display
- **Decision**: Render "−" + abs value in `text-danger` in CxP entries table; subtotals may be negative.
- **Rationale**: Clarification; `formatMoney` would render "-" but without emphasis.
