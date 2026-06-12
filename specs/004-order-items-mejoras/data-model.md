# Data Model: Order Items Mejoras

**Feature**: `specs/004-order-items-mejoras`
**Date**: 2026-06-06

---

## Model Changes

### OrderItem — New Field

```
OrderItem
├── Id                   : Guid         (existing)
├── OrderId              : Guid         (existing)
├── CustomerId           : Guid         (existing)
├── ProductDescription   : string       (existing)
├── ProductLink          : string       (existing)
├── ProductSourceCode    : string       (existing)
├── ProductImage         : byte[]?      (existing)
├── ProductTypeId        : Guid         (existing)
├── ListPrice            : decimal      (existing)
├── ListPriceTaxWithTax  : decimal      (existing)
├── RealPrice            : decimal      (existing)
├── EstimateShipping     : decimal      (existing)
├── ServiceFeeInLocal    : decimal      (existing)
├── AgreedPriceInLocal   : decimal      (existing)
├── CreatedAt            : DateTime     (existing)
├── UpdatedAt            : DateTime     (existing)
├── Order                : Order        (existing nav)
└── IsReceived           : bool         [NEW] default: false
```

**Migration required**: `ADD COLUMN IsReceived BIT NOT NULL DEFAULT 0` on `OrderItems`.

---

## EF Configuration Change

In `OrderItemEntityTypeConfiguration.cs`:

```csharp
builder.Property(oi => oi.IsReceived)
    .IsRequired()
    .HasDefaultValue(false);
```

---

## No Other Schema Changes

All other entities (`Order`, `Customer`, `ProductType`) are unchanged by this spec.

---

## Derived / Response-Only Fields

These fields appear in API responses but are NOT persisted:

| Field | Source | Used in |
|-------|--------|---------|
| `CustomerDisplayName` | `Customer.NickName ?? Customer.Name ?? Customer.Id.ToString()` | `OnGetLoadAsync` response, used for sort/group in frontend |

The `CustomerDisplayName` is computed server-side by joining `OrderItems` with `Customers` in `OnGetLoadAsync`.

---

## State Transitions for IsReceived

```
IsReceived: false  →  [checkbox checked, order is Delivering or Delivered]  →  IsReceived: true
IsReceived: true   →  [checkbox unchecked, order is Delivering or Delivered] →  IsReceived: false
IsReceived: any    →  [order NOT in Delivering/Delivered]  →  checkbox not shown (read-only)
```
