# Tasks: Anulación parcial de ítems de una orden activa

**Input**: Design documents from `specs/020-anulacion-items-orden/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-handlers.md, quickstart.md

**Tests**: No hay proyecto de pruebas automatizadas y el spec no las pide. La verificación es `dotnet build` más el [quickstart.md](quickstart.md).

**Organization**: la Phase 2 deja listos el esquema, el signo de CxP y el cálculo de totales compartido, para que ningún incremento deje la CxP sumando en vez de restar. US1 es el flujo completo de anulación, US2 la comparativa del modal y US3 la presentación en CxP.

## Phase 1: Setup

- [ ] T001 Confirmar que la solución compila antes de los cambios: correr `dotnet build` en `MariCamiStore/` y anotar los warnings que ya existen

---

## Phase 2: Foundational (bloquea todas las historias)

**Purpose**: estado del ítem, tipo CxP que resta y cálculo puro de totales (research R1, R2, R3; data-model.md).

- [ ] T002 [P] En `MariCamiStore/Model/OrderItemStatus.cs` dejar solo `Active` ("Activo") y `Voided` ("Anulado"): eliminar `Pending`, `Checked` y `Delivered`, actualizar `List()` a `new[] { Active, Voided }` y corregir los comentarios XML copiados de OrderStatus ("An order item status", etc.)
- [ ] T003 [P] En `MariCamiStore/Model/OrderItem.cs` agregar `public string Status { get; set; } = OrderItemStatus.Active.Key;` con comentario XML, después de `IsReceived`
- [ ] T004 [P] En `MariCamiStore/Model/CxPEntryType.cs` agregar `public const string AnulacionItems = "AnulacionItems";` y `public static decimal SignedAmount(string type, decimal amount) => type == AnulacionItems ? -amount : amount;`, con un comentario que explique que el monto se guarda positivo y es el tipo el que resta
- [ ] T005 En `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderItemEntityTypeConfiguration.cs` mapear `Status` con `IsRequired().HasMaxLength(20).HasDefaultValue(OrderItemStatus.Active.Key)`, junto a `IsReceived`
- [ ] T006 Generar la migración con `dotnet ef migrations add AddOrderItemStatus` desde `MariCamiStore/` (queda en `MariCamiStore/Infrastructure/Persistance/Migrations/`). Revisar que `Up` solo agregue `Status nvarchar(20) NOT NULL DEFAULT 'Active'` en `OrderItems` y que el snapshot no tenga más cambios. Correr `dotnet ef database update` en local
- [ ] T007 En `MariCamiStore/Services/CxPService.cs` `GetPeriodIndicatorsAsync` cambiar `group.Sum(e => e.Amount)` por `group.Sum(e => CxPEntryType.SignedAmount(e.Type, e.Amount))`. La vista previa y el cierre (`GetClosePreviewAsync` y `ClosePeriodAsync`) ya usan estos indicadores; confirmar que no haya otra suma de `CxPEntries`
- [ ] T008 En `MariCamiStore/Pages/CxP/Index.cshtml.cs` (requiere T004) `OnGetEntriesAsync` exponer por fila `Amount = CxPEntryType.SignedAmount(e.Type, e.Amount)` y calcular `total` con la misma función
- [ ] T009 En `MariCamiStore/Services/IOrderService.cs` agregar `public record OrderTotals(decimal TotalAgreedPriceInLocal, decimal ShippingAmountToCR, decimal TotalWithoutTaxes, decimal TaxesAmount, decimal TotalToPayToSupplier, decimal TotalOfTheOrder, decimal EstimatedProfitInLocal);` y agregar `string Status` al final de `OrderItemWithCustomerDto`
- [ ] T010 En `MariCamiStore/Services/OrderService.cs`:
  - Extraer de `ApplyOrderCalculationSuite` un `private static OrderTotals ComputeOrderTotals(Order order, IEnumerable<OrderItem> items)` con las mismas fórmulas y el mismo redondeo `AwayFromZero`.
  - `ApplyOrderCalculationSuite` pasa a llamarla con `items.Where(i => i.Status == OrderItemStatus.Active.Key)`, asigna los 7 campos y actualiza `UpdatedAt`.
  - `GetOrderItemsWithCustomerAsync` llena `Status`.

**Checkpoint**: compila; las órdenes existentes se ven igual (todos los ítems Active); CxP sin cambios visibles.

---

## Phase 3: User Story 1 - Anular ítems seleccionados de una orden activa (P1) 🎯 MVP

**Goal**: toolbar con "Anular", selección por checkboxes, confirmación con monto a devolver y justificación, procesamiento atómico y filas anuladas en rojo pastel.

**Independent Test**: quickstart §1, §3 (sin validar todavía la comparativa) y §5.

- [ ] T011 [US1] En `MariCamiStore/Services/IOrderService.cs` agregar los records y las firmas:
  - `VoidItemLine(Guid ItemId, decimal RefundAmount)`
  - `VoidPreviewItemDto(Guid Id, string ProductDescription, string CustomerDisplayName, decimal AgreedPriceInLocal, decimal DefaultRefund)`
  - `VoidItemsPreviewDto(bool AllSelected, OrderTotals Before, OrderTotals After, decimal TheoreticalRefund, List<VoidPreviewItemDto> Items)`
  - `Task<(VoidItemsPreviewDto? Preview, string? Error)> PreviewVoidItemsAsync(Guid orderId, IReadOnlyList<Guid> itemIds)`
  - `Task<(bool Success, string? Error)> VoidItemsAsync(Guid orderId, string? justification, IReadOnlyList<VoidItemLine> items)`
- [ ] T012 [US1] En `MariCamiStore/Services/OrderService.cs` implementar `PreviewVoidItemsAsync`:
  - Valida (mensajes de contracts/ui-handlers.md): orden existe y está Active, lista no vacía y todos los ids son ítems Active de la orden.
  - `Before` = un `OrderTotals` con los campos guardados de la orden; `After` = `ComputeOrderTotals(order, activos que no están seleccionados)`.
  - `TheoreticalRefund = Before.TotalToPayToSupplier - After.TotalToPayToSupplier`; `AllSelected` = la selección cubre todos los Active.
  - Por ítem: `DefaultRefund = ListPriceTaxWithTax` y el nombre del cliente con el mismo criterio que `GetOrderItemsWithCustomerAsync` (NickName o Name).
- [ ] T013 [US1] En `MariCamiStore/Services/OrderService.cs` implementar `VoidItemsAsync` (research R4, R5; plan Design Notes):
  - Validaciones en el orden del contrato: orden Active, ≥1 ítem, justificación no vacía y ≤ 1000, montos ≥ 0, ítems Active de la orden, no todos y período abierto (`cxpService.GetOpenPeriodAsync()`).
  - Por ítem: `Status = Voided`, `UpdatedAt` y un `Transaction` Void. Este usa `CustomerId` del ítem, `AgreedPriceInLocal`, `LocalCurrencyId` de la Configuración, fecha `Central America Standard Time`, `Applied`, `Source = OrderItem` y la descripción `Anulación de ítem – {desc≤100} – Orden {NameOfOrder} – {justificación}`, truncada al largo de la columna (verificar en `TransactionEntityTypeConfiguration.cs`).
  - `ApplyOrderCalculationSuite(order, todosLosItems)`.
  - `context.CxPEntries.Add` con `Type = CxPEntryType.AnulacionItems`, `Amount = Σ RefundAmount`, `CurrencyId = order.CurrencyId`, `OrderId`, `PeriodControlId` y `Reference` = `Anulación ítems: {NameOfOrder} – {justificación}`, truncada al largo de `CxPEntryEntityTypeConfiguration`.
  - `context.OrderStatusHistories.Add` con From/To = Active, `TransitionDate` = fecha CR, `Notes = $"Anulación parcial: {n} ítem(s) – ajuste {sign}{monto:N2}"` y `Justification`. El `sign` sale de `(await context.Currencies.FirstOrDefaultAsync(c => c.Id == order.CurrencyId))?.Sign ?? string.Empty`.
  - **Un solo** `SaveChangesAsync`.
- [ ] T014 [US1] En `MariCamiStore/Services/OrderService.cs` aplicar las exclusiones de research R6:
  - `GetOrderItemCountsAsync` cuenta solo los Active.
  - `ReasignarItemAsync` devuelve "No se puede reasignar un ítem anulado." si está Voided, y `otherTotal` suma solo los Active.
  - `ToggleIsReceivedAsync` devuelve "No se puede modificar un ítem anulado." si está Voided.
  - `TransitionOrderAsync`, en la rama Voided, genera `Void` solo para los Active.
- [ ] T015 [US1] En `MariCamiStore/Pages/Orders/Items.cshtml.cs`:
  - Agregar `public record VoidItemsPreviewRequest(Guid OrderId, List<Guid> ItemIds)` y `public record VoidItemsRequest(Guid OrderId, string? Justification, List<VoidItemLine> Items)`.
  - Agregar `OnPostVoidItemsPreviewAsync`, que devuelve `{ success, error, allSelected, before, after, theoreticalRefund, items }`, y `OnPostVoidItemsAsync`, que devuelve `{ success, error }`.
  - En `OnPostUpdateAsync`, si `existing.Status == Voided`, devolver `{ error = "No se puede modificar un ítem anulado." }`.
- [ ] T016 [US1] En `MariCamiStore/Pages/Orders/Items.cshtml`:
  - Cambiar la columna derecha del header a `div.btn-toolbar.justify-content-end`, con "Volver" y, si `order.Status == OrderStatus.Active.Key`, `<button id="btn-void-items" class="btn btn-danger btn-sm ml-1"><i class="fas fa-ban"></i> Anular</button>`.
  - Agregar encima de `#items-table-container` un `#void-select-bar` oculto, con "Anular seleccionados" (`#btn-void-selected`, disabled) y "Cancelar" (`#btn-void-cancel`).
  - Agregar el modal `#modalVoidItems` (`modal-lg`) con: el texto "¿Está seguro que desea anular los ítems seleccionados?"; una tabla `#void-items-table` (Descripción, Cliente, Acordado, Monto a devolver por el proveedor); un contenedor `#void-comparison` (lo llena US2); `#void-justification` (textarea, maxlength 1000, requerido); `#void-all-warning` (alert-warning oculto: "Se seleccionaron todos los ítems; use la anulación de la orden."); `#void-error` (alert-danger oculto); y los botones "Cancelar" y `#btn-void-confirm` "Continuar con la anulación" (btn-danger).
  - Agregar `<style>.table-voided, .table-voided > td { background-color: #f8d7da; }</style>`.
