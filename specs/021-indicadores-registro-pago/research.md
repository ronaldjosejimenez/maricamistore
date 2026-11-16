# Research — 021

## R1. Cómo expresar "ítem no anulado"
- **Decision**: agregar `OrderItemStatus.Received = new(nameof(Received), "Recibido")` e incluirlo en `List()`. Se define **en positivo** una lista de los estados que cuentan en la orden: `OrderItemStatus.CountedKeys = { Active.Key, Received.Key }`. Toda condición que hoy usa `i.Status == OrderItemStatus.Active.Key` con el sentido de "cuenta en la orden" pasa a `OrderItemStatus.CountedKeys.Contains(i.Status)`. Donde el sentido es otro (p. ej. "pendiente de recibir") se compara contra el estado concreto (`Active`).
- **Rationale**: una lista positiva obliga a decidir, para cada estado nuevo, si cuenta o no. Con `!= Voided` un estado futuro quedaría incluido sin que nadie lo decida. EF Core traduce `array.Contains(i.Status)` a `IN (...)`, así que sirve dentro de `IQueryable`. Está en un solo lugar para no repetir la lista en cada consulta.
- **Alternatives**:
  - `Status != Voided` (decisión original). Se descarta porque es una definición en negativo: un estado nuevo se contaría por omisión.
  - Listar los estados en cada consulta. Se descarta por repetirse en 6 lugares; se mantiene explícito donde la regla es distinta (etiquetas).
  - Una propiedad calculada `IsCounted`. Se descarta porque no se traduce a SQL.

## R2. Inventario de los cambios del issue #48
| # | Lugar | Cambio |
|---|-------|--------|
| 1 | `OrderService.TransitionOrderAsync` (→ Voided) | `Where(CountedKeys.Contains(Status))`: genera un Void para Active **y** Received |
| 2 | `OrderService.ComputeOrderTotals` | `Where(CountedKeys.Contains(Status))` |
| 3 | `OrderService.ReasignarItemAsync` (`otherTotal`) | `CountedKeys.Contains(Status)` |
| 4 | `OrderService.GetOrderItemCountsAsync` | `CountedKeys.Contains(Status)` |
| 5 | `OrderService.PreviewVoidItemsAsync` / `VoidItemsAsync` (lista `active`, ahora `counted`) | `CountedKeys.Contains(Status)`. En órdenes Activas no hay Received, así que no cambia el comportamiento; es solo protección |
| 6 | `OrderService.ToggleIsReceivedAsync` | Se renombra a `SetItemReceivedAsync`; escribe `Status` (ver R3) |
| 7 | `items.js` (filas y checkbox) | `item.status === 'Received'` en lugar de `item.isReceived` |
| 8 | `OrderItemStatus.List()` | Incluye `Received` |
| 9 | Migración de datos | R4 |

`TransitionOrderAsync` (→ Active) genera un Charge para **todos** los ítems de la orden. Eso está bien así: en una orden Pendiente no hay ni Voided ni Received. `items.js` ya usa `status !== 'Voided'` para el conteo, Reasignar y los subtotales.

## R3. El checkbox "Recibido"
- **Decision**: el handler `ToggleReceived` conserva su contrato (`{ itemId, isReceived }`); `isReceived` pasa a significar "estado deseado". El servicio pasa a llamarse `SetItemReceivedAsync(itemId, received)` y aplica `Status = received ? Received : Active`. Mantiene los guards actuales: el ítem existe, no está Voided y la orden está en Delivering.
- **Rationale**: la UI no cambia (FR-002). Es idempotente: marcar dos veces deja el ítem en Received.
- **Alternatives**: un handler nuevo `SetItemStatus` con un string de estado. Se descarta porque expone más superficie de la necesaria.

