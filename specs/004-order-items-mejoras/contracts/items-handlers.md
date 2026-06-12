# Contracts: Items Handlers

**Page**: `Pages/Orders/Items.cshtml`
**Feature**: `specs/004-order-items-mejoras`

---

## Modified Handlers

### GET `?handler=Load&orderId={orderId}` — Updated Response

Returns the ordered list of items for an order. **Changed** to include `IsReceived` and `CustomerDisplayName`, pre-sorted.

**Sort order**: `CustomerDisplayName` ASC, then `CreatedAt` DESC.

**Response** (array of):
```json
{
  "id": "guid",
  "orderId": "guid",
  "customerId": "guid",
  "customerDisplayName": "string",     // NEW: NickName ?? Name ?? Id.ToString()
  "productDescription": "string",
  "productLink": "string | null",
  "productSourceCode": "string | null",
  "hasImage": "bool",
  "productTypeId": "guid",
  "listPrice": "decimal",
  "listPriceTaxWithTax": "decimal",
  "realPrice": "decimal",
  "estimateShipping": "decimal",
  "serviceFeeInLocal": "decimal",
  "agreedPriceInLocal": "decimal",
  "isReceived": "bool",                // NEW
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

---

## New Handlers

### POST `?handler=ToggleReceived&orderId={orderId}`

Toggles the `IsReceived` flag for a single order item.

**Guards**: Caller should only invoke when order status is `Delivering` or `Delivered`. Backend validates this as well.

**Request body**:
```json
{
  "itemId": "guid",
  "isReceived": true
}
```

**Success response**:
```json
{ "success": true }
```

**Error response**:
```json
{ "success": false, "error": "string" }
```

**Error cases**:
- Item not found → `{ "success": false, "error": "Ítem no encontrado." }`
- Order not in Delivering/Delivered → `{ "success": false, "error": "Solo se puede verificar recepción en órdenes en estado Entregando o Entregada." }`

---

## Unchanged Handlers

The following handlers remain unchanged in signature and behavior:

| Handler | Method | Notes |
|---------|--------|-------|
| `?handler=Insert` | POST | DTO extended with no new required fields (IsReceived defaults server-side) |
| `?handler=Update` | POST | Same — IsReceived is not part of the item edit flow |
| `?handler=Delete` | POST | Unchanged |
| `?handler=UpdateTotals` | POST | Unchanged |
| `?handler=UpdateOrder` | POST | Unchanged |
| `?handler=ProductType` | GET | Unchanged |
| `?handler=ProductTypesByCurrency` | GET | Unchanged |
| `?handler=ItemImage` | GET | Unchanged |
| `?handler=History` | GET | Unchanged |
