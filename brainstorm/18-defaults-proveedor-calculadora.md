# Brainstorm: Defaults por proveedor, calculadora y fórmulas de ítems

**Date:** 2026-10-25
**Status:** active

## Problem Framing
Cada proveedor tiene su propio impuesto, moneda, tipo de cambio y costo de envío, pero hoy esos valores salen de la Configuración global (o están fijos en la calculadora). Se quiere tener defaults por proveedor, que la Calculadora y la creación de órdenes los usen, y ajustar el orden y las fórmulas de los campos de precio en el modal de ítems.

Fuente: `C:\Temp\prompt varios maricami.txt`.

## Approaches Considered

### A: Un solo spec 018 con los 4 puntos
- Pros: los puntos 2 y 3 dependen de los campos nuevos del proveedor (punto 1); el punto 4 es pequeño; un solo branch y ciclo de PR.
- Cons: el PR toca varias pantallas.

### B: Dos specs (proveedor+calculadora+órdenes / ítems)
- Pros: separa los cambios de fórmulas de ítems.
- Cons: dos ciclos para un cambio pequeño.

### C: Cuatro specs
- Pros: máximo control.
- Cons: demasiados ciclos de PR para el tamaño del trabajo.

## Decision
Opción A: un solo spec 018 que cubra los 4 puntos. Las fórmulas se implementan tal como están en el requerimiento.

## Key Requirements

### 1. Catálogo de proveedores (Supplier)
- Campos nuevos, todos editables: `TaxPercentageDefault` (%), `CurrencyIdDefault`, `ExchangeRateDefault`, costo de envío por defecto, "Activo para la calculadora" (bool).
- Al crear un proveedor se precargan desde la Configuración (top 1): `TaxPercentage` → TaxPercentageDefault, `OrderCurrencyIdDefault` → CurrencyIdDefault, `ExchangeRate` → ExchangeRateDefault.
- Una migración llena esos 3 valores en los proveedores existentes con la Configuración (top 1).

### 2. Calculadora (`Pages/Calculator`)
- Nuevo combo "Proveedores" al inicio, que solo lista los proveedores con "Activo para la calculadora" = true.
- Se quitan los campos "Nombre del cliente" y "Código o descripción de la prenda".
- Al seleccionar el proveedor se cargan Impuesto (TaxPercentageDefault), Tipo de cambio (ExchangeRateDefault) y Costo envío (costo de envío por defecto).
- Nuevo checkbox "Envío rápido o local": si está marcado, Costo envío = costo por defecto × 2.
- Se quitan los botones "Agregar a cuenta" y "Nueva cuenta" con su lógica, junto con el grid "Cuenta del cliente".
- Todos los campos siguen siendo editables.
- Fórmulas:
  - Precio con impuesto = Precio de lista + (Precio de lista × Impuesto%) + Costo envío
  - Precio en colones = Precio con impuesto × Tipo de cambio
  - Total = Precio en colones + Costo servicio
- Se elimina la lectura de valores por query string.

### 3. Órdenes
- Al crear una orden, después de elegir el proveedor se cargan ExchangeRate ← ExchangeRateDefault, TaxPercentage ← TaxPercentageDefault y CurrencyId ← CurrencyIdDefault.

### 4. Ítems de la orden (modal)
- Nuevo orden de los campos debajo de la línea:
  - Fila 1: Precio Lista, Precio + Imp, "Envío estimado" (nueva etiqueta de "Envío Est."), Precio Real
  - Fila 2: Total (calculado), Tarifa Servicio, Precio Acordado
- Fórmulas:
  - Precio Real ahora incluye el Envío Est. (Precio Lista + Envío Est.)
  - Precio Acordado = Total (calculado) + Tarifa Servicio, respetando las reglas de edición actuales
- Estos campos quedan deshabilitados hasta que se seleccione un "Tipo de Producto".

## Open Questions
- Hoy Total (calculado) = (Precio+Imp) × TC + Tarifa. Con Precio Acordado = Total + Tarifa, la tarifa queda sumada dos veces; confirmar si Total deja de incluir la tarifa.
- El encabezado de la orden suma "Envío a CR" (Σ Envío Est.) aparte y calcula impuestos sobre Σ Precio Real; al incluir el envío en Precio Real se contaría dos veces y pagaría impuesto. Confirmar el efecto deseado en los totales del encabezado.
- ¿Al editar una orden existente y cambiar el proveedor también se recargan los defaults, o solo al crear?
- ¿En qué moneda está el costo de envío por defecto del proveedor (se asume su CurrencyIdDefault)?
- ¿Precio Real se recalcula al cambiar Envío Est. (respetando si el usuario lo editó a mano)?
