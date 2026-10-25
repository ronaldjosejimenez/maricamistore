# Quickstart / Manual Verification — 018

Prerrequisitos: `dotnet build` sin errores y `dotnet ef database update` aplicado (migración `SupplierDefaultsAndRealPriceInLocal`).

> **Producción:** el pipeline (`.github/workflows/deploy-azure.yml`) no aplica migraciones. Hay que ejecutar el script idempotente de la migración **antes** de desplegar el código. El código nuevo falla contra el esquema viejo; el código viejo sí funciona con el esquema nuevo.

## 1. Proveedores (US1)
1. Verificar en BD (o en el grid) que los proveedores existentes tienen Impuesto, Moneda y TC default iguales a los del primer registro de Configuración, y Envío = 0. **Shein** debe tener "Activo calculadora" = sí y los demás = no. Abrir la Calculadora: el combo ya muestra Shein.
2. En Proveedores, abrir la fila de inserción: Impuesto, Moneda y TC vienen precargados desde la Configuración de la organización actual. Cambiar el TC y guardar: se guarda el valor editado.
3. Intentar guardar con TC = 0, Impuesto negativo, Impuesto vacío, Moneda vacía o Envío = -1: aparece un mensaje y no se guarda.
4. Editar "Shein": Envío = 2 y TC = 530 (para los ejemplos siguientes). Guardar y recargar la página: los valores persisten.

## 2. Calculadora (US2)
1. Abrir Calculadora: el combo "Proveedores" solo muestra Shein. No están Nombre del cliente, Código de prenda, "Agregar a cuenta", "Nueva cuenta" ni el grid.
2. Seleccionar Shein (Imp 7, TC 530, Envío 2): se cargan los tres campos.
3. Precio de lista 10, Costo servicio 3000 → Calcular: Precio con impuesto 12.70, Precio en colones 6731.00, Total **9800**.
4. Marcar "Envío rápido o local": Costo envío = 4. Calcular: 14.70 / 7791.00 / **10800**. Desmarcar: Costo envío = 2.
5. Abrir `/Calculator?vat=99&exchangeRate=1`: se ignoran los parámetros.
6. Limpiar: vuelve a los valores de Shein, desmarca el checkbox y pone los resultados en 0.
7. Redondeo a la centena. Digitar a mano TC 1, Imp 1, Envío 0 y Lista 100 (Precio con impuesto 101.00), y variar el Costo servicio:
   - 4988 → Total 5100
   - 4999 → Total **5100** (múltiplo exacto, no cambia)
   - 4999.01 → Total 5200
   - 17789 → Total 17900
8. Activar Adidas (Envío 0):
   - Seleccionar Adidas: Envío 0. Marcar el checkbox: sigue en 0.
   - Volver a Shein con el checkbox marcado: Envío 4.
   - Digitar Envío 9 y desmarcar: Envío 2.

## 3. Orden nueva (US3)
1. Darle al proveedor Adidas: Imp 13, TC 520, Moneda EUR.
2. Órdenes → Nueva → seleccionar Adidas: TC 520, Imp 13, Moneda EUR. Cambiar a Shein: se cargan los valores de Shein.
3. Editar una orden existente y cambiar el proveedor: TC, Imp y Moneda no cambian.

## 4. Ítems (US4)
1. En una orden Pendiente (Imp 7, TC 530), Agregar Ítem: los campos de precio están deshabilitados.
2. Elegir un Tipo de Producto (envío 3, tarifa 3000): se habilitan, excepto P. Real en Local y Total, que siempre son de solo lectura. Orden de los campos:
   - Fila 1: Lista | Precio + Imp | Envío estimado | Precio Real
   - Fila 2: P. Real en Local | Tarifa | Total | Acordado
3. Precio Lista 10 → 10.70 | 3 | 13.70 / 7261.00 | 3000 | 10261.00 | 10261.00.
4. Editar Precio Acordado a 11000 y cambiar Lista a 20: Acordado se queda en 11000. Cancelar y abrir el ítem de nuevo con Lista 10 antes del paso 5.
5. Guardar: la columna Total de la tabla = 10261.00. El encabezado muestra Subtotal = 10 (Σ Lista), Impuestos = 0.70 y Envío a CR = 3.
6. Cambiar el TC de la orden a 540 y guardar: P. Real en Local = 7398.00 y Acordado = 10398.00 (Trigger A).
7. Reabrir el ítem: Acordado no aparece como editado a mano (sigue recalculando).
8. Cambiar el Imp de la orden a 13 (TC sigue en 540) y guardar: Precio + Imp 11.30, Precio Real 14.30, P. Real en Local 7722.00 y Acordado 10722.00 (no fue editado a mano, así que sigue al Total).
9. Editar Precio Real a mano a 20.00 y guardar. Cambiar el Imp de la orden a 7: Precio + Imp 10.70, Precio Real se queda en 20.00, P. Real en Local = 10800.00 y Acordado = 13800.00.
10. Anotar en la lista de Órdenes el Total Orden y la Ganancia Est. de una orden **Completada**. Abrirla y volver a la lista: los valores no cambian.
11. En una orden Activa, Reasignar un ítem con otro precio: cambian solo Precio Acordado Total y Ganancia Est.; Subtotal, Impuestos, Envío a CR y Total Orden quedan iguales.
12. Abrir un ítem en modo "Ver" (orden no Pendiente): todos los campos están en solo lectura.
