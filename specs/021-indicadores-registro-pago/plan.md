# Implementation Plan: Indicadores en el registro de pago y estado Recibido de los ítems

**Branch**: `021-indicadores-registro-pago` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/021-indicadores-registro-pago/spec.md`

## Summary

Cubre los issues #44 y #48. El trabajo tiene tres partes:

1. **Estado `Received`.** Reemplaza al booleano `OrderItem.IsReceived`. Se hace con dos migraciones: datos (`IsReceived = 1` → `Status = 'Received'`) y esquema (se elimina la columna). El checkbox "Recibido" escribe el `Status`.
2. **Consistencia.** Toda condición que hoy significa "ítem no anulado" pasa de `Status == Active` a `OrderItemStatus.CountedKeys.Contains(Status)`, una lista positiva de los estados que cuentan: Active y Received (inventario en research R2). El caso crítico es la anulación de la orden completa, que debe generar el Void también de los ítems recibidos.
3. **Etiquetas en Saldos de clientes.** Se calculan en `PaymentService.GetSaldosReportAsync` **después** del saldo, que no cambia. Se hace una consulta agrupada sobre los ítems de los deudores en órdenes Active/Delivering, con el mismo alcance de organización que el saldo. Se agregan 3 campos a `SaldoReportRow` y `renderSaldos` pinta las etiquetas con sus colores.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery, AdminLTE 3 / Bootstrap 4)

**Primary Dependencies**: EF Core 10 (SQL Server); migraciones en `MariCamiStore/Infrastructure/Persistance/Migrations`

**Storage**:
- Se elimina `dbo.OrderItems.IsReceived`.
- `dbo.OrderItems.Status` acepta `'Received'`.
- Dos migraciones: `MigrateIsReceivedToStatus` (solo SQL) y `DropOrderItemIsReceived`. Ambas reversibles (research R4).

**Testing**: no hay proyecto de pruebas automatizadas. La verificación es `dotnet build` más el [quickstart.md](quickstart.md) manual, que compara antes y después de migrar y trae casos de etiquetas con valores exactos.

**Target Platform**: Azure App Service container

**Constraints**:
- El cálculo del saldo no se toca (FR-012).
- OrderItems, Orders y Transactions tienen query filter por organización. El reporte de saldos usa `IgnoreQueryFilters` + `organizationId`, y las etiquetas replican ese alcance.
- El pipeline no aplica migraciones: el script idempotente se corre antes del deploy.

**Scale/Scope**: cientos de clientes y miles de ítems. Las etiquetas agregan 1 consulta agrupada por carga de Saldos (SC-006: < 2 s).

## Constitution Check

`.specify/memory/constitution.md` es la plantilla sin llenar. Gate: **PASS**. Post-design: **PASS**.
- No hay proyectos ni dependencias nuevas.
- Se reutilizan los patrones existentes: `Enumeration`, migraciones de datos con `migrationBuilder.Sql` (como en la 017), `(bool Success, string? Error)` en los servicios y handlers JSON con antiforgery.

## Project Structure

### Documentation (this feature)

```text
specs/021-indicadores-registro-pago/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/ui-handlers.md
├── checklists/requirements.md
└── tasks.md            (lo genera /speckit-tasks)
```

### Source Code

```text
MariCamiStore/
├── Model/
│   ├── OrderItem.cs                               [MODIFY] quitar IsReceived
│   └── OrderItemStatus.cs                         [MODIFY] + Received ("Recibido") en List()
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/OrderItemEntityTypeConfiguration.cs  [MODIFY] quitar mapeo IsReceived
│   └── Migrations/
│       ├── <ts>_MigrateIsReceivedToStatus.cs      [NEW] solo SQL (Up/Down), creada ANTES de quitar la propiedad
│       └── <ts>_DropOrderItemIsReceived.cs        [NEW] dotnet ef migrations add, DESPUÉS de quitarla
├── Services/
│   ├── IOrderService.cs                           [MODIFY] OrderItemWithCustomerDto sin IsReceived; SetItemReceivedAsync
│   ├── OrderService.cs                            [MODIFY] filtros CountedKeys (R2); SetItemReceivedAsync
│   ├── IPaymentService.cs                         [MODIFY] SaldoReportRow + ItemsInTransit, SuggestAdvance, DeliveredToCollect
│   └── PaymentService.cs                          [MODIFY] etiquetas después del saldo (R5)
├── Pages/
│   └── Orders/Items.cshtml.cs                     [MODIFY] OnPostToggleReceivedAsync → SetItemReceivedAsync
└── wwwroot/js/pages/
    ├── orders/items.js                            [MODIFY] status === 'Received' (fila verde, checkbox)
    └── payments/index.js                          [MODIFY] renderSaldos: sin "Saldo pendiente"; 3 etiquetas nuevas
