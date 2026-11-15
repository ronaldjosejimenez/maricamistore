# Implementation Plan: Anulación parcial de ítems de una orden activa

**Branch**: `020-anulacion-items-orden` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/020-anulacion-items-orden/spec.md`

## Summary

En una orden `Active` se pueden anular ítems seleccionados. El flujo es:

1. Cada ítem se marca `Voided`; nuevo `OrderItem.Status`, con el enum reducido a Active/Voided.
2. Se registra un `Void` por ítem al cliente, en moneda local.
3. Se recalculan los totales de la orden sin los anulados, usando una función pura `ComputeOrderTotals` compartida entre la vista previa y la persistencia.
4. Se registra **una** entrada CxP de tipo nuevo `AnulacionItems`, con monto positivo, que **resta** en los indicadores vía `CxPEntryType.SignedAmount`.

Todo ocurre en un solo `SaveChangesAsync`. La UI agrega:
- una toolbar con "Anular";
- un modo selección con checkboxes;
- un modal con montos por ítem, justificación, comparativa Antes/Después (vista previa del servidor) y ajuste;
- filas anuladas con fondo rojo pastel.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery, AdminLTE 3 / Bootstrap 4)

**Primary Dependencies**: EF Core 10 (SQL Server); migraciones en `MariCamiStore/Infrastructure/Persistance/Migrations`

**Storage**: una columna nueva, `dbo.OrderItems.Status` (nvarchar(20), default `'Active'`). Migración `AddOrderItemStatus`. Sin cambios de esquema en CxP ni en Transactions.

**Testing**: no hay proyecto de pruebas automatizadas. La verificación es `dotnet build` más el [quickstart.md](quickstart.md) manual, que trae valores esperados exactos.

**Target Platform**: Azure App Service container

**Constraints**: montos `decimal(18,2)` con `Math.Round(..., 2, AwayFromZero)`. Las transacciones al cliente van en moneda local y la CxP en la moneda de la orden. OrderItems y Transactions tienen query filter por organización. El pipeline no aplica migraciones: el script se ejecuta antes del deploy.

**Scale/Scope**: decenas de ítems por orden; anulaciones esporádicas, con un solo operador.

## Constitution Check

`.specify/memory/constitution.md` es la plantilla sin llenar. Gate: **PASS**. Post-design: **PASS** (sin proyectos ni dependencias nuevas; se reutilizan los patrones existentes: handlers JSON con antiforgery, `(bool Success, string? Error)` en los servicios, un `SaveChanges` atómico como en `OrderPackageService.DeleteAsync`).

## Project Structure

### Documentation (this feature)

```text
specs/020-anulacion-items-orden/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/ui-handlers.md
├── checklists/requirements.md
└── tasks.md            (lo genera /speckit-tasks)
```

### Source Code

```text
MariCamiStore/
├── Model/
│   ├── OrderItem.cs                               [MODIFY] Status (default Active)
│   ├── OrderItemStatus.cs                         [MODIFY] solo Active, Voided
│   └── CxPEntryType.cs                            [MODIFY] AnulacionItems + SignedAmount()
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/OrderItemEntityTypeConfiguration.cs  [MODIFY] mapeo Status (max 20, default 'Active')
│   └── Migrations/<ts>_AddOrderItemStatus.cs     [NEW] dotnet ef migrations add
├── Services/
│   ├── IOrderService.cs                           [MODIFY] DTO + Status; records VoidItemLine/VoidItemsPreviewDto/OrderTotals; firmas nuevas
│   ├── OrderService.cs                            [MODIFY] ComputeOrderTotals; PreviewVoidItemsAsync; VoidItemsAsync; filtros Voided (R6)
│   └── CxPService.cs                              [MODIFY] SignedAmount en GetPeriodIndicatorsAsync
├── Pages/
│   ├── Orders/Items.cshtml                        [MODIFY] toolbar, barra de selección, modal #modalVoidItems, CSS .table-voided
│   ├── Orders/Items.cshtml.cs                     [MODIFY] OnPostVoidItemsPreviewAsync, OnPostVoidItemsAsync, guard Voided en Update
│   └── CxP/Index.cshtml.cs                        [MODIFY] amount/total con signo
└── wwwroot/js/pages/
    ├── orders/items.js                            [MODIFY] modo selección, modal, render de Voided, exclusión en subtotales/conteo
    └── cxp/index.js                               [MODIFY] TYPE_LABELS['AnulacionItems'] = 'Anulación de ítems'
