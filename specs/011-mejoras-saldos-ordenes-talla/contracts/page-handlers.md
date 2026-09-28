# Page Handler Contracts: 011-mejoras-saldos-ordenes-talla

## GET /Orders?statuses={csv}

- `statuses` optional.
  - absent → checkboxes Pending + Active checked.
  - present, empty → no checkbox checked; grid empty.
  - present with keys → those valid keys checked; unknown keys ignored; if none valid → default.
- Renders six checkboxes: `<input type="checkbox" class="status-filter" value="{Key}">` with label `{Name}`.

## GET /Orders?handler=Load&statuses={csv}

- Returns JSON array of orders (same shape as today) whose `Status` is in the parsed selection.
- Client never calls it with an empty selection (returns `[]` locally).
- Previous parameter name `statusFilter` is removed.

## GET /Orders/Items?orderId={guid}&statuses={csv}

- `statuses` optional; echoed unchanged into the "Volver" link: `/Orders?statuses={csv}`. When absent, "Volver" → `/Orders`.

## POST /Orders/Items?handler=Insert | handler=Update

- Request body (`OrderItemDto`) adds `size: string | null`.
- Server: `size = blank ? "N/A" : size.Trim()`; if `size.Length > 20` → `200 { "error": "La talla no puede superar 20 caracteres." }` and nothing is saved.
- Response adds `size`.

## GET /Orders/Items?handler=Load&orderId={guid}

- Each item adds `size` (string, `"N/A"` when not specified). Not displayed in the grid; used to prefill the edit modal.

## GET /Payments?handler=Saldos

- Unchanged. Total computed client-side.