- [ ] T017 [US1] En `MariCamiStore/wwwroot/js/pages/orders/items.js`, modo selección y render de anulados:
  - Variable `voidSelectMode`. `#btn-void-items` la activa, muestra `#void-select-bar` y re-renderiza; `#btn-void-cancel` la desactiva y re-renderiza.
  - En `renderItemsTable`, si `voidSelectMode`, agregar al principio la columna de selección (encabezado vacío): `input.void-chk[data-item-id]` solo en las filas `status === 'Active'`. El `change` habilita `#btn-void-selected` cuando hay ≥1 marcado.
  - Filas `status === 'Voided'`: `addClass('table-voided')`, un `<span class="badge badge-danger ml-1">Anulado</span>` después de la descripción, sin Reasignar ni checkbox Recibido (celda vacía si `isDelivering`). No suman en `groupSubtotal` ni en `grandTotal`.
  - `#item-count-badge` cuenta solo los Active.
- [ ] T018 [US1] En `MariCamiStore/wwwroot/js/pages/orders/items.js`, el flujo del modal:
  - `#btn-void-selected` llama a `ajaxPost('VoidItemsPreview', { orderId, itemIds })`. Si hay error, `alert`. Si no, guardar `voidPreview` y llenar `#void-items-table` con un `input.void-refund` (type number, min 0, step 0.01, value `defaultRefund`); mostrar u ocultar `#void-all-warning` según `allSelected`; limpiar la justificación y `#void-error`; y abrir el modal.
  - `updateVoidConfirmState()` deshabilita `#btn-void-confirm` si `allSelected`, si la justificación está vacía (trim) o si algún monto es vacío, NaN o < 0. Se llama en el `input` de `.void-refund` y de `#void-justification`.
  - `#btn-void-confirm` hace POST `VoidItems` con `{ orderId, justification, items: [{ itemId, refundAmount }] }`. Si tiene éxito, `location.reload()`; si falla, muestra el error en `#void-error`.

