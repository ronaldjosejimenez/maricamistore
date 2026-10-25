# Quickstart / Manual Verification — 018

Prerrequisitos: `dotnet build` sin errores y `dotnet ef database update` aplicado (migración `SupplierDefaultsAndRealPriceInLocal`).

## 1. Proveedores (US1)
1. Verificar en BD (o en el grid) que los proveedores existentes tienen Impuesto, Moneda y TC default iguales a los de la Configuración, Envío = 0 y "Activo calculadora" = no.
2. En Proveedores, abrir la fila de inserción: Impuesto, Moneda y TC vienen precargados desde la Configuración. Cambiar el TC y guardar: se guarda el valor editado.
3. Intentar guardar con TC = 0 o Impuesto negativo: aparece un mensaje y no se guarda.
4. Editar "Shein": Envío = 2, Activo calculadora = sí. Guardar.

## 2. Calculadora (US2)
1. Abrir Calculadora: el combo "Proveedores" solo muestra Shein. No están Nombre del cliente, Código de prenda, "Agregar a cuenta", "Nueva cuenta" ni el grid.
2. Seleccionar Shein (Imp 7, TC 530, Envío 2): se cargan los tres campos.
3. Precio de lista 10, Costo servicio 3000 → Calcular: Precio con impuesto 12.70, Precio en colones 6731.00, Total **9800**.
4. Marcar "Envío rápido o local": Costo envío = 4. Calcular: 14.70 / 7791.00 / **10800**. Desmarcar: Costo envío = 2.
5. Abrir `/Calculator?vat=99&exchangeRate=1`: se ignoran los parámetros.
6. Limpiar: vuelve a los valores de Shein, desmarca el checkbox y pone los resultados en 0.

## 3. Orden nueva (US3)
1. Darle al proveedor Adidas: Imp 13, TC 520, Moneda EUR.
2. Órdenes → Nueva → seleccionar Adidas: TC 520, Imp 13, Moneda EUR. Cambiar a Shein: se cargan los valores de Shein.
3. Editar una orden existente y cambiar el proveedor: TC, Imp y Moneda no cambian.

## 4. Ítems (US4)
1. En una orden Pendiente (Imp 7, TC 530), Agregar Ítem: los campos de precio están deshabilitados.
2. Elegir un Tipo de Producto (envío 3, tarifa 3000): se habilitan. Orden de los campos:
   - Fila 1: Lista | Precio + Imp | Envío estimado | Precio Real
   - Fila 2: P. Real en Local | Tarifa | Total | Acordado
3. Precio Lista 10 → 10.70 | 3 | 13.70 / 7261.00 | 3000 | 10261.00 | 10261.00.
4. Editar Precio Acordado a 11000, luego cambiar Lista: Acordado se queda en 11000.
5. Guardar: la columna Total de la tabla = 10261.00. El encabezado muestra Subtotal = 10 (Σ Lista), Impuestos = 0.70 y Envío a CR = 3.
6. Cambiar el TC de la orden a 540 y guardar: P. Real en Local = 7398.00 y Acordado = 10398.00 (Trigger A).
7. Reabrir el ítem: Acordado no aparece como editado a mano (sigue recalculando).