```

**Structure Decision**: una sola web app. Las reglas de las etiquetas quedan en `PaymentService` y la vista solo pinta.

## Design Notes

### Estado Received
- **`OrderItemStatus`**: `public static OrderItemStatus Received = new OrderItemStatus(nameof(Received), "Recibido");`, con comentario XML. `List()` devuelve `{ Active, Received, Voided }`. Además `public static readonly string[] CountedKeys = { Active.Key, Received.Key }`.
- **`SetItemReceivedAsync(Guid itemId, bool received)`**, que reemplaza a `ToggleIsReceivedAsync`. Mantiene los mismos guards y mensajes y asigna `item.Status = received ? Received.Key : Active.Key` y `UpdatedAt`.
- **`GetOrderItemsWithCustomerAsync`**: el DTO deja de llevar `IsReceived`; sigue llevando `Status`.

### Consistencia (#48)
Se aplica research R2. Los cambios son de una línea cada uno: `== OrderItemStatus.Active.Key` → `OrderItemStatus.CountedKeys.Contains(...)`. Lugares:
- `GetOrderItemCountsAsync`
- `ComputeOrderTotals`
- `TransitionOrderAsync` (→ Voided)
- `ReasignarItemAsync` (`otherTotal`)
- `PreviewVoidItemsAsync`
- `VoidItemsAsync`

Se actualizan los comentarios que dicen "active items" cuando el sentido es "no anulados" (p. ej. el XML de `OrderTotals`).

### Etiquetas (`PaymentService.GetSaldosReportAsync`)
1. El cálculo actual de `rows` queda **tal cual**.
2. `var debtorIds = rows.Where(r => r.Balance > 0).Select(r => r.CustomerId).ToList();`
3. Si `debtorIds.Count > 0`, se hace la consulta de research R5 (`IgnoreQueryFilters`, `OrderItemStatus.CountedKeys.Contains(Status)`, orden Active/Delivering, `organizationId` opcional), agrupada por `CustomerId`, con `InTransit`, `Received` y `AgreedSum`. El resultado va a un `Dictionary`.
4. En el `Select` final de `SaldoReportRow` se derivan los campos según `data-model.md`. Los clientes sin entrada en el diccionario, o con `Balance <= 0`, quedan con `0` / `false`.

### UI
- **`items.js`**:
  - línea ~302: `else if (item.status === 'Received') $tr.addClass('table-success');`
  - línea ~336: `if (item.status === 'Received') $chk.prop('checked', true);`
  - El handler `.is-received-chk` no cambia: sigue enviando `isReceived`. Solo se actualiza su comentario ("IsReceived" → "Received status").
- **`payments/index.js` → `renderSaldos`**: se arman los badges en el orden "Crédito a favor", "Productos en camino (n)", "Sugerir adelanto" y "Entregado – por cobrar", según el contrato. Se elimina la rama de "Saldo pendiente". Los textos se escapan con `escapeHtml` cuando corresponde; son literales, salvo el número.

### Migraciones
1. Con el modelo **todavía** con `IsReceived`, se ejecuta `dotnet ef migrations add MigrateIsReceivedToStatus`. Sale vacía y se completa con el SQL Up/Down de R4, con comentario al estilo de `RemoveDeliveredOrderStatus`.
2. Se quita `IsReceived` del modelo y de la configuración y se ejecuta `dotnet ef migrations add DropOrderItemIsReceived`. Hay que revisar que solo contenga el `DropColumn` / `AddColumn`.
3. Para producción se genera `dotnet ef migrations script --idempotent`.

## Complexity Tracking

Sin violaciones.
