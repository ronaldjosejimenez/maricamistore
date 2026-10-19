# UI / Handler Contracts

## Orders

### `POST /Orders?handler=Transition` (existing)
- `toStatus = "Delivered"` (or any order currently `Delivered`) → `{ success: false, error: "Transición 'X' → 'Y' no permitida." }`
- `Delivering → Completed` → `{ success: true }`

### `GET /Orders?handler=Load` (existing)
- `nextStatuses` for Active: `["Delivering","Voided"]`; for Delivering: `["Completed","Voided"]`; for Completed: `[]`.

### `POST /Orders/Items?handler=ToggleReceived` (existing)
- Order not in `Delivering` → `{ success: false, error: "Solo se puede marcar recepción en órdenes en estado Entregando." }`

### `GET /Orders/Items?handler=History` (existing, unchanged)
- Returns raw keys (`fromStatus`, `toStatus`). The client maps them to labels, with legacy `Delivered` → "Entregada".

## Sales goals

### `GoalDto` JSON (Mes Actual load/update responses), new fields
```json
{ "commissionRate": 5.0, "commissionAmount": 50000.00, "commissionStatus": "green" }
```
`commissionStatus` ∈ `red | yellow | green | green-plus`

### `HistoryRowDto` JSON (Histórico list), new fields
Same three fields.

## Customers

### `GET /Customers?handler=Load` (existing, unchanged)
Returns the full catalog. All filtering happens on the client.
