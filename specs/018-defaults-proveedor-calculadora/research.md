# Research: Defaults por proveedor, calculadora y fórmulas de ítems

No quedan `NEEDS CLARIFICATION` en el Technical Context. Estas son las decisiones de diseño tomadas al revisar el código.

## R1 — Fuente de los defaults de Configuración

- **Decision**: Para la precarga en pantalla se usa `ICatalogService.GetConfigurationAsync()` (`FirstOrDefaultAsync` con el filtro de organización), igual que `Orders/Index.OnGetConfigurationAsync`. Para la migración se usa `SELECT TOP 1 ... FROM dbo.Configurations ORDER BY Id`.
- **Rationale**: El usuario indicó "top 1". Los proveedores son globales y la Configuración es por organización; en producción hay una sola fila. `ORDER BY Id` hace que la migración sea determinística.
- **Alternatives considered**: Defaults por organización en una tabla puente proveedor × organización. Se descartó por YAGNI (hoy hay una organización).

## R2 — Columna de moneda default nullable

- **Decision**: `Suppliers.CurrencyIdDefault` es `uniqueidentifier NULL` en la base. La obligatoriedad se valida en `CatalogService` al crear y editar (FR-002a).
- **Rationale**: Si la tabla `Configurations` está vacía, la migración no tiene de dónde sacar la moneda; una columna NOT NULL fallaría o exigiría un GUID inventado. `Configuration.OrderCurrencyIdDefault` tampoco tiene FK; se sigue el mismo patrón (sin FK).
- **Alternatives considered**: NOT NULL con default `Guid.Empty`. Se descartó porque dejaría un valor inválido silencioso.

## R3 — Precarga del proveedor nuevo en jsGrid

- **Decision**: La página de Proveedores carga la Configuración (nuevo handler `OnGetConfigurationAsync`) antes de iniciar el grid. Los campos nuevos usan `insertTemplate` para precargar el valor en la fila de inserción. El usuario puede cambiarlo antes de guardar.
- **Rationale**: Cumple FR-002 (valores visibles y editables antes de guardar) con el mismo patrón jsGrid que ya usan `product-types/index.js` y la carga de monedas.
- **Alternatives considered**: Llenar los valores en el servidor al crear. Se descartó porque el usuario no los vería ni podría editarlos antes de guardar.

## R4 — Calculadora: JS en archivo propio y datos del proveedor

- **Decision**: Se mueve el script inline de `Pages/Calculator/Index.cshtml` a `wwwroot/js/pages/calculator/index.js`. El PageModel recibe `ICatalogService` y expone `OnGetSuppliersAsync`, que devuelve solo los proveedores con `IsActiveForCalculator = true` y los campos default necesarios.
- **Rationale**: Es la convención de las demás páginas (`wwwroot/js/pages/<page>/index.js`). Se elimina todo el código de "cuenta" (`listOfAccount`, `RefreshGrid`, `getUrlParameter`).
- **Redondeo**: `Math.ceil(total / 100) * 100` aplicado solo al Total. Precio con impuesto y Precio en colones se muestran con 2 decimales y no se redondean a la centena.
- **Duplicar envío**: El JS guarda el `shippingCostDefault` del proveedor seleccionado. El checkbox calcula `base × (checked ? 2 : 1)`, y cambiar de proveedor o el checkbox sobrescribe el campo (casos borde del spec).

## R5 — Defaults del proveedor en Orden nueva

- **Decision**: `orders/index.js` ya carga la lista completa de proveedores (`/Suppliers/Index?handler=Load`, que devuelve la entidad completa). Se guarda en `supplierItems` y, en el handler `change` de `#order-supplier`, solo cuando `#order-id` está vacío (creación), se asignan tipo de cambio, impuesto y moneda. No hace falta un endpoint nuevo.
- **Rationale**: Se reutiliza la llamada existente y se respeta FR-014 (no cambia nada al editar). Si el proveedor no tiene moneda default, la moneda no se cambia.
- **Nota**: El handler actual sale temprano si `userEditedName` es verdadero; la carga de defaults debe hacerse antes de ese `return`, separada de la lógica del nombre.

## R6 — Fórmulas del ítem (cliente y servidor)

- **Decision**:
  - Cliente (`items.js`): `calcModalFields` / `calcModalFromLptpwt` se reemplazan por una sola función `recalcModal()` con esta cadena: Lista → Precio + Imp (salvo que se edite directamente) → Precio Real (si `!userEditedRealPrice`) → Precio Real en Local → Total → Precio Acordado (si `!userEditedAgreed`).
  - Servidor: `RealPriceInLocal` se recalcula en el servidor al crear o editar un ítem (`RealPrice × order.ExchangeRate`, 2 decimales), en lugar de confiar en el valor del cliente.
