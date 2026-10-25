# Implementation Plan: Defaults por proveedor, calculadora y fórmulas de ítems

**Branch**: `018-defaults-proveedor-calculadora` | **Date**: 2026-10-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/018-defaults-proveedor-calculadora/spec.md`

## Summary

Cuatro partes que comparten una migración:

1. **Proveedores (P1)**: `Supplier` gana `TaxPercentageDefault`, `CurrencyIdDefault`, `ExchangeRateDefault`, `ShippingCostDefault` e `IsActiveForCalculator`. El grid jsGrid precarga Impuesto, Moneda y TC desde la Configuración en la fila de inserción. `CatalogService` valida (FR-002a). La migración llena los 3 defaults de los proveedores existentes desde `TOP 1 Configurations`.
2. **Calculadora (P1)**: el script inline pasa a `wwwroot/js/pages/calculator/index.js`. Se agregan el combo de proveedores activos (nuevo handler `Suppliers`), el checkbox de envío rápido (× 2) y el campo Costo envío. Se quitan los campos de cliente y prenda, la "cuenta" y la lectura del query string. Fórmula nueva con Total redondeado a la centena superior.
3. **Órdenes (P2)**: en `orders/index.js`, al seleccionar un proveedor en modo creación, se cargan TC, Impuesto y Moneda desde la lista de proveedores que ya se carga.
4. **Ítems (P2)**: nuevo `OrderItem.RealPriceInLocal` (persistido, calculado en el servidor). El modal se reordena, cambia la etiqueta de envío, se encadenan las fórmulas (Precio Real = Precio + Imp + Envío → × TC → + Tarifa → Acordado) y los campos quedan bloqueados hasta elegir el Tipo de Producto. El encabezado usa Σ ListPrice como Subtotal (cliente y servidor), la columna Total de la tabla usa la nueva fórmula y Trigger A se adapta (FR-018a).

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery, jsGrid, select2, AdminLTE 3 / Bootstrap 4)

**Primary Dependencies**: EF Core 10 (SQL Server); migraciones en `MariCamiStore/Infrastructure/Persistance/Migrations`

**Storage**: Cambio de esquema: 5 columnas en `dbo.Suppliers` y 1 en `dbo.OrderItems`. Una migración (`SupplierDefaultsAndRealPriceInLocal`) con SQL de llenado y backfill al final de `Up`.

**Testing**: No hay proyecto de pruebas automatizadas. La verificación es `dotnet build` más el [quickstart.md](quickstart.md) manual, que trae valores esperados exactos.

**Target Platform**: Azure App Service container

**Constraints**: Montos `decimal(18,2)` con redondeo a 2 decimales (`Math.Round` en el servidor, `round2` en el cliente). El redondeo a la centena aplica solo al Total de la calculadora. Los proveedores son globales (sin filtro de organización) y la Configuración es por organización (con query filter).

**Scale/Scope**: Decenas de proveedores; ítems por orden en decenas. El backfill de `RealPriceInLocal` es un solo UPDATE con JOIN.

## Constitution Check

`.specify/memory/constitution.md` es la plantilla sin llenar, sin principios ratificados. Gate: **PASS**. Post-design: **PASS** (sin proyectos ni dependencias nuevas; se reutilizan los patrones existentes: jsGrid, handlers JSON, validación por `ArgumentException` en `CatalogService`).

## Project Structure

### Documentation (this feature)

```text
specs/018-defaults-proveedor-calculadora/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/ui-handlers.md
├── checklists/requirements.md
└── tasks.md            (lo genera /speckit-tasks)
```

### Source Code

```text
MariCamiStore/
├── Model/
│   ├── Supplier.cs                                [MODIFY] 5 propiedades nuevas
│   └── OrderItem.cs                               [MODIFY] RealPriceInLocal
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/SupplierEntityTypeConfiguration.cs   [MODIFY] mapeo + defaults
│   ├── EntityConfigurations/OrderItemEntityTypeConfiguration.cs  [MODIFY] mapeo RealPriceInLocal
│   └── Migrations/<ts>_SupplierDefaultsAndRealPriceInLocal.cs   [NEW] dotnet ef + SQL llenado/backfill
├── Services/
│   ├── CatalogService.cs                          [MODIFY] validación de proveedor; GetCalculatorSuppliersAsync
│   ├── ICatalogService.cs                         [MODIFY] firma nueva
│   ├── IOrderService.cs                           [MODIFY] OrderItemWithCustomerDto + RealPriceInLocal
│   └── OrderService.cs                            [MODIFY] Σ ListPrice; Trigger A (R8); DTO
├── Pages/
│   ├── Suppliers/Index.cshtml.cs                  [MODIFY] OnGetConfigurationAsync; 400 en validación
│   ├── Calculator/Index.cshtml                    [MODIFY] markup nuevo; sin script inline
│   ├── Calculator/Index.cshtml.cs                 [MODIFY] ICatalogService + OnGetSuppliersAsync
│   ├── Orders/Items.cshtml                        [MODIFY] reorden de campos, etiqueta, P. Real en Local
│   └── Orders/Items.cshtml.cs                     [MODIFY] DTO + cálculo de RealPriceInLocal en Insert/Update
└── wwwroot/js/pages/
    ├── suppliers/index.js                         [MODIFY] columnas nuevas, insertTemplate con defaults
    ├── calculator/index.js                        [NEW] lógica de la calculadora
    ├── orders/index.js                            [MODIFY] supplierItems + defaults en creación
    └── orders/items.js                            [MODIFY] recalcModal, gating por Tipo de Producto, header Σ listPrice, columna Total
