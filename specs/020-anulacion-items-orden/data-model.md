# Data Model — 020

## OrderItem (MODIFY)
| Campo | Tipo | Notas |
|---|---|---|
| `Status` | `string` (nvarchar(20), NOT NULL, default `'Active'`) | Valores: `OrderItemStatus.Active`, `OrderItemStatus.Voided` |

Transiciones: `Active → Voided` (solo vía `VoidItemsAsync`, con la orden en `Active`). No hay reversa.

Reglas para un ítem `Voided`: no se edita, no se reasigna, no cambia `IsReceived` y no entra en totales, conteos ni en la anulación de la orden completa.

## OrderItemStatus (MODIFY)
`List() = [Active, Voided]`. Se eliminan `Pending`, `Checked` y `Delivered`.

## CxPEntryType (MODIFY)
- `AnulacionItems = "AnulacionItems"`: monto positivo, **resta**.
- `static decimal SignedAmount(string type, decimal amount)`: devuelve `-amount` si `type == AnulacionItems` y `amount` en otro caso.

## CxPEntry (sin cambio de esquema)
Fila creada por la anulación:
| Campo | Valor |
|---|---|
| `PeriodControlId` | período abierto |
| `CurrencyId` | `order.CurrencyId` |
| `Amount` | Σ montos a devolver (≥ 0) |
| `Type` | `AnulacionItems` |
| `OrderId` | `order.Id` |
| `Reference` | `Anulación ítems: {NameOfOrder} – {justificación}` (truncada) |

## Transaction (sin cambio de esquema)
Una fila por ítem anulado: `Void`, `Source=OrderItem`, `SourceId=item.Id`, `CustomerId=item.CustomerId`, `Amount=item.AgreedPriceInLocal`, moneda local, fecha CR, `Applied`.

## Order (sin cambio de esquema)
Los 7 totales se recalculan con `ComputeOrderTotals` sobre los ítems `Active`. `DiscountAmount` y `ShippingAmountIntern` no cambian.

## OrderTotals (NEW, record interno)
`TotalAgreedPriceInLocal, ShippingAmountToCR, TotalWithoutTaxes, TaxesAmount, TotalToPayToSupplier, TotalOfTheOrder, EstimatedProfitInLocal`. Lo devuelve `ComputeOrderTotals`, que es puro y usa las mismas fórmulas y redondeo (`AwayFromZero`) que hoy usa `ApplyOrderCalculationSuite`.

## Migración
`<ts>_AddOrderItemStatus`: `AddColumn Status nvarchar(20) NOT NULL DEFAULT 'Active'` en `dbo.OrderItems`. No requiere backfill.
