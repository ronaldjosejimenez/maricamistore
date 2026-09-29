# Data Model: 015-filtro-organizacion-pagos

No schema changes. No new entities, columns or migrations.

## Organization (unchanged)

Used as-is (`Id`, `Name`) to populate the filter combo via the existing `IOrganizationService.GetOrganizationsAsync()`.

## Transaction (unchanged)

`OrganizationId` already exists. This feature changes **which** organization id is used when creating a `Payment` transaction (the filter's explicit choice instead of always the session's), and adds a read path that scopes balance queries to an explicit organization id instead of the session one.

## Filter selection (client/request-only value, not persisted)

| Value | Meaning |
|-------|---------|
| `null` / empty | "Todas": saldos global (unchanged `IgnoreQueryFilters()`); register-payment disabled/rejected |
| a real `Organization.Id` | Saldos and register-payment scoped to that organization |

Not stored anywhere; recomputed from the session's current organization on every page load (spec Assumptions).