## R4. Migraciones
- **Decision**: dos migraciones EF, en este orden.
  1. **`MigrateIsReceivedToStatus`**: se crea **antes** de quitar la propiedad, así que el diff del modelo queda vacío y solo lleva SQL manual.
     - Up: `UPDATE dbo.OrderItems SET Status = 'Received' WHERE IsReceived = 1 AND Status <> 'Voided';`
     - Down: `UPDATE dbo.OrderItems SET IsReceived = 1, Status = 'Active' WHERE Status = 'Received';`
     - No toca `UpdatedAt`, porque es una corrección de datos y no una acción del usuario.
     - El SQL crudo se salta el filtro de organización, así que aplica a todas.
  2. **`DropOrderItemIsReceived`**: se genera después de quitar `IsReceived` del modelo y de la configuración. Es un `DropColumn`; el Down lo vuelve a agregar con default `false`.
- **Rationale**: el issue pide dos migraciones (datos y esquema). Con ese orden el rollback también funciona: el Down de la 2 recrea la columna y el Down de la 1 repuebla `IsReceived` a partir de `Received`.
- **Alternatives**: una sola migración con `Sql` + `DropColumn`. Se descarta porque el issue pide explícitamente dos.
- **Producción**: el pipeline no aplica migraciones. Antes del deploy se ejecuta `dotnet ef migrations script --idempotent`, igual que en la 018 y la 020. El código nuevo no conoce `IsReceived`, así que el script se puede correr antes o después de desplegar sin romper EF. Aun así, se recomienda correrlo antes, para que no haya una ventana en la que se marquen ítems con el campo viejo.

## R5. Dónde y cómo calcular las etiquetas
- **Decision**: en `PaymentService.GetSaldosReportAsync`, **después** de calcular `rows` (el saldo sigue igual, FR-012):
  1. `debtorIds` = los clientes con `Balance > 0`.
  2. Si hay alguno, se hace **una** consulta agrupada:
     ```
     context.OrderItems.IgnoreQueryFilters()
       .Where(i => debtorIds.Contains(i.CustomerId)
                && OrderItemStatus.CountedKeys.Contains(i.Status)
                && (i.Order.Status == Active || i.Order.Status == Delivering)
                && (organizationId == null || i.Order.OrganizationId == organizationId))
       .GroupBy(i => i.CustomerId)
       .Select(g => new { CustomerId, InTransit = g.Count(Active), Sum = g.Sum(AgreedPriceInLocal) })
     ```
  3. Por cliente:
     - `deliveredToCollect = Balance > 0 && InTransit == 0` (un deudor sin ítems en curso también cuenta);
     - `suggestAdvance = InTransit > 0 && Balance >= Sum`;
     - `itemsInTransit = InTransit`.
- **Rationale**:
  - `IgnoreQueryFilters` + `organizationId` replica exactamente el alcance que usa el saldo, tanto con "Ver saldo completo" como sin él (FR-013).
  - Se hace una sola consulta extra, y solo sobre los deudores.
  - Las reglas de negocio quedan en el servidor y la vista solo pinta.
- **Alternatives**:
  - Calcular en JS. Se descarta porque llevaría ítems al cliente y dejaría reglas de negocio en la UI.
  - Una subconsulta dentro del `GroupBy` de transacciones. Se descarta porque mezcla el cálculo del saldo con el de las etiquetas, y FR-012 pide separarlos.
- `suggestAdvance` exige al menos un ítem en esas órdenes. Sin ítems la suma es 0 y, como `Balance > 0`, la comparación `Balance >= Sum` sería verdadera: la cláusula `InTransit > 0` es necesaria para no marcar a los clientes sin nada pendiente.

## R6. Presentación
- **Decision**: `SaldoReportRow` gana `int ItemsInTransit`, `bool SuggestAdvance` y `bool DeliveredToCollect`. `renderSaldos` (en `payments/index.js`) pinta:
  - "Crédito a favor" con `badge-success`, igual que hoy;
  - "Productos en camino (n)" con `badge-primary` (azul);
  - "Sugerir adelanto" con `badge-warning` (amarillo);
  - "Entregado – por cobrar" con `badge-danger` (rojo).

  "Saldo pendiente" se elimina. Las etiquetas van una tras otra junto al nombre, con `ml-1`.
- **Rationale**: en Bootstrap 4 / AdminLTE 3, `badge-info` es cian, así que el azul que pidió el usuario corresponde a `badge-primary`.
- **Alternatives**: una columna "Indicadores" aparte. Se descarta para no cambiar la estructura de la tabla ni el total.
