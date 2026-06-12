# Contract: Items Reassignment Handler

**Feature**: `010-flexibilidad-items-activos`
**Page**: `MariCamiStore/Pages/Orders/Items.cshtml.cs`
**Pattern**: ASP.NET Core Razor Pages named handler (`?handler=Reasignar`)

---

## Handler: `OnPostReasignarAsync`

### Endpoint

```
POST /Orders/Items?handler=Reasignar
Content-Type: application/json
```

### Request Body

```json
{
  "itemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newCustomerId": "84828e82-81ca-437d-b2f0-b9877ef044c6",
  "newAgreedPriceInLocal": 12500.00
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `itemId` | `Guid` | Yes | Must be an existing `OrderItem.Id` |
| `newCustomerId` | `Guid` | Yes | Must be an existing `Customer.Id` |
| `newAgreedPriceInLocal` | `decimal` | Yes | Must be ≥ 0. If 0, frontend shows confirmation before sending. |

### Response — Success

```json
{ "success": true }
```

### Response — Failure

```json
{ "success": false, "error": "Mensaje de error legible." }
```

### Error Cases

| Condition | `error` value |
|---|---|
| Item not found | `"Ítem no encontrado."` |
| Order not found | `"Orden no encontrada."` |
| Order in Pending or Voided status | `"No se puede reasignar en estado {Status}."` |
| Neither customer nor price changed | Returns `{ "success": true }` (no-op, not an error) |

### C# Request Record (in `Items.cshtml.cs`)

```csharp
public record ReasignarRequest(Guid ItemId, Guid NewCustomerId, decimal NewAgreedPriceInLocal);
```

### Handler Signature

```csharp
public async Task<JsonResult> OnPostReasignarAsync([FromBody] ReasignarRequest request)
{
    var (success, error) = await orderService.ReasignarItemAsync(
        request.ItemId,
        request.NewCustomerId,
        request.NewAgreedPriceInLocal);
    return new JsonResult(new { success, error });
}
```

---

## Service Method: `IOrderService.ReasignarItemAsync`

### Signature

```csharp
Task<(bool Success, string? Error)> ReasignarItemAsync(
    Guid itemId,
    Guid newCustomerId,
    decimal newAgreedPriceInLocal);
```

### Business Logic Summary

| Condition | Transactions Created | Item/Order Update |
|---|---|---|
| Customer changed (any price) | `Void(oldCustomer, oldPrice)` + `Charge(newCustomer, newPrice)` | Update `CustomerId`, `AgreedPriceInLocal` |
| Same customer, price increased | `Charge(customer, diff)` | Update `AgreedPriceInLocal` |
| Same customer, price decreased | `Payment(customer, diff)` | Update `AgreedPriceInLocal` |
| No change (same customer + same price) | None (no-op) | No change |

After any price change: recalculate `Order.TotalAgreedPriceInLocal` and `Order.EstimatedProfitInLocal`.
Supplier-side totals (`TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`) are **never modified**.

### Transaction Description Templates

| Case | Description |
|---|---|
| Customer change (Void + Charge) | `"Reasignación – {order.NameOfOrder} – {ProductDescription[..100]}"` |
| Price increase (Charge) | `"Ajuste de precio – {ProductDescription[..100]}"` |
| Price decrease (Payment) | `"Descuento por ajuste de precio – {ProductDescription[..100]}"` |

`ProductDescription` is truncated to 100 characters maximum.

---

## JavaScript Interaction (Frontend)

### Flow

1. User clicks "Reasignar" button on a row → open modal with prefilled `customerId` + `agreedPriceInLocal`
2. User selects new customer (or keeps same) + sets new price
3. If `newAgreedPriceInLocal === 0` → show inline warning and require explicit confirmation before submitting
4. On submit: `POST /Orders/Items?handler=Reasignar` with `RequestVerificationToken` header
5. On `success: true` → close modal + reload items table (reuse existing `loadItems()` call)
6. On `success: false` → show `error` message inside the modal (do not close)

### Required Request Header

```
RequestVerificationToken: <antiforgery token>
```

(Standard Razor Pages antiforgery — same pattern as `OnPostToggleReceivedAsync`.)
