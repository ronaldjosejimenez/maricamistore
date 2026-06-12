# Contracts: Order Form Enhancements

## `Pages/Orders/Index` — New Handler

### Get Active Organization Configuration (NEW)
`GET /Orders?handler=Configuration`

Called by JS when opening the "New Order" modal to pre-populate exchange rate and tax.

Response (configuration exists):
```json
{
  "exchangeRate": 530.50,
  "taxPercentage": 13.0
}
```

Response (no configuration for active org):
```json
{
  "exchangeRate": null,
  "taxPercentage": null
}
```

---

## UI Behavior Contracts (JavaScript)

### Order Modal — Field Order
The order creation/edit modal MUST present fields in this sequence:
1. Proveedor (supplier select) — **first, no default selection on new order**
2. Nombre de la Orden — pre-filled on supplier change
3. Tipo de Cambio — pre-loaded from org configuration on new order
4. Impuesto (%) — pre-loaded from org configuration on new order
5. Envío Internacional — default 0
6. Descuento — default 0

### Supplier Change → Name Auto-Suggestion
When the user selects a supplier in a **new** order (id is empty):
- JS reads the selected option's text (supplier name)
- JS builds: `{SupplierName}-{DD}-{MM}-{YYYY}` using today's date
- JS sets the name field value to this string
- If the user manually edited the name field, it is NOT overwritten on subsequent supplier changes

### New Order Open → Configuration Pre-load
When `openNewOrder()` is called:
- JS calls `GET /Orders?handler=Configuration`
- On success: sets `#order-exchange-rate` and `#order-tax` to returned values (if not null)
- On null: leaves fields empty (user fills manually)
- On error: logs to console; leaves fields empty

---

## `Pages/Orders/Items` — Existing Behavior Clarification

### Add Item Button
The "Agregar ítem" row in jsGrid MUST be hidden when `Order.Status != "Pending"`.
- `Items.cshtml` already passes `Order.Status` to the page model
- JS reads the status and sets jsGrid `inserting: false` when not Pending

This prevents adding items to non-Pending orders both at the UI level and at the service level (`CreateOrderItemAsync` already validates status server-side).
