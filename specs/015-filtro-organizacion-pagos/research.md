# Research: 015-filtro-organizacion-pagos

## R1 — "Todas" wire representation
- **Decision**: `Guid? organizationId`, `null` = "Todas".
- **Rationale**: Idiomatic nullable type; avoids a `Guid.Empty` sentinel that could theoretically collide with a real id and already means "no session org" elsewhere (`CurrentOrganizationService`, `OrganizationPageModel.CheckOrganization`). Resolves spec review finding #3.
- **Alternatives**: `Guid.Empty` sentinel — rejected, overloads a value that already has a different meaning in this codebase.

## R2 — Scoping balances to an arbitrary chosen organization
- **Decision**: Reuse the existing `IgnoreQueryFilters()` + explicit `.Where(t => t.OrganizationId == id)` pattern already used by `GetSaldosReportAsync` (global) and add the same pattern parameterized by an explicit id, instead of relying on the `Transaction` EF query filter (which is pinned to the *session* org via `ICurrentOrganizationService`, not to a page-level filter).
- **Rationale**: The chosen organization in the Payments filter is independent from the session's active organization (spec Story 2); the EF query filter cannot express "this other, non-session organization" without changing `ICurrentOrganizationService` itself, which the spec explicitly puts out of scope (FR-004).
- **Alternatives**: Temporarily overriding `ICurrentOrganizationService` for the request — rejected in the brainstorm (Approach B) as fragile and reaching outside Payments.

## R3 — Stale requests on rapid filter changes
- **Decision**: Abort the previous in-flight request before issuing a new one, same pattern as `wwwroot/js/pages/cxp/index.js` (`closePreviewRequest`).
- **Rationale**: Existing, proven pattern in this codebase; keeps the UI consistent with what the user last selected (spec edge case).

## R4 — Payment rejection message (FR-013)
- **Decision**: "Seleccione una organización específica para registrar el pago." — same wording as the brainstorm's UI legend (FR-011), reused as the server error to keep the message consistent whether triggered by the disabled button's guard or a direct call.
