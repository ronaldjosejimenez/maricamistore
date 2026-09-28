# Data Model: 011-mejoras-saldos-ordenes-talla

## OrderItem (modified)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Size` | `string` | NOT NULL, max 20 (`OrderItem.SizeMaxLength`), DB default `'N/A'` (`OrderItem.DefaultSize`) | New. Free text (e.g. "M", "38", "10-12 años"). Trimmed on save; blank → `N/A`. |

- Migration `AddOrderItemSize`: `ALTER TABLE OrderItems ADD Size nvarchar(20) NOT NULL DEFAULT N'N/A'` (schema per `MariCamiStoreContext.DEFAULT_SCHEMA`). Existing rows → `'N/A'`.
- Validation: after trim, `Size.Length <= 20`; otherwise reject with "La talla no puede superar 20 caracteres.".
- Future (out of scope): relation to a size catalog per `ProductType`.

## DTOs (modified)

- `OrderItemDto` (Pages/Orders/Items.cshtml.cs): add `string? Size` (null/blank treated as `N/A`).
- `OrderItemWithCustomerDto` (Services): add `string Size` (used only to prefill the edit modal).

## OrderStatus (unchanged)

Fixed catalog: `Pending` (Pendiente), `Active` (Activa), `Delivering` (Entregando), `Delivered` (Entregada), `Completed` (Completada), `Voided` (Anulada).

### Status selection (value object, not persisted)

- Serialized as comma-separated keys in `statuses` query parameter.
- Parse rules: absent → default `[Pending, Active]`; present & empty → `[]`; present with values → valid keys only (ordered as `OrderStatus.List()`), fallback to default if none valid.

## Saldo row (unchanged)

`{ customerName, balance, isGeneric }` from `Payments?handler=Saldos`. Total = Σ `balance` over visible rows (not persisted).
