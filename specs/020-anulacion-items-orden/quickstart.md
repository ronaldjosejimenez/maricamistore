# Quickstart / Manual Verification — 020

Prerrequisitos: `dotnet build` sin errores y migración `AddOrderItemStatus` aplicada.

> **Producción:** el pipeline no aplica migraciones. Hay que ejecutar el script idempotente **antes** de desplegar el código (el código nuevo falla contra el esquema viejo).

> **Verificación de registro:** el proyecto no tiene pruebas automatizadas. Este quickstart es la verificación de la feature. La atomicidad (FR-018 / SC-006) no se puede provocar a mano: se verifica por inspección del único `SaveChangesAsync` de `OrderService.VoidItemsAsync`.

## Datos base
Período CxP abierto con TC 500. Orden en USD con Impuesto 7%, TC 500, envío interno 0 y descuento 0, sin paquetes registrados. Ítems (en Pendiente, antes de activar):

| Ítem | Cliente | Lista | P+Imp | Envío Est. | Acordado (₡) |
|---|---|---|---|---|---|
| A | Ana | 10 | 10.70 | 2 | 9 000 |
| B | Beto | 20 | 21.40 | 2 | 15 000 |
| C | Ana | 30 | 32.10 | 2 | 20 000 |

Activar la orden. Totales esperados: Acordado ₡44 000, SubTotal $60.00, Impuestos $4.20, Envío CR $6.00, Total a Proveedor $64.20, Total Orden $70.20, Ganancia ₡8 900. CxP: AutoActiva $64.20.

**Antes de cada anulación de los §3 y §4, anotar** los indicadores de CxP (Por pagar, Saldos por cobrar, Shipping CR pendiente, Posición) y el saldo de Beto. Los escenarios se verifican por **diferencias** contra esas notas, así que el resultado no depende de otros movimientos del período. Cada escenario usa una orden nueva y equivalente (activada de nuevo con los mismos ítems).

## 1. Toolbar y selección (US1)
1. En una orden Pendiente, Entregando, Completada o Anulada no aparece "Anular". En la orden Activa aparece en rojo junto a "Volver".
2. Presionar "Anular": el botón "Anular" se oculta y aparece la barra con "Anular seleccionados" (deshabilitado) y "Cancelar", y la tabla gana una columna de checkboxes. Marcar un ítem habilita "Anular seleccionados".
3. Marcar un ítem y presionar "Cancelar": desaparece la columna y la barra, vuelve el botón "Anular", y al activar de nuevo el modo no queda nada marcado.
4. Con un ítem ya anulado (después del §3), en modo selección esa fila no tiene checkbox.

## 2. Modal y comparativa (US2)
1. Marcar B → "Anular seleccionados". El modal pregunta "¿Está seguro que desea anular los ítems seleccionados?", lista a B (descripción, cliente, acordado) y trae el monto a devolver precargado con **21.40**.
   - Antes: los totales de arriba.
   - Después: Acordado ₡29 000, SubTotal $40.00, Impuestos $2.80, Envío CR $4.00, Total a Proveedor $42.80, Total Orden $46.80, Ganancia ₡5 600.
   - Ajuste 21.40, teórica 21.40, diferencia 0.00.
2. Cambiar el monto a 18: el Ajuste pasa a 18.00 y la diferencia a −3.40, de inmediato.
3. Marcar A+B+C: el aviso pide usar la anulación de la orden y "Continuar con la anulación" queda deshabilitado.
4. Justificación vacía: "Continuar" deshabilitado. Monto −1: deshabilitado.
5. Monto vacío: "Continuar" deshabilitado (el Ajuste muestra "—"). Monto **0**: permitido; el Ajuste es 0.00 y la diferencia es −21.40.
6. Justificación de más de 1000 caracteres: el campo no deja escribir más de 1000. Por API, una de 1001 devuelve "La justificación no puede superar 1000 caracteres." y no cambia nada.
7. Por API, un monto de 1 000 000 001 devuelve "El monto a devolver es demasiado grande."
8. **Posición en el modal (FR-008a).** Con B marcado, la tabla "Posición CxP" muestra la Posición actual del panel de CxP (la misma cifra que el panel) y la de "Después de anular", que es **₡3 300 menor** con el monto precargado 21.40. Cambiar el monto a 18 la deja ₡5 000 menor; a 25, ₡1 500 menor; vacío, "—". Sin período abierto (§5.1) ambas columnas muestran "—". Tras confirmar, la Posición del panel de CxP coincide con la "Después" mostrada.

