# Page Handler Contracts: 015-filtro-organizacion-pagos

All on `/Payments` (IndexModel). POSTs require the antiforgery header used by the existing handler.

## GET / (page load)

Server renders the Organization `<select>` (`#payment-org-filter`) with `<option value="">Todas</option>` first, then one `<option value="{Id}">{Name}</option>` per organization from `GetOrganizationsAsync()`, marking the session's current organization `selected`.

## GET ?handler=Saldos&organizationId={guid?}
- `organizationId` omitted/empty → same shape and values as today (global, `IgnoreQueryFilters()`, no org predicate).
- `organizationId={guid}` → only that organization's transactions.
- Response shape unchanged: `[{ customerId, customerName, balance, isGeneric }]`.

## GET ?handler=Balance&customerId={guid}&organizationId={guid?}
- Response unchanged shape: `{ customerId, customerName, globalBalance, orgBalance }`.
- `globalBalance`: always all organizations (unchanged).
- `orgBalance`: organization from `organizationId` when provided; equals `globalBalance` when `organizationId` is omitted/empty ("Todas").

## POST ?handler=RegisterPayment
Body: `{ "customerId": "…", "amount": 30.00, "organizationId": "…" | null }`
- `organizationId == null` → `{ "success": false, "error": "Seleccione una organización específica para registrar el pago." }`, no transaction created.
- `organizationId` present → transaction created with that organization; response `{ "success": true, "balance": { customerId, customerName, globalBalance, orgBalance } }` where `orgBalance` reflects the same `organizationId` just used.
- Existing validation (`customerId`/`amount`) unchanged and still runs first.
