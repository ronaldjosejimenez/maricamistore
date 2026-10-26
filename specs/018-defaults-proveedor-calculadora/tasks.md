# Tasks: Defaults por proveedor, calculadora y fórmulas de ítems

**Input**: Design documents from `specs/018-defaults-proveedor-calculadora/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-handlers.md, quickstart.md

**Tests**: No hay proyecto de pruebas automatizadas y el spec no las pide. La verificación es `dotnet build` más el [quickstart.md](quickstart.md).

**Organization**: una fase por historia de usuario. El modelo y la migración son compartidos (Phase 2). Después, US1–US4 tocan archivos casi disjuntos. US1 y US2 comparten `CatalogService.cs` / `ICatalogService.cs`.

## Phase 1: Setup

- [X] T001 Confirmar que la solución compila antes de los cambios: correr `dotnet build` en `MariCamiStore/` y anotar los warnings que ya existen

---

## Phase 2: Foundational (bloquea todas las historias)

**Purpose**: esquema y migración única (research R2, R9; data-model.md).

- [X] T002 [P] En `MariCamiStore/Model/Supplier.cs` agregar, con comentarios XML `<summary>` como los existentes: `decimal TaxPercentageDefault`, `Guid? CurrencyIdDefault`, `decimal ExchangeRateDefault`, `decimal ShippingCostDefault`, `bool IsActiveForCalculator`
- [X] T003 [P] En `MariCamiStore/Model/OrderItem.cs` agregar `decimal RealPriceInLocal` (con comentario XML) justo después de `RealPrice`
- [X] T004 En `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/SupplierEntityTypeConfiguration.cs`:
  - Mapear `TaxPercentageDefault`, `ExchangeRateDefault` y `ShippingCostDefault` como `decimal(18,2)` `IsRequired().HasDefaultValue(0m)`, `CurrencyIdDefault` opcional (sin FK) e `IsActiveForCalculator` `IsRequired().HasDefaultValue(false)`.
  - En `HasData`, poner `IsActiveForCalculator = true` en Shein (`63B4D953-66D5-409E-929D-6036111FB710`).
- [X] T005 [P] En `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderItemEntityTypeConfiguration.cs` mapear `RealPriceInLocal` como `decimal(18,2)` `IsRequired().HasDefaultValue(0m)`, junto a `RealPrice`
- [X] T006 Generar la migración con `dotnet ef migrations add SupplierDefaultsAndRealPriceInLocal` desde `MariCamiStore/` (queda en `MariCamiStore/Infrastructure/Persistance/Migrations/`). Al **final** de `Up`, después de los `AddColumn` y de cualquier `UpdateData` de seeds, agregar `migrationBuilder.Sql` con:
  - Defaults de proveedores desde la Configuración:
    `UPDATE s SET s.TaxPercentageDefault = c.TaxPercentage, s.CurrencyIdDefault = c.OrderCurrencyIdDefault, s.ExchangeRateDefault = c.ExchangeRate FROM dbo.Suppliers s CROSS JOIN (SELECT TOP 1 TaxPercentage, OrderCurrencyIdDefault, ExchangeRate FROM dbo.Configurations ORDER BY Id) c;`
  - Shein activo aunque tenga otro Id: `UPDATE dbo.Suppliers SET IsActiveForCalculator = 1 WHERE Name = N'Shein';`
  - Backfill: `UPDATE oi SET oi.RealPriceInLocal = ROUND(oi.RealPrice * o.ExchangeRate, 2) FROM dbo.OrderItems oi INNER JOIN dbo.Orders o ON o.Id = oi.OrderId;`
  - `Down` (el generado) elimina las columnas. Revisar que el snapshot solo agregue las 6 columnas y el cambio de seed de Shein.
- [X] T007 Correr `dotnet build` y `dotnet ef database update` en local y verificar en BD los defaults de los proveedores, Shein activo y `RealPriceInLocal` lleno

**Checkpoint**: esquema listo; las historias pueden empezar.

---

## Phase 3: User Story 1 - Valores por defecto en el catálogo de proveedores (P1) 🎯 MVP

**Goal**: los proveedores tienen los 5 campos nuevos, editables y validados, con la fila de inserción precargada desde la Configuración.

**Independent Test**: quickstart §1.

- [X] T008 [US1] En `MariCamiStore/Services/CatalogService.cs` agregar `private static void ValidateSupplier(Supplier s)`. Lanza `ArgumentException` con los mensajes de data-model.md, en este orden: moneda (`CurrencyIdDefault` null o `Guid.Empty`) → "La moneda por defecto es obligatoria.", `ExchangeRateDefault <= 0` → "El tipo de cambio por defecto debe ser mayor a cero.", `TaxPercentageDefault < 0` → "El impuesto por defecto no puede ser negativo." y `ShippingCostDefault < 0` → "El costo de envío por defecto no puede ser negativo.". Llamarla al inicio de `CreateSupplierAsync` y `UpdateSupplierAsync`.
- [X] T009 [US1] En `MariCamiStore/Pages/Suppliers/Index.cshtml.cs`:
  - Agregar `OnGetConfigurationAsync()`, que devuelve `new JsonResult(config == null ? null : new { config.TaxPercentage, config.ExchangeRate, CurrencyId = config.OrderCurrencyIdDefault })` usando `catalogService.GetConfigurationAsync()`.
  - Cambiar `OnPostInsertAsync` y `OnPostUpdateAsync` para que devuelvan `IActionResult`: atrapan `ArgumentException` y devuelven `BadRequest(ex.Message)`.
- [X] T010 [US1] En `MariCamiStore/wwwroot/js/pages/suppliers/index.js`:
  - `ajaxPost(handler, data, done, fail)` con un callback de error opcional: en el error hace `alert('Error: ' + xhr.responseText)` y luego `fail()`. Insert y Update hacen `d.reject()` en el fail.
  - En `$(function)`, cargar en paralelo con `$.when` `/Currencies?handler=Load` (mapear a `{id, text: abbreviation, sign}` en `currencyItems`) y `?handler=Configuration` (`cfg`), y después llamar `initGrid()`.
  - Campos nuevos del grid, en este orden después de Nombre: `taxPercentageDefault` ("Impuesto %", number), `currencyIdDefault` ("Moneda", select con `currencyItems`), `exchangeRateDefault` ("Tipo de cambio", number), `shippingCostDefault` ("Envío", number, `itemTemplate` con `formatMoney(val, signo de la moneda del ítem)`) e `isActiveForCalculator` ("Activo calculadora", checkbox).
  - En los 3 campos de defaults, `insertTemplate` llama al template base (`jsGrid.fields.number.prototype.insertTemplate.call(this)` / `select`) y, si existe `cfg`, asigna `cfg.taxPercentage`, `cfg.currencyId` o `cfg.exchangeRate`.
  - FR-003: `shippingCostDefault` usa `insertTemplate` con valor `0` e `isActiveForCalculator` arranca sin marcar (el default del checkbox de jsGrid).

**Checkpoint**: US1 se puede verificar con el quickstart §1.

---

## Phase 4: User Story 2 - Calculadora basada en proveedor (P1)

**Goal**: calculadora con el combo de proveedores activos, envío (× 2 opcional), fórmula nueva y redondeo a la centena; sin "cuenta" y sin query string.

**Independent Test**: quickstart §2.

- [X] T011 [US2] En `MariCamiStore/Services/ICatalogService.cs` y `MariCamiStore/Services/CatalogService.cs` agregar `Task<List<Supplier>> GetCalculatorSuppliersAsync()`, que devuelve `context.Suppliers.Where(s => s.IsActiveForCalculator).OrderBy(s => s.Name).ToListAsync()`, en la sección Suppliers
- [X] T012 [US2] En `MariCamiStore/Pages/Calculator/Index.cshtml.cs` inyectar `ICatalogService` con un constructor primario (como `Suppliers/Index`) y agregar `OnGetSuppliersAsync()`, que devuelve `JsonResult` con `{ id, name, taxPercentageDefault, exchangeRateDefault, shippingCostDefault }` de `GetCalculatorSuppliersAsync()`. Conservar el namespace actual `MariCamiStore.Pages.SheinCalculator`.
- [X] T013 [US2] En `MariCamiStore/Pages/Calculator/Index.cshtml`:
  - Quitar los form-groups `CustomerName` e `ItemDescription`, los botones `btnAdd` y `btnRefresh`, toda la `<section>` del grid (`jsGrid1`, `TitleOfAccount`) y todo el `<script>` inline.
  - Agregar al inicio del card-body un `<select id="SupplierId" class="form-control">` con la etiqueta "Proveedores" y un `<small id="NoSuppliersWarning" class="text-muted" style="display:none">No hay proveedores activos para la calculadora.</small>`.
  - Después de Tipo de cambio, agregar un `custom-control custom-checkbox` con `<input type="checkbox" id="FastShipping">` y la etiqueta "Envío rápido o local", y luego un form-group "Costo envío" con `<input type="number" id="Shipping">`.
  - Orden final: Proveedores, Precio de lista, Impuesto, Tipo de cambio, Envío rápido o local, Costo envío, Costo servicio y resultados. Al final: `<script src="/js/pages/calculator/index.js" asp-append-version="true"></script>`.
- [X] T014 [US2] Crear `MariCamiStore/wwwroot/js/pages/calculator/index.js`:
  - `var suppliers = []`, `var defaults = { serviceCost: 3000 }`.
  - `loadSuppliers()`: `$.get('?handler=Suppliers')`, llena `#SupplierId`. Sin datos, agrega una opción vacía y muestra `#NoSuppliersWarning`; con datos, selecciona el primero y llama `applySupplier()`.
  - `currentSupplier()` y `applySupplier()`: asignan `#Vat` = taxPercentageDefault, `#ExchangeRate` = exchangeRateDefault y `#Shipping` = shippingCostDefault × (`#FastShipping` marcado ? 2 : 1), con `toFixed(2)`. Sin proveedor, dejan los campos vacíos.
  - Eventos: `change` en `#SupplierId` y en `#FastShipping` llaman `applySupplier()` (sobrescriben la edición manual, según los casos borde del spec).
  - `Calculate()` (FR-010): `priceWithVat = list + list*vat/100 + shipping`, `priceLocal = priceWithVat*rate`, `total = Math.ceil((priceLocal + service)/100)*100`. Muestra `#PriceWithVAT` y `#PriceCOL` con `toFixed(2)` y `#Total` con `toFixed(0)`.
  - `Validate()`: igual que hoy (TC > 0, servicio > 0, lista > 0, impuesto > 0) y además Shipping ≥ 0.
  - `Clean()` (FR-012): Precio de lista = 0, Costo servicio = 3000, desmarca `#FastShipping`, `applySupplier()` y resultados en 0.00.
  - Botones `#btnCalculate` y `#btnClean`. No leer `window.location.search`.

