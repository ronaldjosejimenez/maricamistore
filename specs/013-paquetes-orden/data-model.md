# Data Model: 013-paquetes-orden

## OrderPackage (new) — table `OrderPackages`

| Field | Type | Constraints |
|-------|------|-------------|
| Id | Guid | PK |
| OrderId | Guid | FK → Orders (Restrict / NO ACTION, avoids multiple cascade paths), required, indexed |
| DeliveryDate | date | required |
| Amount | decimal(18,2) | required, > 0 |
| CurrencyId | Guid | FK → Currencies (Restrict), required; always = Order.CurrencyId |
| Description | nvarchar(500) | optional (null when blank) |
| CreatedAt | datetime2 | required (UTC) |

- Immutable (no update path). Organization scoping via query filter on `Order.OrganizationId`.
- Allowed operations only when Order.Status ∈ {Active, Delivering}.

## CxPEntry (modified) — table `CxPEntries`

| Field | Change |
|-------|--------|
| OrderPackageId | **new** Guid?, FK → OrderPackages (**SetNull**), indexed |
| Type | new values `AutoPaquete`, `ReversoPaquete` |
| Amount | may be negative only for `ReversoPaquete` |

- AutoPaquete: Amount = package amount, OrderPackageId = package, OrderId = order, Reference = "{Order} - {Desc}" | "{Order}".
- ReversoPaquete: Amount = −package amount, OrderPackageId = null, OrderId = order, Reference = "Reverso: " + same.

## Order (modified) — table `Orders`

- **Removed**: `ActualShippingAmountToCR` (column dropped).
- `ShippingAmountToCR` (estimated, sum of item EstimateShipping) is the basis for pending shipping.

## Derived values

- Order pending shipping = max(0, ShippingAmountToCR − Σ packages.Amount).
- Shipping CR Pendientes (period) = Σ over orders in {Active, Delivering} of pending shipping, × period.ExchangeRate unless order currency = local currency; 0 when ExchangeRate = 0.

## Migration `AddOrderPackages`

Create `OrderPackages` (+ FKs, index) · add `CxPEntries.OrderPackageId` (+ FK SetNull, index) · drop `Orders.ActualShippingAmountToCR`. Down reverses (re-adds column with default 0).
