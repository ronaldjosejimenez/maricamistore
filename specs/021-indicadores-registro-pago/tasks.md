# Tasks: Indicadores en el registro de pago y estado Recibido de los ítems

**Input**: Design documents from `specs/021-indicadores-registro-pago/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-handlers.md, quickstart.md

**Tests**: no hay proyecto de pruebas automatizadas y la spec no las pide. La verificación es `dotnet build` más el [quickstart.md](quickstart.md).

**Organization**:
- **Phase 2** agrega el valor `Received`, cambia todos los filtros de "cuenta en la orden" a la lista positiva `OrderItemStatus.CountedKeys` (#48) y crea la migración de datos. Los filtros van antes que esa migración porque, en cuanto existan ítems `Received`, el código viejo los excluiría de totales y anulaciones.
- **US1** cambia el checkbox para que escriba el estado y elimina `IsReceived`.
- **US2** verifica la consistencia y ajusta comentarios.
- **US3** agrega las etiquetas en Saldos de clientes.

## Phase 1: Setup

- [X] T001 Correr `dotnet build` en `MariCamiStore/` antes de los cambios y anotar los warnings que ya existen
- [X] T002 En la BD local, **antes** de cualquier migración, anotar lo que pide el [quickstart.md](quickstart.md) §0:
  - el conteo `IsReceived = 1 AND Status <> 'Voided'` (N);
  - los totales de las órdenes;
  - los saldos de Saldos de clientes, con y sin "Ver saldo completo";
  - el conteo de ítems de las órdenes Entregando en el listado.

---

## Phase 2: Foundational (bloquea todas las historias)

**Purpose**: nuevo estado, la regla "no anulado" (research R1, R2) y la migración de datos (R4).

- [X] T003 [P] En `MariCamiStore/Model/OrderItemStatus.cs` (además, `public static readonly string[] CountedKeys = { Active.Key, Received.Key };` con comentario XML):
  - agregar `public static OrderItemStatus Received = new OrderItemStatus(nameof(Received), "Recibido");` entre `Active` y `Voided`, con comentario XML ("The item was received by the customer; it still counts towards the order.");
  - cambiar `List()` a `new[] { Active, Received, Voided }`;
  - ajustar el comentario de `Active` para que diga "pendiente de recibir" ("The item is valid, counts towards the order and is pending reception.").
- [X] T004 [P] En `MariCamiStore/Services/OrderService.cs` cambiar `i.Status == OrderItemStatus.Active.Key` por `OrderItemStatus.CountedKeys.Contains(i.Status)` en:
  - `GetOrderItemCountsAsync`;
  - `ComputeOrderTotals` (la lista `list`);
  - `TransitionOrderAsync`, rama `ToStatus == Voided` (ajustar el comentario: "Items voided earlier already have their Void transaction; Active and Received items are voided now");
  - `ReasignarItemAsync` (`otherTotal`);
  - `PreviewVoidItemsAsync` y `VoidItemsAsync` (la lista `active`, que pasa a llamarse `counted`, incluido su uso en `selected.Count == ...`).

  Confirmar con `grep "OrderItemStatus.Active.Key"` que solo queden el default del modelo y el de la configuración. T007 agrega uno más, en `SetItemReceivedAsync`.
- [X] T005 Con el modelo **todavía** con `IsReceived`, correr `dotnet ef migrations add MigrateIsReceivedToStatus` desde `MariCamiStore/`. Debe salir vacía. Completar `Up` y `Down` en `MariCamiStore/Infrastructure/Persistance/Migrations/<ts>_MigrateIsReceivedToStatus.cs`, con el SQL de research R4 y un comentario al estilo de `20261019231103_RemoveDeliveredOrderStatus.cs`: migración solo de datos (spec 021), SQL crudo que se salta el filtro de organización, no toca `UpdatedAt` y su Down es reversible mientras exista la columna.
  - Up: `UPDATE dbo.OrderItems SET Status = 'Received' WHERE IsReceived = 1 AND Status <> 'Voided';`
  - Down: `UPDATE dbo.OrderItems SET IsReceived = 1, Status = 'Active' WHERE Status = 'Received';`

  Correr `dotnet ef database update` en local.

**Checkpoint**: compila. `SELECT COUNT(*) FROM dbo.OrderItems WHERE Status = 'Received'` da N (T002). Los totales y conteos siguen iguales.

---

## Phase 3: User Story 1 - Marcar ítems como recibidos mediante su estado (P1) 🎯 MVP

**Goal**: el checkbox escribe `Status` y `IsReceived` desaparece del código y de la BD.

**Independent Test**: quickstart §0 (parte de migración) y §1.

- [X] T006 [US1] En `MariCamiStore/Services/IOrderService.cs`:
  - quitar `bool IsReceived` de `OrderItemWithCustomerDto`;
  - reemplazar `Task<(bool Success, string? Error)> ToggleIsReceivedAsync(Guid itemId, bool isReceived);` por `SetItemReceivedAsync(Guid itemId, bool received)`, con comentario XML ("Marks the item as Received or back to Active; only for Delivering orders, never for voided items.").
- [X] T007 [US1] En `MariCamiStore/Services/OrderService.cs` (después de T006):
  - en `GetOrderItemsWithCustomerAsync`, quitar `i.IsReceived` de la construcción del DTO;
  - renombrar `ToggleIsReceivedAsync` a `SetItemReceivedAsync(Guid itemId, bool received)` con los mismos guards y mensajes, y reemplazar `item.IsReceived = isReceived;` por `item.Status = received ? OrderItemStatus.Received.Key : OrderItemStatus.Active.Key;`.
- [X] T008 [US1] En `MariCamiStore/Pages/Orders/Items.cshtml.cs`, `OnPostToggleReceivedAsync` pasa a llamar a `orderService.SetItemReceivedAsync(request.ItemId, request.IsReceived)`. `ToggleReceivedRequest` no cambia (contrato en contracts/ui-handlers.md).
- [X] T009 [P] [US1] En `MariCamiStore/wwwroot/js/pages/orders/items.js`:
  - en `renderItemsTable`, cambiar `else if (item.isReceived)` por `else if (item.status === 'Received')` (fila `table-success`) y `if (item.isReceived) $chk.prop('checked', true);` por `if (item.status === 'Received') ...`;
  - cambiar el comentario `// IsReceived checkbox handler (delegated)` por `// Received checkbox handler (delegated): toggles the item status Active <-> Received`;
  - en el éxito del handler, actualizar `status` del ítem en `currentItems` (`'Received'` o `'Active'`) para que un re-render no lo revierta.
