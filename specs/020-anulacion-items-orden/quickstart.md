# Quickstart / Manual Verification — 020

Prerrequisitos: `dotnet build` sin errores y migración `AddOrderItemStatus` aplicada.

> **Producción:** el pipeline no aplica migraciones. Hay que ejecutar el script idempotente **antes** de desplegar el código (el código nuevo falla contra el esquema viejo).

## Datos base
Período CxP abierto con TC 500. Orden en USD con Impuesto 7%, TC 500, envío interno 0 y descuento 0. Ítems (en Pendiente, antes de activar):

| Ítem | Cliente | Lista | P+Imp | Envío Est. | Acordado (₡) |
|---|---|---|---|---|---|
| A | Ana | 10 | 10.70 | 2 | 9 000 |
| B | Beto | 20 | 21.40 | 2 | 15 000 |
| C | Ana | 30 | 32.10 | 2 | 20 000 |

Activar la orden. Totales esperados: Acordado ₡44 000, SubTotal $60.00, Impuestos $4.20, Envío CR $6.00, Total a Proveedor $64.20, Total Orden $70.20, Ganancia ₡8 900. CxP: AutoActiva $64.20.

Anotar los indicadores de CxP (Por pagar, Saldos por cobrar, Shipping CR pendiente, Posición) y el saldo de Beto.

## 1. Toolbar y selección (US1)
1. En una orden Pendiente, Entregando o Completada, no aparece "Anular". En la orden Activa aparece en rojo junto a "Volver".
2. Presionar "Anular": aparece la columna de checkboxes. "Anular seleccionados" está deshabilitado. "Cancelar" oculta la columna.

## 2. Modal y comparativa (US2)
1. Marcar B → "Anular seleccionados".
   - Antes: los totales de arriba.
   - Después: Acordado ₡29 000, SubTotal $40.00, Impuestos $2.80, Envío CR $4.00, Total a Proveedor $42.80, Total Orden $46.80, Ganancia ₡5 600.
   - Monto a devolver de B = 21.40. Ajuste 21.40, teórico 21.40, diferencia 0.00.
2. Cambiar a 18: el Ajuste pasa a 18.00 y la diferencia a −3.40, de inmediato.
3. Marcar A+B+C: el aviso pide usar la anulación de la orden y "Continuar" queda deshabilitado.
4. Justificación vacía: no deja continuar. Monto −1: no deja continuar.

## 3. Escenario A (US1, US3)
Anular B con 21.40 y la justificación "Cliente desistió".
- B se ve con fondo rojo pastel y la etiqueta "Anulado", sin Reasignar. El badge muestra 2. El Total Precio Acordado de la tabla es ₡29 000.
- El header muestra los totales "Después".
- Transacciones: un `Anulación` de ₡15 000 a Beto, con fecha de hoy. El saldo de Beto baja ₡15 000.
- CxP: una fila "Anulación de ítems" de $21.40, en rojo con "−". Por pagar USD = $42.80.
- Shipping CR pendiente baja $2 × 500 = ₡1 000. Saldos por cobrar baja ₡15 000. La Posición queda igual a la de una orden creada solo con A y C.

## 4. Escenarios B y C (US3)
Repetir con órdenes equivalentes:
- Devuelve 18: Por pagar $46.20. La Posición queda ₡1 700 (3.40 × 500) menor que en el Escenario A.
- Devuelve 25: Por pagar $39.20. La Posición queda ₡1 800 (3.60 × 500) mayor que en el Escenario A.

## 5. Bordes
1. Cerrar el período CxP (o probar sin período abierto) e intentar anular: aparece "No hay un período CxP abierto" y no cambia nada (ítems, transacciones y totales intactos).
2. Reasignar o Ver en el ítem anulado: no está disponible. Por API, `Update` y `Reasignar` sobre B devuelven error.
3. Anular después la orden completa: solo se generan `Anulación` para A y C (no un segundo para B).
4. Pasar una orden con un ítem anulado a Entregando: el ítem anulado no muestra el checkbox Recibido.
5. Listado de órdenes: el conteo de ítems excluye los anulados.
6. Cierre de mes con una "Anulación de ítems" en el período: el saldo anterior refleja la resta.