```

**Structure Decision**: una sola web app. Toda la lógica de negocio queda en `OrderService`; la página solo traduce JSON.

## Design Notes

### Servicio
- **`ComputeOrderTotals(Order, IEnumerable<OrderItem>)`** (privado y estático, puro): son las fórmulas actuales de `ApplyOrderCalculationSuite`. `ApplyOrderCalculationSuite` pasa a llamarla con `items.Where(Active)` y asignar los resultados.
- **`PreviewVoidItemsAsync(orderId, itemIds)`** → `(VoidItemsPreviewDto? Preview, string? Error)`. Valida la orden Activa y que los ítems sean Active y de la orden. `before` sale de los campos guardados de la orden; `after` sale de `ComputeOrderTotals(order, active − selected)`. Incluye `allSelected` y los ítems con `defaultRefund = ListPriceTaxWithTax` y el nombre del cliente (mismo criterio que `GetOrderItemsWithCustomerAsync`: NickName o Name).
- **`VoidItemsAsync(orderId, justification, IReadOnlyList<VoidItemLine>)`** → `(bool, string?)`, con validaciones en el orden del contrato:
  1. Período: `cxpService.GetOpenPeriodAsync()`; si es null, devuelve error.
  2. Por ítem: `Status = Voided`, `UpdatedAt` y un `Transaction` Void (R5).
  3. Totales: `ApplyOrderCalculationSuite(order, todosLosItems)`, que ya filtra los Active.
  4. CxP: `context.CxPEntries.Add(new CxPEntry { Type = AnulacionItems, Amount = Σ refund, CurrencyId = order.CurrencyId, OrderId, Reference })`.
  5. Un solo `SaveChangesAsync`.
- **`TransitionOrderAsync`** al pasar a Voided: `items.Where(i => i.Status == Active)`.
- **`ReasignarItemAsync`**, **`ToggleIsReceivedAsync`** y **`GetOrderItemCountsAsync`**: aplican los filtros y guards de R6.

### CxP
- `GetPeriodIndicatorsAsync`: `group.Sum(e => CxPEntryType.SignedAmount(e.Type, e.Amount))`.
- `OnGetEntriesAsync`: `Amount = SignedAmount(...)` por fila y en `total`.

### UI (`Items.cshtml` / `items.js`)
- **Header**: la columna derecha pasa a `div.btn-toolbar.justify-content-end` con "Volver" y `@if (order.Status == Active)` `#btn-void-items` (`btn btn-danger btn-sm`, `fa-ban`, "Anular").
- **Modo selección** (`voidSelectMode` flag):
  - `renderItemsTable` antepone una columna `<th>` con un checkbox por fila Active (`.void-chk`).
  - `#void-select-bar` (encima de la tabla) tiene "Anular seleccionados" (`#btn-void-selected`, deshabilitado si hay 0) y "Cancelar".
  - Al salir del modo, la selección se descarta y se vuelve a renderizar.
- **Modal `#modalVoidItems`**: se abre después de `VoidItemsPreview`. Contiene:
  - el mensaje de confirmación;
  - una tabla de ítems (descripción, cliente, acordado e input `.void-refund` con step 0.01 y min 0, precargado con `defaultRefund`);
  - la tabla Antes/Después de los 7 totales (signos de moneda como en el header);
  - el bloque de ajuste: Ajuste = Σ refund, Teórico y Diferencia, recalculados en el `input` de `.void-refund`;
  - `#void-justification` (textarea obligatorio);
  - `#void-all-warning`, visible si `allSelected`.

  "Continuar con la anulación" se deshabilita si `allSelected`, si la justificación está vacía o si hay algún monto inválido o < 0. Al confirmar se hace POST `VoidItems`; si tiene éxito, `location.reload()`, y si falla se muestra el error en el modal.
- **Render Voided**: `$tr.addClass('table-voided')`, un badge `badge-danger` "Anulado" junto a la descripción, sin botones Reasignar ni checkbox Recibido (solo Ver y Ver Img). No suma en `groupSubtotal` ni en `grandTotal`. `#item-count-badge` cuenta solo los Active.
- **CSS**: `.table-voided, .table-voided > td { background-color: #f8d7da; }` en `Items.cshtml`.

### Migración
`dotnet ef migrations add AddOrderItemStatus`: una sola columna con default, sin SQL manual. Generar el script idempotente para producción (`dotnet ef migrations script --idempotent`), como en la 018.

## Complexity Tracking

Sin violaciones.