**Checkpoint**: US2 se puede verificar con el quickstart §2.

---

## Phase 5: User Story 3 - Orden nueva con defaults del proveedor (P2)

**Goal**: al crear una orden, seleccionar el proveedor carga TC, Impuesto y Moneda.

**Independent Test**: quickstart §3.

- [X] T015 [P] [US3] En `MariCamiStore/wwwroot/js/pages/orders/index.js`:
  - Declarar `var supplierItems = [];` junto a `currencyItems` y, en la carga de `/Suppliers/Index?handler=Load` (~L215), guardar `supplierItems = data || [];`.
  - Agregar `applySupplierDefaults(supplierId)`: busca el proveedor y, si existe, asigna `#order-exchange-rate` = exchangeRateDefault, `#order-tax` = taxPercentageDefault y, si `currencyIdDefault` no es null, `#order-currency` = currencyIdDefault.
  - En `$('#order-supplier').on('change')` (~L141), como **primera** línea: `if (!$('#order-id').val()) applySupplierDefaults($(this).val());`. Después sigue la lógica del nombre sin cambios (FR-014: en edición no se aplica nada).

**Checkpoint**: US3 se puede verificar con el quickstart §3.

---

## Phase 6: User Story 4 - Campos de precio del ítem reordenados y con nuevas fórmulas (P2)