- **Detección de edición manual al abrir un ítem**: `userEditedRealPrice = |RealPrice − (ListPriceTaxWithTax + EstimateShipping)| > 0.01` y `userEditedAgreed = |AgreedPrice − (RealPriceInLocal + ServiceFee)| > 0.01`.
- **Consecuencia aceptada**: En los ítems existentes, Precio Real tiene la semántica vieja (≈ Precio Lista), así que se detectan como "editado a mano" y no se recalculan solos. Como el encabezado ya no usa Precio Real (R7), esto no afecta los totales. Para corregir un ítem viejo, el usuario edita Precio Real.

## R7 — Totales del encabezado

- **Decision**: `TotalWithoutTaxes = Σ ListPrice` en los dos lugares donde se calcula: `OrderService.ApplyOrderCalculationSuite` (servidor) y `recalcOrderHeader` en `items.js` (cliente). El resto de las fórmulas no cambia.
- **Rationale**: FR-021. El cliente recalcula y persiste los totales al abrir la orden (`renderItemsTable → refreshTotals`); si no se cambian los dos lugares, divergen.
- **Solo Pendientes (FR-022)**: `refreshTotals()` recalcula y llama `persistTotals` solo si `isPending`. En otro estado, el encabezado muestra los valores que renderiza el servidor en `Items.cshtml`. Como defensa, `OrderService.UpdateOrderTotalsAsync` ignora la llamada si `order.Status != Pending`.
- **Reasignar (FR-023)**: el servidor no cambia. `OnPostReasignarAsync` devuelve además `totalAgreedPriceInLocal` y `estimatedProfitInLocal` ya actualizados, y el JS refresca solo `#order-total-agreed` y `#h-profit`, sin llamar `refreshTotals`.

## R8 — Recálculo al cambiar Impuesto o TC (Trigger A)

- **Decision**: En `OrderService.RecalcItemsOnOrderSaveAsync`, por ítem: guardar `oldAuto = ListPriceTaxWithTax + EstimateShipping` antes de recalcular; recalcular `ListPriceTaxWithTax`; si `|RealPrice − oldAuto| ≤ 0.01`, entonces `RealPrice = ListPriceTaxWithTax + EstimateShipping`; luego `RealPriceInLocal = RealPrice × newExchangeRate`; y, si cambió el TC, `AgreedPriceInLocal = RealPriceInLocal + ServiceFeeInLocal`.
- **Rationale**: FR-018a. Mantiene el comportamiento actual (recálculo forzado de Acordado al cambiar el TC) con la nueva fórmula.

## R9 — Migraciones

- **Decision**: Una sola migración `SupplierDefaultsAndRealPriceInLocal`, generada con `dotnet ef migrations add`, que agrega:
  - `Suppliers`: `TaxPercentageDefault decimal(18,2) NOT NULL DEFAULT 0`, `CurrencyIdDefault uniqueidentifier NULL`, `ExchangeRateDefault decimal(18,2) NOT NULL DEFAULT 0`, `ShippingCostDefault decimal(18,2) NOT NULL DEFAULT 0`, `IsActiveForCalculator bit NOT NULL DEFAULT 0`.
  - `OrderItems`: `RealPriceInLocal decimal(18,2) NOT NULL DEFAULT 0`.
  - SQL al final de `Up` (después de cualquier `UpdateData` de los seeds de proveedores):
    - Llenar los 3 defaults de `dbo.Suppliers` desde `TOP 1 dbo.Configurations ORDER BY Id`. Si no hay fila, no cambia nada.
    - Backfill de `dbo.OrderItems.RealPriceInLocal = ROUND(oi.RealPrice * o.ExchangeRate, 2)` con JOIN a `dbo.Orders`.
  - Shein (seed `63B4D953-66D5-409E-929D-6036111FB710`) queda con `IsActiveForCalculator = true` en `HasData`. Así EF genera el `UpdateData` y el snapshot queda consistente. Para un Shein con otro Id (creado a mano), se agrega `UPDATE dbo.Suppliers SET IsActiveForCalculator = 1 WHERE Name = 'Shein'`.
  - `Down` elimina las columnas.
- **Rationale**: `Supplier` tiene `HasData`. Agregar propiedades puede hacer que EF genere `UpdateData` para los seeds con valores en 0, y el SQL debe correr después para no quedar sobrescrito.
