# Data Model: Fase 1 — Sales & Order Management System

**Date**: 2026-06-03

---

## New Entity: OrderStatusHistory

Tracks every order status transition as an immutable audit record.

```csharp
public class OrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }           // FK → Order
    public string FromStatus { get; set; }      // OrderStatus.Key (e.g. "Pending")
    public string ToStatus { get; set; }        // OrderStatus.Key (e.g. "Active")
    public DateTime TransitionDate { get; set; } // User-provided date (Costa Rica local)
    public string? Notes { get; set; }          // Optional for all transitions
    public string? Justification { get; set; }  // Required for Voided, nullable otherwise
    public DateTime CreatedAt { get; set; }     // Server timestamp (UTC)
}
```

**EF Configuration**:
- Table: `dbo.OrderStatusHistory`
- PK: `Id` (ValueGeneratedOnAdd)
- FK: `OrderId` → `Orders.Id`
- `FromStatus` / `ToStatus`: `HasMaxLength(20)`, required
- `Notes`: `HasMaxLength(500)`, nullable
- `Justification`: `HasMaxLength(1000)`, nullable
- `TransitionDate`: required
- `CreatedAt`: required

**No GQF** — history records are accessed via their parent Order, which is already filtered.

---

## Modified Entity: MariCamiStoreContext

Add to `OnModelCreating`:

```csharp
// New DbSet
public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

// Global Query Filters — applied on entities with OrganizationId
modelBuilder.Entity<Order>()
    .HasQueryFilter(o => o.OrganizationId == _currentOrganizationService.OrganizationId);

modelBuilder.Entity<OrderItem>()
    .HasQueryFilter(oi => oi.Order.OrganizationId == _currentOrganizationService.OrganizationId);

modelBuilder.Entity<Transaction>()
    .HasQueryFilter(t => t.OrganizationId == _currentOrganizationService.OrganizationId);
```

**No GQF on**: `Customer`, `Currency`, `ProductType`, `Supplier`, `Configuration`, `Organization` (global catalogs).

**Constructor change**: Add `ICurrentOrganizationService` parameter.

---

## Existing Entities — No Schema Changes Required

All existing entities have the correct fields per spec. The following are confirmed as-is:

| Entity | Status | Notes |
|--------|--------|-------|
| `Order` | ✅ Complete | All financial fields present; `Status` stored as string key |
| `OrderItem` | ✅ Complete | All pricing fields present |
| `Transaction` | ✅ Complete | Has `OrganizationId`, `SourceId` (nullable), `TransactionAmount` |
| `ProductType` | ✅ Complete | Has `EstimateShipping` and `ServiceFeeInLocal` |
| `Customer` | ✅ Complete | Global entity, no `OrganizationId` |
| `Currency` | ✅ Complete | |
| `Supplier` | ✅ Complete | |
| `Configuration` | ✅ Complete | |
| `Organization` | ✅ Complete | |

---

## Migration Required

One new EF Core migration:
1. Create `dbo.OrderStatusHistory` table
2. Global Query Filters are applied in code; no schema change for GQF

**Migration name**: `AddOrderStatusHistory`

---

## Entity Relationships Summary

```
Organization (1) ──── (*) Order
Order        (1) ──── (*) OrderItem
Order        (1) ──── (*) OrderStatusHistory
Order        (1) ──── (*) Transaction     [via OrganizationId scope]
OrderItem    (1) ──── (*) Transaction     [via SourceId, nullable]
Customer     (1) ──── (*) OrderItem       [CustomerId on item]
Customer     (1) ──── (*) Transaction     [CustomerId on transaction]
ProductType  (1) ──── (*) OrderItem       [ProductTypeId on item]
Supplier     (1) ──── (*) Order
Currency     (1) ──── (*) Order
Currency     (1) ──── (*) Transaction
Currency     (1) ──── (*) ProductType
```