**Goal**: modal reordenado, Precio Real en Local, cadena de fórmulas, campos bloqueados hasta elegir el Tipo de Producto, encabezado con Σ ListPrice solo en Pendientes, Trigger A y Reasignar.

**Independent Test**: quickstart §4.

- [X] T016 [US4] En `MariCamiStore/Services/IOrderService.cs` agregar `decimal RealPriceInLocal` a `OrderItemWithCustomerDto`, después de `RealPrice`. En `MariCamiStore/Services/OrderService.cs`, `GetOrderItemsWithCustomerAsync`, pasar `i.RealPriceInLocal` en la posición correspondiente.
- [X] T017 [US4] En `MariCamiStore/Services/OrderService.cs`:
  - `ApplyOrderCalculationSuite`: `order.TotalWithoutTaxes = Math.Round(items.Sum(i => i.ListPrice), 2);` (FR-021).
  - `UpdateOrderTotalsAsync`: después de obtener la orden, `if (order.Status != OrderStatus.Pending.Key) return;` (FR-022).
  - `RecalcItemsOnOrderSaveAsync` (research R8 / FR-018a), por ítem:
    - `var oldAuto = item.ListPriceTaxWithTax + item.EstimateShipping;`
    - Recalcular `ListPriceTaxWithTax` (como hoy).
    - `if (Math.Abs(item.RealPrice - oldAuto) <= 0.01m) item.RealPrice = item.ListPriceTaxWithTax + item.EstimateShipping;`
    - `item.RealPriceInLocal = Math.Round(item.RealPrice * newExchangeRate, 2);`
    - `if (exchangeRateChanged) item.AgreedPriceInLocal = Math.Round(item.RealPriceInLocal + item.ServiceFeeInLocal, 2);`