**Checkpoint**: US1 se puede verificar con el quickstart §1, §3 y §5. La CxP ya resta (Phase 2).

---

## Phase 4: User Story 2 - Comparativa financiera antes/después y ajuste (P1)

**Goal**: el modal muestra los 7 totales Antes/Después, el Ajuste, el Teórico y la Diferencia, actualizados al editar los montos.

**Independent Test**: quickstart §2.

- [ ] T019 [US2] En `MariCamiStore/Pages/Orders/Items.cshtml`, dentro de `#void-comparison`, agregar:
  - una tabla `table-sm` con las columnas Concepto / Antes / Después y las filas Precio Acordado Total, SubTotal, Impuestos, Envío a CR, Total a Proveedor, Total Orden y Ganancia Est.;
  - un bloque con `#void-adjustment` (Ajuste financiero), `#void-theoretical` (Total a Proveedor Antes − Después) y `#void-difference` (Ajuste − Teórico).
- [ ] T020 [US2] En `MariCamiStore/wwwroot/js/pages/orders/items.js`:
  - `renderVoidComparison(before, after)` llena la tabla con `formatMoney`, usando `localCurrencySign` para Acordado y Ganancia, y `orderCurrencySign` para el resto (como `renderHeader`). Se llama al abrir el modal.
  - `recalcVoidAdjustment()` suma los `.void-refund` válidos con `round2`, muestra el Ajuste, el Teórico (`voidPreview.theoreticalRefund`) y la Diferencia con `orderCurrencySign`. Se llama al abrir el modal y en el `input` de `.void-refund`, junto con `updateVoidConfirmState()`.