- [X] T010 [US1] Quitar `public bool IsReceived { get; set; }` de `MariCamiStore/Model/OrderItem.cs`, y el bloque `builder.Property(oi => oi.IsReceived)...` de `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderItemEntityTypeConfiguration.cs`. Confirmar con `grep -rn "IsReceived\|isReceived"` (sin contar `Migrations/`) que solo queden `ToggleReceivedRequest` y el handler JS, que son el contrato que se mantiene.
- [X] T011 [US1] Correr `dotnet ef migrations add DropOrderItemIsReceived` desde `MariCamiStore/`. Revisar que `Up` solo tenga `DropColumn("IsReceived", "OrderItems")` y que `Down` lo vuelva a agregar como `bit NOT NULL DEFAULT 0`. Correr `dotnet ef database update`.

**Checkpoint**: quickstart §0 (la columna ya no existe; N ítems Received) y §1 completos.

---

## Phase 4: User Story 2 - Los ítems recibidos siguen contando en la orden (P1)

**Goal**: los totales, conteos, Reasignar y la anulación de orden incluyen a los Received. El código ya está desde T004; esta fase verifica y deja la documentación coherente.

**Independent Test**: quickstart §0 (totales y conteos idénticos) y §2.

- [X] T012 [P] [US2] En `MariCamiStore/Services/IOrderService.cs`, cambiar el XML de `OrderTotals` de "computed from its active items" a "computed from its non-voided (Active and Received) items". En `MariCamiStore/Services/OrderService.cs`, cambiar el `<param name="items">` de `ComputeOrderTotals` para que diga que se ignoran los Voided y se cuentan los Active y los Received.
- [X] T013 [US2] En `MariCamiStore/wwwroot/js/pages/orders/items.js` definir `var COUNTED_STATUSES = ['Active', 'Received'];` y `function isCounted(item)`, y usarlos en el conteo `#item-count-badge` y en los subtotales (`groupSubtotal`), en lugar de `status !== 'Voided'` / `!isVoided`. Revisar también `MariCamiStore/wwwroot/js/pages/orders/index.js`. Las comparaciones con `'Voided'` que son propias de una fila anulada (clase, badge, sin botones) se mantienen.
- [ ] T014 [US2] Ejecutar el quickstart §2 (totales sin cambio al marcar; Reasignar incluye a los Recibidos; anular una orden Entregando genera el Void de A, B y C).

**Checkpoint**: SC-002 y SC-003 verificados.

---

## Phase 5: User Story 3 - Etiquetas informativas en Saldos de clientes (P2)

**Goal**: se quita "Saldo pendiente" y se agregan "Productos en camino (n)", "Sugerir adelanto" y "Entregado – por cobrar", calculadas después del saldo.

**Independent Test**: quickstart §3 (casos a–h).

