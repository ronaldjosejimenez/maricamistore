# Data Model: Refactor Orden e Ítems

**Feature**: specs/003-refactor-orden-items/spec.md
**Date**: 2026-06-04

---

## DB Migration Changes

### Migration: `RefactorOrderItems`

**Table `dbo.Orders`**:
- **REMOVE** column `RequestedAt` (DateTime?, nullable)
- **ADD** column `TotalAgreedPriceInLocal` (decimal(18,2), NOT NULL default 0)

**Table `dbo.OrderItems`**:
- **RENAME** column `ListPriceTax` → `ListPriceTaxWithTax` (decimal(18,2), NOT NULL)

> **Note**: `ProductLink`, `ProductSourceCode`, `ProductImage` ya deben existir en la DB (agregados en `20260603023734_new_field-other.cs`). No se agregan en esta migración.

---

## Cambios en C# Models

### `MariCamiStore/Model/Order.cs`

| Campo | Acción | Tipo | Notas |
|-------|--------|------|-------|
| `RequestedAt` | **REMOVE** | DateTime? | Ya no requerido |
| `TotalAgreedPriceInLocal` | **ADD** | decimal | = Σ AgreedPriceInLocal de ítems; persiste en DB |

**Campos existentes sin cambio** (referencia):
- `Id`, `NameOfOrder`, `OrganizationId`, `SupplierId`, `CurrencyId`
- `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`
- `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`
- `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal`
- `Status`, `CreatedAt`, `UpdatedAt`

### `MariCamiStore/Model/OrderItem.cs`

| Campo | Acción | Tipo | Notas |
|-------|--------|------|-------|
| `ListPriceTax` | **RENAME** → `ListPriceTaxWithTax` | decimal(18,2) | Rename en modelo y DB |
| `ProductLink` | ya existe | string?, max 500 | Sin cambio |
| `ProductSourceCode` | ya existe | string?, max 50 | Sin cambio |
| `ProductImage` | ya existe | byte[]? | Sin cambio |

**Campos existentes sin cambio** (referencia):
- `Id`, `OrderId`, `CustomerId`, `ProductTypeId`
- `ProductDescription`, `ListPrice`, `RealPrice`
- `EstimateShipping`, `ServiceFeeInLocal`, `AgreedPriceInLocal`
- `CreatedAt`, `UpdatedAt`

**Campo computado (NO persiste)**:
- `Total` (decimal) — `(ListPriceTaxWithTax * Order.ExchangeRate) + ServiceFeeInLocal` — computed property en C# para uso en serialización de la grilla

---

## Cambios en Entity Type Configurations

### `OrderEntityTypeConfiguration`

- Remover configuración de `RequestedAt`
- Agregar: `builder.Property(o => o.TotalAgreedPriceInLocal).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m)`

### `OrderItemEntityTypeConfiguration`

- Cambiar: `builder.Property(oi => oi.ListPriceTax)` → `builder.Property(oi => oi.ListPriceTaxWithTax)`
- Mapeo de columna: `.HasColumnName("ListPriceTaxWithTax")` (post-rename migration)

---

## Cambios en DTOs

### `OrderTotalsDto` (en `IOrderService.cs` o `OrderService.cs`)

```
Antes:
  OrderId, ShippingAmountToCR, TotalWithoutTaxes, TaxesAmount,
  TotalToPayToSupplier, TotalOfTheOrder, EstimatedProfitInLocal

Después:
  + TotalAgreedPriceInLocal (decimal)  ← NUEVO
```

---

## Cambios en Services

### `IOrderService` + `OrderService`

| Método | Acción | Notas |
|--------|--------|-------|
| `UpdateOrderTotalsAsync(OrderTotalsDto)` | **UPDATE** | Agregar `TotalAgreedPriceInLocal` al update EF |
| Todos los métodos que referencian `ListPriceTax` | **UPDATE** | Rename al nuevo nombre |
| Todos los métodos que referencian `RequestedAt` | **REMOVE** | Eliminar referencias |

---

## Nuevos Handlers en `Items.cshtml.cs`

| Handler | Método | Propósito |
|---------|--------|-----------|
| `OnGetProductTypesByCurrencyAsync(Guid currencyId)` | GET | Retorna ProductTypes filtrados por moneda |
| `OnGetItemImageAsync(Guid itemId)` | GET | Retorna imagen binaria del ítem como FileContentResult |
| `OnPostUpdateOrderAsync([FromBody] OrderHeaderDto)` | POST | Actualiza los 4 campos editables de la orden in-place |

### `OrderHeaderDto` (nuevo DTO)

```csharp
public record OrderHeaderDto(
    Guid OrderId,
    decimal ExchangeRate,
    decimal TaxPercentage,
    decimal ShippingAmountIntern,
    decimal DiscountAmount);
```

---

## Relaciones y Estado del Modelo

```
Organization (1) ──── (M) Order
Order
  ├─ CurrencyId (FK → Currency) ← default desde Configuration.OrderCurrencyIdDefault
  ├─ SupplierId (FK → Supplier)
  ├─ (1) ──── (M) OrderItem
  │     ├─ CustomerId (FK → Customer)
  │     ├─ ProductTypeId (FK → ProductType, filtrado por CurrencyId)
  │     └─ ProductImage (byte[]?, max 2 MB)
  └─ (1) ──── (M) OrderStatusHistory

Configuration
  ├─ One per Organization
  ├─ OrderCurrencyIdDefault (Guid) → default para Order.CurrencyId
  └─ ExchangeRate, TaxPercentage → defaults para Order creation
```

---

## Reglas de Cálculo (referencia para implementación)

### A nivel de ítem (en JS, tiempo real):
1. `ListPriceTaxWithTax = ListPrice + (ListPrice * TaxPercentage)`
2. `Total = (ListPriceTaxWithTax * ExchangeRate) + ServiceFeeInLocal` ← NO persiste
3. `AgreedPriceInLocal = Total` (default, a menos que editado manualmente)
4. `RealPrice = ListPrice` (default, a menos que editado manualmente)

### A nivel de orden — Order Calculation Suite (en JS + persistido vía `UpdateTotals`):
1. `TotalAgreedPriceInLocal` = Σ `AgreedPriceInLocal` ← NUEVO
2. `ShippingAmountToCR` = Σ `EstimateShipping`
3. `TotalWithoutTaxes` = Σ `RealPrice`
4. `TaxesAmount` = `(TotalWithoutTaxes - DiscountAmount) * TaxPercentage`
5. `TotalToPayToSupplier` = `ShippingAmountIntern + TotalWithoutTaxes + TaxesAmount - DiscountAmount`
6. `TotalOfTheOrder` = `TotalToPayToSupplier + ShippingAmountToCR`
7. `EstimatedProfitInLocal` = `TotalAgreedPriceInLocal - (TotalOfTheOrder * ExchangeRate)`

### Trigger A (al guardar Orden — cambio en TaxPercentage o ExchangeRate):
- Recalcular para TODOS los ítems: `ListPriceTaxWithTax` y `ServiceFeeInLocal` (reload desde ProductType)
- Recalcular `AgreedPriceInLocal` para TODOS los ítems (override de flag manual)
- Ejecutar Order Calculation Suite

### Trigger B (al insertar/editar/eliminar ítem):
- Ejecutar Order Calculation Suite inmediatamente
