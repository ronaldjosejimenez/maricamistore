# Research — 020 Anulación parcial de ítems

## R1. Estado del ítem
- **Decision**: nueva columna `OrderItems.Status nvarchar(20) NOT NULL DEFAULT 'Active'`, mapeada como `string` (mismo patrón que `Order.Status`). `OrderItemStatus` queda con solo `Active` y `Voided`; se eliminan `Pending`, `Checked` y `Delivered`. La migración `AddOrderItemStatus` deja todos los ítems existentes en `Active` gracias al default.
- **Rationale**: el enum nunca se usó (no hay referencias fuera de su archivo), así que eliminar valores no rompe nada. Usar un `string` con `Enumeration` es la convención del proyecto.
- **Alternatives**: un `bool IsVoided` (más simple, pero el ticket pide estados y deja la puerta a futuros); `Pending/Active/Voided` (descartado en el brainstorm 19 porque exige sincronizarse con la orden).

## R2. Tipo CxP que resta
- **Decision**: nueva constante `CxPEntryType.AnulacionItems = "AnulacionItems"` y un helper `CxPEntryType.SignedAmount(type, amount)`, que devuelve `-amount` para los tipos que restan (por ahora solo `AnulacionItems`) y `amount` para el resto. Se aplica en los dos puntos que agregan montos:
  1. `CxPService.GetPeriodIndicatorsAsync` (`group.Sum(...)`). La vista previa y el cierre de mes (y por lo tanto el saldo anterior) reutilizan esos indicadores, así que heredan el signo.
  2. `Pages/CxP/Index.cshtml.cs` `OnGetEntriesAsync` (`total = g.Sum(...)`).
  Además, `OnGetEntriesAsync` expone por fila `Amount` con signo, para que el JS existente (`formatSignedMoneyHtml`) la pinte en rojo con "−". En la base de datos el monto sigue siendo positivo.
- **Rationale**: el usuario no quiere montos negativos guardados. Con un solo helper evitamos repetir la regla de signo; el inventario de `context.CxPEntries` muestra que no hay otros lectores que sumen.
- **Alternatives**: guardar negativo, como `ReversoPaquete` (rechazado por el usuario); agregar una columna `Sign` en `CxPEntries` (más esquema sin necesidad).
- **Note**: `ReversoPaquete` sigue guardándose negativo (fuera de alcance). El helper no lo toca.

## R3. Cálculo del Antes/Después
- **Decision**: el cálculo de totales se extrae a una función pura `OrderService.ComputeOrderTotals(Order, IEnumerable<OrderItem>) → OrderTotals`, y `ApplyOrderCalculationSuite` pasa a usarla. El modal pide al servidor una **vista previa** (`VoidItemsPreview`) con los ids seleccionados. El servidor devuelve Antes (los totales guardados de la orden) y Después (`ComputeOrderTotals` sobre los ítems Activos que no están seleccionados), más la diferencia teórica de Total a Proveedor y el default por ítem. La confirmación persiste con la misma función, así que Después = lo persistido (FR-014).
- **Rationale**: una sola fuente de verdad. El cliente `recalcOrderHeader` replica las fórmulas, pero si se dependiera de él podría haber diferencias de redondeo entre lo mostrado y lo guardado.
- **Alternatives**: calcular en el JS (ya existe `recalcOrderHeader`); descartado por FR-014.
- El Ajuste financiero y su diferencia se recalculan en el cliente al editar montos (es solo una suma); no necesitan el servidor.
- **Posición de CxP después de anular (FR-008a)**: la Posición depende de los indicadores globales del período, así que el servidor devuelve en la vista previa `position` = `{ current, saldosDelta, shippingDelta, refundRate }`: `current` sale de `GetPeriodIndicatorsAsync` del período abierto; `saldosDelta` = −Σ precio acordado de los seleccionados; `shippingDelta` = (pendiente antes − pendiente después) × tipo de cambio, con pendiente = max(0, envío a CR de la orden − paquetes registrados); `refundRate` = 1 si la orden está en moneda local, si no el tipo de cambio del período. El cliente calcula `current + saldosDelta + shippingDelta + ajuste × refundRate`, porque el ajuste es lo único que el usuario edita. Con tipo de cambio 0 el indicador ignora deuda y envío, así que `shippingDelta` y `refundRate` son 0. Sin período abierto, `position` es null.