- [X] T018 [US4] En `MariCamiStore/Pages/Orders/Items.cshtml.cs`:
  - Agregar `decimal RealPriceInLocal` al record `OrderItemDto`, después de `RealPrice` (el request lo ignora).
  - En el handler de Insert (~L160) y en el de Update (~L200), obtener la orden con `orderService.GetOrderAsync(dto.OrderId)` y asignar `RealPriceInLocal = Math.Round(dto.RealPrice * order.ExchangeRate, 2)` (en Update: `existing.RealPriceInLocal`). Incluir `RealPriceInLocal` en el DTO de respuesta.
  - En `OnPostReasignarAsync`, si la operación fue exitosa, releer la orden y devolver además `totalAgreedPriceInLocal` y `estimatedProfitInLocal` (FR-023).
- [X] T019 [US4] En `MariCamiStore/Pages/Orders/Items.cshtml` (~L262-307), reemplazar las dos filas de precio:
  - Fila 1 (4 × `col-md-3`): Precio Lista `#item-list-price`, Precio + Imp. `#item-list-price-tax-with-tax`, "Envío estimado" `#item-estimate-shipping` y Precio Real `#item-real-price`.
  - Fila 2 (4 × `col-md-3`): "Precio Real en Local" `#item-real-price-in-local` (nuevo, `readonly`), Tarifa Servicio `#item-service-fee-in-local`, Total (calculado) `#item-total` (`readonly`) y Precio Acordado `#item-agreed-price-in-local`.
  - Conservar los ids existentes.
- [X] T020 [US4] En `MariCamiStore/wwwroot/js/pages/orders/items.js`, fórmulas del modal (research R6):
  - Reemplazar `calcModalFields` y `calcModalFromLptpwt` por `recalcModal(source)`:
    - Si `source === 'list'`, recalcula lptwt = round2(lp + lp*tax/100).
    - Si `!userEditedRealPrice && source !== 'real'`, real = round2(lptwt + shipping).
    - realLocal = round2(real*exchangeRate) y total = round2(realLocal + fee); se escriben en `#item-real-price-in-local` y `#item-total`.
    - Si `!userEditedAgreed`, Acordado = total.
  - Handlers:
    - `#item-list-price` input → `recalcModal('list')` (quitar la asignación vieja de real = lista).
    - `#item-list-price-tax-with-tax` → `'lptwt'`.
    - `#item-estimate-shipping` input → `'shipping'` (nuevo).
    - `#item-real-price` input → `userEditedRealPrice = true; recalcModal('real')`.
    - `#item-service-fee-in-local` → `'fee'`.
    - `change` de Tipo de Producto, después de asignar tarifa y envío → `recalcModal('shipping')`.
  - Actualizar `calcItem` con la misma cadena (real, realPriceInLocal, `_total`).
  - En `openAddItem`, inicializar `#item-real-price-in-local` en 0.
- [X] T021 [US4] En `MariCamiStore/wwwroot/js/pages/orders/items.js`, bloqueo por Tipo de Producto y edición (FR-020, FR-020a):
  - Agregar `setPriceFieldsEnabled(enabled)`, que aplica `prop('disabled', !enabled)` a `#item-list-price, #item-list-price-tax-with-tax, #item-estimate-shipping, #item-real-price, #item-service-fee-in-local, #item-agreed-price-in-local`.
  - Llamarla con `false` en `openAddItem`, con `!!id` en el `change` de `#item-product-type` y con `!!item.productTypeId` (y no `Guid.Empty`) en `openEditItem`.
  - En la restauración del modo "Ver" (`restoreItemModalEditable`), volver a aplicar `setPriceFieldsEnabled(!!$('#item-product-type').val())` después de habilitar todo.
  - En `openEditItem` (~L153): `userEditedRealPrice = |realPrice − (listPriceTaxWithTax + estimateShipping)| > 0.01` y `userEditedAgreed = |agreedPriceInLocal − (realPriceInLocal + serviceFeeInLocal)| > 0.01`. Mostrar `#item-real-price-in-local` y `#item-total`.
  - Tabla de ítems (~L272): `var total = round2((parseFloat(item.realPriceInLocal) || 0) + (parseFloat(item.serviceFeeInLocal) || 0));`.