```

**Structure Decision**: Una sola web app. `CatalogService` lo tocan las partes 1 y 2; `OrderService` y `items.js` solo la parte 4.

## Design Notes

### Parte 1 — Proveedores
- **Grid** (`suppliers/index.js`): primero se cargan las monedas (`/Currencies?handler=Load`) y la Configuración (`?handler=Configuration`) en paralelo (`$.when`), y después `initGrid()`. Columnas: Nombre, Impuesto %, Moneda (select), TC, Envío (con signo de la moneda) y Activo calculadora (checkbox). Los campos de los 3 defaults usan `insertTemplate`, que llama al template base y asigna el valor de la Configuración.
- **Validación**: `CatalogService.ValidateSupplier(supplier)` es privado y se llama en Create y Update; lanza `ArgumentException`. En la página, los handlers Insert y Update atrapan `ArgumentException` y devuelven `BadRequest(ex.Message)`. En el JS, `ajaxPost` recibe un callback de error que llama `d.reject()`.

### Parte 2 — Calculadora
- **Markup**: Proveedores (select) → Precio de lista → Impuesto % → Tipo de cambio → checkbox "Envío rápido o local" → Costo envío → Costo servicio → resultados. Los botones son solo Calcular y Limpiar. Se eliminan la sección del grid y `#TitleOfAccount`.
- **JS**: `suppliers` en memoria; `applySupplier()` asigna Vat, TC y Shipping (= base × factor). `Calculate()` aplica FR-010 con `Math.ceil(total/100)*100`. `Validate()` permite Shipping ≥ 0. Si no hay proveedores activos, aparece una opción vacía y un aviso `small.text-muted` ("No hay proveedores activos para la calculadora").
- Costo servicio arranca en 3000, como hoy.

### Parte 3 — Órdenes
- `supplierItems = data` en la carga del dropdown. El handler `change` primero llama `applySupplierDefaults()` si `!$('#order-id').val()`, y después sigue la lógica del nombre (con su propio `return`).

### Parte 4 — Ítems
- **Markup**: fila 1 con 4 × `col-md-3`; fila 2 con 4 × `col-md-3`. Nuevo `#item-real-price-in-local` (readonly). `#item-total` (readonly). Etiqueta "Envío estimado".
- **`recalcModal(source)`**: `source` ∈ {`list`, `lptwt`, `shipping`, `real`, `fee`}. Si es `list`, recalcula Precio + Imp; si `!userEditedRealPrice` y `source ≠ real`, Precio Real = lptwt + shipping; P. Real en Local = round2(real × exchangeRate); Total = round2(realLocal + fee); si `!userEditedAgreed`, Acordado = Total. Ahora `#item-estimate-shipping` también dispara el recálculo.
- **Gating**: `setPriceFieldsEnabled(bool)` actúa sobre Lista, Precio + Imp, Envío, Precio Real, Tarifa y Acordado (`prop('disabled')`); los readonly no cambian. Se llama con `false` en `openAddItem`, con `true` en el `change` de Tipo de Producto cuando hay un id y con `!!item.productTypeId` en `openEditItem`. El modo "Ver" (`restoreItemModalEditable`) debe respetar el gating.
- **openEditItem**: los flags de edición manual se detectan con la fórmula nueva (research R6) y se muestra `realPriceInLocal`.
- **Tabla**: Total = `realPriceInLocal + serviceFeeInLocal`.
- **Header**: `totalWithoutTaxes = Σ listPrice` en `recalcOrderHeader` y `ApplyOrderCalculationSuite`.
- **Servidor**: en Insert y Update, `RealPriceInLocal = Math.Round(dto.RealPrice * order.ExchangeRate, 2)`; la orden se obtiene con `orderService.GetOrderAsync(dto.OrderId)`, como en `OnPostUpdateOrderAsync`. Trigger A según research R8.

## Complexity Tracking

Sin violaciones.