## 3. Escenario A (US1, US3)
Anular B con 21.40 y la justificación "Cliente desistió".
- B se ve con fondo rojo pastel y la etiqueta "Anulado". Su fila muestra solo "Ver" (y "Ver Img" si tiene imagen); no tiene Editar, Eliminar ni Reasignar. El badge de conteo muestra 2. El Total Precio Acordado de la tabla es ₡29 000.
- Tras recargar, el header sigue mostrando los totales "Después".
- Transacciones: una fila `Anulación` de ₡15 000 a Beto, en moneda local, con fecha de hoy y una descripción que incluye "Orden {nombre}" y "Cliente desistió". El saldo de Beto baja ₡15 000. Los cargos originales de A, B y C y la entrada AutoActiva siguen intactos (SC-004).
- CxP: una fila "Anulación de ítems" de $21.40 en rojo con "−", con la referencia de la orden. Por pagar USD = $42.80.
- Historial de la orden: una fila nueva Activa → Activa con la nota "Anulación parcial: 1 ítem(s) – ajuste $21.40" y la justificación.
- **Diferencias contra lo anotado**: Por pagar −$21.40 (−₡10 700), Saldos por cobrar −₡15 000, Shipping CR pendiente −₡1 000, **Posición −₡3 300**.
- Con un período limpio (sin otros movimientos, órdenes activas ni pagos), la Posición pasa de ₡8 900 a ₡5 600, igual que la de una orden creada solo con A y C.

## 4. Escenarios B y C (US3)
Cada uno sobre una orden nueva, con las diferencias contra los indicadores anotados justo antes:

| Escenario | Devuelve | Δ Por pagar | Δ Saldos | Δ Shipping | Δ Posición |
|---|---|---|---|---|---|
| A | 21.40 | −$21.40 (−₡10 700) | −₡15 000 | −₡1 000 | **−₡3 300** |
| B (menos) | 18.00 | −$18.00 (−₡9 000) | −₡15 000 | −₡1 000 | **−₡5 000** |
| C (más) | 25.00 | −$25.00 (−₡12 500) | −₡15 000 | −₡1 000 | **−₡1 500** |

Comparado con A: B queda ₡1 700 por debajo (3.40 × 500) y C queda ₡1 800 por encima (3.60 × 500).

## 5. Bordes
1. **Sin período abierto.** Cerrar un período desde la app no sirve, porque crea el del mes siguiente. En un entorno de prueba, correr `UPDATE dbo.PeriodControls SET IsClosed = 1 WHERE IsClosed = 0` (y revertirlo al terminar). Con una selección válida y justificación, "Continuar" muestra "No hay un período CxP abierto." y no cambia nada: ítems, transacciones, totales, CxP e historial quedan como en las notas.
2. **Ítem anulado en el resto de la app.** En B no hay Reasignar. Por API (DevTools, POST con el token antiforgery), `Update`, `Reasignar` y `ToggleReceived` sobre B devuelven "No se puede modificar un ítem anulado." / "No se puede reasignar un ítem anulado.".
3. **Anulación completa posterior.** Anular después la orden completa: solo se generan `Anulación` para A y C, no un segundo para B.
4. **Entregando.** Pasar una orden con un ítem anulado a Entregando: la columna Recibido existe, pero el ítem anulado no tiene checkbox.
5. **Listado de órdenes.** El conteo de ítems excluye los anulados.
6. **Cierre de mes.** La vista previa del cierre y el cierre con una "Anulación de ítems" en el período usan la deuda ya restada: el saldo anterior refleja la resta.
7. **Eliminar el movimiento.** Eliminar desde CxP la fila "Anulación de ítems": se permite, y Por pagar vuelve a sumar los $21.40. Los ítems y los cobros al cliente siguen anulados.
8. **Estado cambiado con el modal abierto.** Abrir el modal en una pestaña y pasar la orden a Entregando en otra: "Continuar" muestra "Solo se pueden anular ítems de órdenes en estado Activa." y no cambia nada.
9. **Ítem reasignado.** Reasignar A a Beto con otro precio acordado y luego anular A: la `Anulación` va a Beto, por el precio acordado nuevo.
10. **Sin montos negativos (SC-005).** En la BD, ambas consultas deben dar 0: `SELECT COUNT(*) FROM dbo.Transactions WHERE TransactionAmount < 0 AND TransactionDescription LIKE N'Anulación de ítem%'` y `SELECT COUNT(*) FROM dbo.CxPEntries WHERE Type = 'AnulacionItems' AND Amount < 0`.