- [ ] T015 [US3] En `MariCamiStore/Services/IPaymentService.cs`, extender `SaldoReportRow` con `int ItemsInTransit, bool SuggestAdvance, bool DeliveredToCollect` al final, y documentar en XML las reglas de data-model.md.
- [ ] T016 [US3] En `MariCamiStore/Services/PaymentService.cs`, `GetSaldosReportAsync` (después de T015, research R5):
  - dejar intacto el cálculo de `rows`;
  - luego `debtorIds = rows.Where(r => r.Balance > 0)`;
  - si hay deudores, consultar `context.OrderItems.IgnoreQueryFilters()` filtrando `debtorIds.Contains(i.CustomerId)`, `OrderItemStatus.CountedKeys.Contains(i.Status)`, `i.Order.Status` Active o Delivering y `organizationId == null || i.Order.OrganizationId == organizationId`, agrupando por `CustomerId` en `{ InTransit = Count(Active), Received = Count(Received), AgreedSum = Sum(AgreedPriceInLocal) }`, a un diccionario;
  - en el `Select` final, derivar `ItemsInTransit`, `DeliveredToCollect = Received > 0 && InTransit == 0` y `SuggestAdvance = !DeliveredToCollect && (InTransit + Received) > 0 && AgreedSum >= Balance`. Usar `0` / `false` cuando `Balance <= 0` o no hay entrada;
  - agregar un comentario que explique que las etiquetas se evalúan después del saldo, solo sobre los deudores (FR-012).
- [ ] T017 [P] [US3] En `MariCamiStore/wwwroot/js/pages/payments/index.js`, `renderSaldos`, reemplazar el ternario `badge`:
  - `balance < 0` → `<span class="badge badge-success ml-1">Crédito a favor</span>`;
  - si no, concatenar `itemsInTransit > 0` → `badge-primary` "Productos en camino (n)", `suggestAdvance` → `badge-warning` "Sugerir adelanto" y `deliveredToCollect` → `badge-danger` "Entregado – por cobrar".

  Eliminar "Saldo pendiente". Mantener la fila `table-success` para el saldo a favor.
- [ ] T018 [US3] Ejecutar el quickstart §3, casos a–h, con y sin "Ver saldo completo", y medir la carga de Saldos (< 2 s, SC-006).

**Checkpoint**: quickstart §3 completo.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T019 Correr `dotnet build` en `MariCamiStore/`: cero errores y ningún warning nuevo respecto a T001
- [ ] T020 Generar el script de producción desde `MariCamiStore/` con `dotnet ef migrations script AddOrderItemStatus DropOrderItemIsReceived --idempotent -o ../specs/021-indicadores-registro-pago/migration.sql`. Incluye `MigrateIsReceivedToStatus` y `DropOrderItemIsReceived`. Adjuntarlo en la descripción del PR con la nota "ejecutar antes del deploy"
- [ ] T021 Probar el rollback en local: `dotnet ef database update AddOrderItemStatus` debe recrear `IsReceived` con los N ítems en 1 y `Status` en Active. Volver a `dotnet ef database update`
- [ ] T022 Ejecutar el [quickstart.md](quickstart.md) completo (§0–§3)
- [ ] T023 [P] El PR debe decir "Closes #44, closes #48"

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2 → US1 → US2 → US3 → Polish.**
- **Phase 2:**
  - T003 y T004 son independientes entre sí (T004 solo usa `Voided`). T003 tiene que estar antes de T007 y T016, que usan `Received`.
  - T004 va **antes** de T005: los filtros nuevos tienen que estar antes de que existan datos `Received`.
  - T005 corre con `IsReceived` todavía en el modelo.
- **US1:**
  - T006 → T007 → T008 (firma del servicio).
  - T009 es paralela.
  - T010 después de T007–T009 (no debe quedar código que use la propiedad).
  - T011 después de T010.
- **US2** depende de T004 (código) y de US1 (para verificar con el checkbox real).
- **US3** depende de la Phase 2 (`Received` y su semántica). T015 → T016; T017 es paralela a T016.

## Parallel Example

```text
Phase 2: T003 OrderItemStatus.cs | T004 OrderService.cs (luego T005)
US1: T009 items.js | T006→T007→T008 (servicio y página)
US3: T017 payments/index.js | T015→T016 (PaymentService)
```

## Implementation Strategy

1. **MVP** = Phase 1 + Phase 2 + US1. El estado `Received` reemplaza a `IsReceived` de punta a punta y los montos son correctos desde la Phase 2.
2. **+US2**: verificación formal de la consistencia (#48).
3. **+US3**: las etiquetas en Saldos de clientes, que son el valor visible del #44.
4. Polish: build, script idempotente, rollback probado y quickstart completo.
