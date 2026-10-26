# Data Model: Defaults por proveedor, calculadora y fórmulas de ítems

## Supplier (`dbo.Suppliers`) — MODIFY

| Propiedad | Tipo C# | Columna | Regla |
|-----------|---------|---------|-------|
| Id | Guid | uniqueidentifier PK | sin cambio |
| Name | string | nvarchar(100) | sin cambio |
| **TaxPercentageDefault** | decimal | decimal(18,2) NOT NULL DEFAULT 0 | ≥ 0 |
| **CurrencyIdDefault** | Guid? | uniqueidentifier NULL (sin FK) | obligatorio al crear o editar (validación en servicio) |
| **ExchangeRateDefault** | decimal | decimal(18,2) NOT NULL DEFAULT 0 | > 0 |
| **ShippingCostDefault** | decimal | decimal(18,2) NOT NULL DEFAULT 0 | ≥ 0; en la moneda `CurrencyIdDefault` |
| **IsActiveForCalculator** | bool | bit NOT NULL DEFAULT 0 | — |

Validación (FR-002a) en `CatalogService.CreateSupplierAsync` y `UpdateSupplierAsync`. Si falla, lanza `ArgumentException` con un mensaje en español, igual que `UpsertConfigurationAsync`. El handler de la página devuelve el mensaje y el grid lo muestra.

Mensajes:
- "La moneda por defecto es obligatoria."
- "El tipo de cambio por defecto debe ser mayor a cero."
- "El impuesto por defecto no puede ser negativo."
- "El costo de envío por defecto no puede ser negativo."

Seeds (`HasData`): los 6 proveedores sembrados agregan los valores nuevos con sus defaults (0 / null / false). La migración llena después los 3 defaults desde la Configuración.

## OrderItem (`dbo.OrderItems`) — MODIFY

| Propiedad | Tipo C# | Columna | Regla |
|-----------|---------|---------|-------|
| **RealPriceInLocal** | decimal | decimal(18,2) NOT NULL DEFAULT 0 | = Round(RealPrice × Order.ExchangeRate, 2, AwayFromZero); lo calcula siempre el servidor |

Cadena de cálculo del ítem (moneda de la orden → local):

```text
ListPriceTaxWithTax = Round(ListPrice × (1 + Tax%/100), 2)      editable
RealPrice           = ListPriceTaxWithTax + EstimateShipping    editable (manual flag)
RealPriceInLocal    = Round(RealPrice × ExchangeRate, 2)        read-only, persisted
Total (calculado)   = RealPriceInLocal + ServiceFeeInLocal      read-only, not persisted
AgreedPriceInLocal  = Total por defecto                          editable (manual flag)
```

## Order (`dbo.Orders`) — sin cambio de esquema

Cambio de fórmula en `ApplyOrderCalculationSuite` y `recalcOrderHeader`:

```text
TotalWithoutTaxes      = Σ ListPrice                 (antes Σ RealPrice)
TaxesAmount            = (TotalWithoutTaxes − Discount) × Tax%      sin cambio
TotalToPayToSupplier   = ShippingIntern + TotalWithoutTaxes + Taxes − Discount   sin cambio
ShippingAmountToCR     = Σ EstimateShipping          sin cambio
TotalOfTheOrder        = TotalToPayToSupplier + ShippingAmountToCR   sin cambio
EstimatedProfitInLocal = TotalAgreed − TotalOfTheOrder × ExchangeRate  sin cambio
```

## Configuration — sin cambio

Es la fuente de: `TaxPercentage` → `TaxPercentageDefault`, `OrderCurrencyIdDefault` → `CurrencyIdDefault` y `ExchangeRate` → `ExchangeRateDefault`.

## Calculadora — sin persistencia

```text
PriceWithTax = ListPrice + ListPrice × Vat/100 + Shipping
Shipping     = ShippingCostDefault × (FastShipping ? 2 : 1)   (editable)
PriceLocal   = PriceWithTax × ExchangeRate
Total        = ceil((PriceLocal + ServiceCost) / 100) × 100
```