**Checkpoint**: quickstart §2 completo.

---

## Phase 5: User Story 3 - Consistencia de CxP, Saldos y Posición (P2)

**Goal**: el movimiento se ve en CxP con su etiqueta y como reducción; los escenarios A, B y C cuadran.

**Independent Test**: quickstart §3 (parte CxP), §4 y §5.6–5.7.

- [ ] T021 [P] [US3] En `MariCamiStore/wwwroot/js/pages/cxp/index.js` agregar `'AnulacionItems': 'Anulación de ítems'` a `TYPE_LABELS`. El monto ya llega con signo (T008), así que `formatSignedMoneyHtml` lo pinta en rojo con "−"
- [ ] T022 [US3] Revisar en `MariCamiStore/Services/CxPService.cs` que `GetClosePreviewAsync` y `ClosePeriodAsync` calculen el saldo anterior solo a partir de `GetPeriodIndicatorsAsync` (heredan el signo de T007) y que `DeleteEntryAsync` no necesite cambios (Clarifications: se permite eliminar)

**Checkpoint**: quickstart §4 cuadra en los 3 escenarios.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T023 Correr `dotnet build` en `MariCamiStore/`: cero errores y ningún warning nuevo respecto a T001
- [ ] T024 Ejecutar el [quickstart.md](quickstart.md) completo (§1–§5) en local y verificar SC-005 en la BD: `SELECT COUNT(*) FROM dbo.Transactions WHERE TransactionAmount < 0 AND TransactionDescription LIKE N'Anulación de ítem%'` y `SELECT COUNT(*) FROM dbo.CxPEntries WHERE Type = 'AnulacionItems' AND Amount < 0` deben dar 0
- [ ] T025 Generar el script idempotente para producción con `dotnet ef migrations script --idempotent` (desde la migración anterior, `SupplierDefaultsAndRealPriceInLocal`, hasta `AddOrderItemStatus`) y adjuntarlo en la descripción del PR, con la nota de ejecutarlo antes del deploy
- [ ] T026 [P] En `brainstorm/19-anulacion-items-orden.md` cambiar el `Status` a `spec-created (specs/020-anulacion-items-orden)`, y en `brainstorm/00-overview.md` poner `specs/020-anulacion-items-orden` en la fila 19

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2 → US1 → US2**. US2 extiende el modal y el JS de US1 (mismos archivos `Items.cshtml` e `items.js`), así que va después.
- **US3** depende solo de la Phase 2. T021 se puede hacer en paralelo con US1. La verificación de los escenarios (quickstart §4) necesita US1.
- Dentro de la Phase 2: T002, T003 y T004 son [P] (archivos distintos). T005 depende de T003; T006 de T002–T005; T007 y T008 de T004 (y entre ellas son paralelas); T010 de T009.
- Dentro de US1: T011 → T012 → T013 → T014 (mismo archivo, en secuencia) → T015 → T016 → T017 → T018.

## Parallel Example

```text
Phase 2: T002 OrderItemStatus.cs | T003 OrderItem.cs | T004 CxPEntryType.cs
Después de T004: T007 CxPService.cs | T008 CxP/Index.cshtml.cs
Con US1 en curso: T021 cxp/index.js | T026 brainstorm docs
```

## Implementation Strategy

1. **MVP** = Phase 1 + Phase 2 + US1. La anulación funciona de punta a punta y es contablemente correcta (la CxP resta desde la Phase 2); el modal todavía no tiene la comparativa.
2. **+US2**: la comparativa y el ajuste en vivo completan lo que pide el ticket.
3. **+US3**: la etiqueta en CxP y la verificación de los escenarios A, B y C.
4. Polish: build, quickstart completo, script de migración y cierre del brainstorm.
