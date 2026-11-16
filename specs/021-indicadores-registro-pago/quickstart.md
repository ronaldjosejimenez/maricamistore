# Quickstart / Manual Verification — 021

Prerrequisitos: `dotnet build` sin errores y las migraciones `MigrateIsReceivedToStatus` y `DropOrderItemIsReceived` aplicadas.

> **Producción:** el pipeline no aplica migraciones. Se ejecuta el script idempotente (`dotnet ef migrations script --idempotent`) antes de desplegar.

> **Verificación de registro:** el proyecto no tiene pruebas automatizadas. Este quickstart es la verificación de la feature.

## 0. Antes de migrar (SC-001, SC-002, SC-004)
Sobre una copia de la base, antes de aplicar las migraciones, anotar:
```sql
SELECT COUNT(*) FROM dbo.OrderItems WHERE IsReceived = 1 AND Status <> 'Voided';   -- N
SELECT Id, TotalAgreedPriceInLocal, TotalToPayToSupplier, TotalOfTheOrder, EstimatedProfitInLocal FROM dbo.Orders;
```
Anotar también los saldos de Saldos de clientes, con y sin "Ver saldo completo", y el conteo de ítems del listado de órdenes en Entregando.

Después de migrar:
- `SELECT COUNT(*) FROM dbo.OrderItems WHERE Status = 'Received'` debe dar **N**, y la columna `IsReceived` ya no existe.
- Los totales de las órdenes, los saldos y los conteos son **idénticos** a los anotados.
- En una orden Entregando, los ítems que antes estaban marcados siguen con el checkbox marcado y la fila en verde.

## 1. Checkbox Recibido (US1)
1. En una orden Entregando, marcar el ítem A. La fila se pone verde. Recargar: A sigue marcado (`Status = Received`).
2. Desmarcar A. La fila vuelve a la normalidad. Recargar: A queda desmarcado (`Status = Active`).
3. Por DevTools, hacer POST `ToggleReceived` sobre un ítem de una orden Activa. Debe responder "Solo se puede marcar recepción en órdenes en estado Entregando.". Sobre un ítem anulado, debe responder "No se puede modificar un ítem anulado.".

## 2. Los Recibidos cuentan (US2)
Orden en USD, TC 500, impuesto 7%, sin envío interno ni descuento. Ítems A (Ana, lista 10, acordado ₡9 000), B (Beto, lista 20, acordado ₡15 000) y C (Ana, lista 30, acordado ₡20 000), con envío estimado de 2 cada uno. Activar la orden y pasarla a Entregando.
1. Anotar los totales: Acordado ₡44 000, SubTotal $60.00, Total a Proveedor $64.20. Marcar A y C como recibidos. Los totales y el conteo (3) del listado de órdenes no cambian.
2. Reasignar B a otro cliente con un precio de ₡16 000. El acordado total pasa a ₡45 000; incluye a A y C, que están Recibidos.
3. Anotar los saldos de Ana y del nuevo cliente de B. Anular la orden completa. El saldo de Ana baja ₡29 000 (A + C, que están Recibidos) y el del cliente de B baja ₡16 000. En Transacciones se ven 3 Void de la anulación (A, B y C). Son adicionales al Void que generó la reasignación.

## 3. Etiquetas en Saldos de clientes (US3)
Para cada caso, abrir Pagos → Saldos de clientes.

| Caso | Situación | Etiquetas esperadas |
|------|-----------|---------------------|
| a | Cliente con saldo a favor | Solo "Crédito a favor" (verde) |
| b | Saldo ₡50 000, 3 ítems Activos en órdenes Activa/Entregando que suman ₡60 000 | "Productos en camino (3)" (azul) + "Sugerir adelanto" (amarillo) |
| c | Igual que b, pero los ítems suman exactamente ₡50 000 | "Productos en camino (3)" + "Sugerir adelanto" |
| d | Saldo ₡50 000, 1 ítem Activo de ₡40 000 | Solo "Productos en camino (1)" |
| e | Saldo ₡50 000, todos sus ítems de órdenes Entregando Recibidos (suman ₡100 000) | Solo "Entregado – por cobrar" (rojo). **No** aparece "Sugerir adelanto" |
| f | 1 ítem Activo en una orden Activa + 1 Recibido en una orden Entregando | "Productos en camino (1)", sin "Entregado – por cobrar" |
| g | Saldo que solo viene de órdenes Completadas (todos sus ítems Recibidos) o de una transacción manual | Solo "Entregado – por cobrar" (rojo) |
| h | Cliente con saldo en esta organización y con ítems Activos solo en otra organización | Sin "Ver saldo completo": "Entregado – por cobrar" (en esta organización no le queda nada por entregar). Con "Ver saldo completo": "Productos en camino (n)" |

Además:
- "Saldo pendiente" no aparece en ninguna fila.
- El filtro de texto no cambia las etiquetas de cada fila.
- La lista carga en menos de 2 s (SC-006).