- [X] T022 [US4] Encabezado y Reasignar (FR-021, FR-022, FR-023):
  - En `MariCamiStore/Pages/Orders/Items.cshtml`, junto a `var exchangeRate = ...` (~L371), agregar `var savedTotals = { totalAgreedPriceInLocal: @order.TotalAgreedPriceInLocal, shippingToCR: @order.ShippingAmountToCR, totalWithoutTaxes: @order.TotalWithoutTaxes, taxesAmount: @order.TaxesAmount, totalToSupplier: @order.TotalToPayToSupplier, totalOrder: @order.TotalOfTheOrder, profit: @order.EstimatedProfitInLocal };`. Usar el mismo estilo de emisión que `exchangeRate`, que ya sale en formato invariante.
  - En `MariCamiStore/wwwroot/js/pages/orders/items.js`:
    - Separar de `recalcOrderHeader` una función `renderHeader(totals)` con las 7 líneas `$('#...').text(formatMoney(...))`. `recalcOrderHeader` calcula, llama `renderHeader` y devuelve los totales.
    - En `recalcOrderHeader`, `totalWithoutTaxes = Σ listPrice` (antes realPrice).
    - `refreshTotals()`: `if (!isPending) { renderHeader(savedTotals); return; }` como primera línea. Solo formatea los valores guardados, sin recalcular ni persistir.
    - En el success de Reasignar (~L711), después de `loadItems()`, si vienen `r.totalAgreedPriceInLocal` y `r.estimatedProfitInLocal`, asignarlos a `savedTotals.totalAgreedPriceInLocal` y `savedTotals.profit` y llamar `renderHeader(savedTotals)`.

**Checkpoint**: US4 se puede verificar con el quickstart §4.

---

## Phase 7: Polish & Cross-Cutting

- [X] T023 Grep en `MariCamiStore/` (`*.cs`, `*.js`, `*.cshtml`, excluyendo Migrations) de los usos restantes de `realPrice`/`RealPrice` en cálculos de totales, y de `calcModalFields`/`calcModalFromLptpwt`/`getUrlParameter`; corregir lo que quede
- [X] T024 `dotnet build` sin warnings nuevos respecto a T001
- [X] T025 Recorrer el `specs/018-defaults-proveedor-calculadora/quickstart.md` completo (§1–§4) en local y confirmar los valores esperados exactos
- [X] T026 Generar el script de producción con `dotnet ef migrations script --idempotent` desde `MariCamiStore/` para la migración `SupplierDefaultsAndRealPriceInLocal`

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2 → historias.** T006 depende de T002–T005; T007 depende de T006.
- **US1 (T008–T010)** y **US2 (T011–T014)** comparten `CatalogService.cs`: hacer T008 y T011 en secuencia (o juntos). El resto de US1 y US2 es independiente.
- **US3 (T015)** solo depende de Phase 2 (usa los campos que devuelve el Load de proveedores).
- **US4 (T016–T022)**: T016 → T017 (mismo archivo `OrderService.cs`); T018 depende de T016. T019 es independiente. T020 → T021 → T022 (mismo archivo `items.js`).
- **Polish** al final.

## Parallel Opportunities

- Phase 2: T002, T003 y T005 en paralelo.
- Después de Phase 2: US3 (T015) y T019 (markup de Items) en paralelo con cualquier otra historia; T012–T014 de la calculadora en paralelo con T009–T010 de proveedores.

## Implementation Strategy

1. **MVP**: Phase 1 + Phase 2 + US1. Los proveedores ya tienen sus defaults y se pueden configurar.
2. US2 (calculadora): el beneficio diario más visible.
3. US3 (órdenes): cambio pequeño de un solo archivo.
4. US4 (ítems): el más delicado por las fórmulas y los totales; verificar el quickstart §4 con cuidado, incluido que una orden no Pendiente no cambie sus totales al abrirla.
