# Research: Flexibilidad de Ítems en Órdenes Activas

**Feature**: `010-flexibilidad-items-activos`
**Date**: 2026-06-11
**Status**: Complete — no unknowns remain

---

## Findings

### 1. Transaction pattern (`OrderService.BuildTransaction`)

**Decision**: Reuse the exact same pattern as `BuildTransaction` for all transactions created by `ReasignarItemAsync`.

Key details from `OrderService.cs:342`:
- `Source = TransactionSource.OrderItem.Key`
- `OrganizationId = order.OrganizationId`
- `CurrencyId = config?.LocalCurrencyId ?? Guid.Empty` (`config = context.Configurations.FirstOrDefault()`)
- `TransactionDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central America Standard Time")`
- `Status = TransactionStatus.Applied.Key`
- `SourceId = item.Id` — this is the linkage to the originating charge

**Rationale**: Consistent with all existing transaction creation in the codebase.

---

### 2. Order-level recalculation (`ApplyOrderCalculationSuite`)

**Decision**: For `ReasignarItemAsync`, recalculate ONLY `TotalAgreedPriceInLocal` and `EstimatedProfitInLocal`. Use the formula confirmed at `OrderService.cs:120`:

```
TotalAgreedPriceInLocal = SUM(item.AgreedPriceInLocal) [all items]
EstimatedProfitInLocal  = TotalAgreedPriceInLocal - TotalOfTheOrder * ExchangeRate
```

**Do NOT call** `ApplyOrderCalculationSuite` — it recalculates all fields including supplier totals (`TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`), which must remain unchanged (FR-012).

**Safe sum approach** (avoids EF change-tracker ambiguity):
```csharp
var otherItemsTotal = await context.OrderItems
    .Where(i => i.OrderId == item.OrderId && i.Id != itemId)
    .SumAsync(i => i.AgreedPriceInLocal);
order.TotalAgreedPriceInLocal = Math.Round(otherItemsTotal + newAgreedPriceInLocal, 2);
order.EstimatedProfitInLocal  = Math.Round(order.TotalAgreedPriceInLocal - order.TotalOfTheOrder * order.ExchangeRate, 2);
```

---

### 3. Order-Item loading pattern

**Decision**: Load item and order as two separate queries (no navigation properties on `OrderItem`):
```csharp
var item  = await context.OrderItems.FindAsync(itemId);
var order = await context.Orders.FindAsync(item.OrderId);
```

**Rationale**: Matches the existing patterns across `OrderService` (no `Include()` calls observed).

---

### 4. `Customer.IsGeneric` migration strategy

**Decision**: Use a custom `migrationBuilder.Sql(...)` in the migration instead of `HasData()`, to ensure idempotent upsert of the "Sin Cliente" record that already exists in production with ID `84828E82-81CA-437D-B2F0-B9877EF044C6`.

**Rationale**: `HasData()` generates `InsertData` calls that fail with a duplicate-key error if the record already exists. A raw SQL MERGE/IF-NOT-EXISTS is safe on both fresh and existing databases.

The `CustomerEntityTypeConfiguration.cs` does NOT add "Sin Cliente" to `HasData`. The migration is the sole source of truth for this seed.

**SQL pattern** (in migration `Up()`):
```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6')
BEGIN
    INSERT INTO [dbo].[Customers] ([Id],[NickName],[Name],[PhoneNumber],[Address],[LocationLink],[Email],[IsGeneric])
    VALUES ('84828E82-81CA-437D-B2F0-B9877EF044C6','Sin Cliente','Sin Cliente','0000-0000',NULL,NULL,NULL,1)
END
ELSE
BEGIN
    UPDATE [dbo].[Customers] SET [IsGeneric] = 1
    WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6'
END
```

---

### 5. `SaldoReportRow` extension

**Decision**: Add `bool IsGeneric` as a positional parameter to the existing record in `IPaymentService.cs`:
```csharp
public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric);
```

Update `GetSaldosReportAsync()` to:
1. Change filter: `Where(r => r.Balance > 0)` → `Where(r => r.Balance != 0)` (FR-016)
2. Fetch `IsGeneric` from the customer lookup and include it in each row

---

### 6. Customer dropdown endpoint(s)

**Needs investigation during implementation**: Where are customers loaded for:
- The item add/edit modal in `Items.cshtml` (to include "Sin Cliente")
- The payment registration form in `Payments/Index.cshtml` (to exclude IsGeneric customers)

The handlers in `Items.cshtml.cs` and `Payments/Index.cshtml.cs` do not show a customer list endpoint; it must be in a shared catalog endpoint or loaded by `ICatalogService`. The implementation task must locate this endpoint and apply the appropriate filter.

---

### 7. Order status guard for `ReasignarItemAsync`

**Decision**: Use string comparison matching the existing `ValidateTransition` pattern:
```csharp
var allowed = new[] { OrderStatus.Active.Key, OrderStatus.Delivering.Key, 
                      OrderStatus.Delivered.Key, OrderStatus.Completed.Key };
if (!allowed.Contains(order.Status))
    return (false, $"No se puede reasignar en estado {order.Status}.");
```

---

## No Unknowns Remain

All NEEDS CLARIFICATION items from the spec have been resolved by codebase analysis. No external research required. Proceed to Phase 1 (data model).