## R4. Atomicidad y concurrencia
- **Decision**: `OrderService.VoidItemsAsync` hace todas las validaciones y después agrega al `context` el cambio de estado de los ítems, las transacciones `Void`, los totales de la orden y la entrada `CxPEntry`, con **un solo** `SaveChangesAsync` (el mismo patrón que `OrderPackageService.DeleteAsync`). El período abierto se obtiene con `cxpService.GetOpenPeriodAsync()` antes de agregar cambios. No se usa `CreateAutoEntryAsync`, porque ese guarda por su cuenta.
- **Rationale**: FR-018 (todo o nada). EF envuelve un solo `SaveChanges` en una transacción.
- **Concurrencia**: no hay rowversion. La validación de "ítems Activos y orden Activa" se repite dentro de la operación, y para el volumen actual (un solo operador) se acepta last-writer-wins. Una solicitud repetida con los mismos ids falla, porque los ítems ya están `Voided`.

## R5. Transacciones de anulación
- **Decision**: igual que `BuildTransaction` y `ReasignarItemAsync`: tipo `Void`, `Source = OrderItem`, `SourceId = item.Id`, `CustomerId` = el cliente actual, monto `AgreedPriceInLocal`, `CurrencyId = Configuration.LocalCurrencyId`, fecha en hora de Costa Rica (`Central America Standard Time`), estado `Applied`. Descripción: `Anulación de ítem – {desc≤100} – Orden {NameOfOrder} – {justificación}`, truncada a la longitud de la columna.
- Se verifica la longitud máxima de `TransactionDescription` y de `CxPEntry.Reference` en las configuraciones de EF y se trunca, como hace `OrderPackageService.Truncate`.

## R6. Dónde se excluyen los ítems Voided (FR-019 a FR-022)
| Punto | Cambio |
|---|---|
| `ApplyOrderCalculationSuite` / `RecalcItemsOnOrderSaveAsync` | Filtrar `Status == Active` (en Pending no hay Voided, pero queda blindado) |
| `GetOrderItemCountsAsync` | Contar solo los Active |
| `ReasignarItemAsync` | Rechazar un ítem Voided; `otherTotal` solo suma los Active |
| `ToggleIsReceivedAsync` | Rechazar un ítem Voided |
| `Items.cshtml.cs OnPostUpdateAsync` | Rechazar un ítem Voided (hoy el servidor no valida el estado) |
| `DeleteOrderItemAsync` | Sin cambio (solo en Pending, donde no hay Voided) |
| `TransitionOrderAsync` → Voided | Generar `Void` solo para los ítems Active |
| `items.js renderItemsTable` | Fila `table-voided` (rojo pastel) + badge "Anulado"; sin Reasignar ni checkbox Recibido; excluida de los subtotales, del total y del badge de conteo |

## R7. UI de selección
- **Decision**: el header de la página se convierte en una toolbar (`div.btn-toolbar`) con "Volver" y, si `order.Status == Active`, `#btn-void-items` (`btn-danger`). En modo selección se agrega al principio de la tabla una columna de checkboxes (solo para filas Active) y aparece una barra con "Anular seleccionados" (deshabilitado si hay 0 marcados) y "Cancelar". Si la selección cubre todos los Active, "Anular seleccionados" sigue habilitado, pero el modal muestra el aviso y deshabilita "Continuar" (FR-009). El servidor también lo rechaza.
- El color rojo pastel claro es una clase CSS propia `.table-voided { background-color: #f8d7da; }` (el tono de `alert-danger` de Bootstrap 4).
