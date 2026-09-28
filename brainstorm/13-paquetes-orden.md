---
name: paquetes-orden
description: Paquetes/envíos por orden (OrderPackage) que generan la deuda de shipping en CxP, con reversión al eliminar; nuevo cálculo de "Shipping CR Pendientes"; descarta la regla de shipping real al entregar (requerimiento 009).
metadata:
  type: project
---

# Brainstorm: Paquetes de Envío por Orden (OrderPackage)

**Date:** 2026-09-28
**Status:** spec-created
**Spec:** specs/013-paquetes-orden

## Problem Framing

El requerimiento 009 (Cuentas por Pagar) definió que el costo del shipping a CR entraba a CxP en una sola transición:

- Al pasar la orden a **Entregada**, el diálogo pedía "Shipping real a CR" (`Order.ActualShippingAmountToCR`) y se creaba una entrada CxP `AutoDelivered`.
- "Shipping CR Pendientes" sumaba el shipping estimado (`ShippingAmountToCR`) **solo de órdenes Activas**.

En la práctica esa regla **no funciona ni sirve**. Esto se investigó en producción el 2026-09-28:

- Las órdenes se quedan en **Entregando**: 9 órdenes, sin ningún paso por el botón "Entregada". Las 2 órdenes "Entregada" llegaron a ese estado por fuera de la app, porque no tienen historial de esa transición. Resultado: **0 entradas AutoDelivered**; el shipping nunca llegó a CxP.
- Hueco: al pasar a Entregando, la orden sale de "Shipping CR Pendientes", pero su shipping tampoco está en CxP. La **Posición no descuenta** el shipping de esas órdenes.
- El shipping real no llega en un solo pago al final. Llega **por paquetes**, a lo largo del tiempo, mientras la orden está Activa o Entregando.

**Se descarta la regla de shipping del requerimiento 009** y se reemplaza por paquetes de envío registrados por orden.

## Approaches Considered

La decisión principal fue qué hacer en CxP al **eliminar** un paquete.

### A: Sin reversión (ajuste manual)
- Pros: lo más simple.
- Cons: la deuda queda inflada hasta un ajuste manual (fácil de olvidar), y "Shipping CR Pendientes" sube al instante. El monto se cuenta **dos veces** en la Posición hasta que alguien lo corrija.

### B: Borrar la entrada CxP vinculada si su mes sigue abierto
- Pros: todo queda cuadrado sin entradas extra mientras el mes esté abierto.
- Cons: si el paquete es de un mes ya cerrado, la entrada no se puede tocar y vuelve el problema de A.

### C: Movimiento de reversión — elegido
- Pros: siempre cuadra, aunque el paquete sea de un mes cerrado. Deja rastro de auditoría. La deuda baja justo cuando el pendiente sube, así que no hay doble conteo.
- Cons: aparecen montos negativos en CxP. Requiere un mes abierto también para eliminar.

## Decision

Nueva entidad **OrderPackage**. Cada paquete registrado crea una entrada CxP **AutoPaquete**. Al eliminarlo se crea una entrada **ReversoPaquete** con el monto en negativo (**enfoque C**). "Shipping CR Pendientes" se recalcula por orden (Activas y Entregando) como `máx(0, estimado − paquetes)`. Se elimina por completo la regla del requerimiento 009: el campo del diálogo, la columna y la entrada AutoDelivered.

## Key Requirements

### 1. Entidad OrderPackage
- Campos:
  - `Id`
  - `OrderId` (requerido, relación con la orden)
  - `DeliveryDate` (requerido; el formulario sugiere hoy y acepta cualquier fecha)
  - `Amount` (requerido, mayor que 0)
  - `CurrencyId` (requerido; **siempre la moneda de la orden**, asignada automáticamente y no editable)
  - `Description` (opcional)
  - `CreatedAt`
  - Auditoría estándar del proyecto (`UpdatedAt` si aplica)
- La moneda de la orden solo se puede cambiar en *Pendiente*, y en *Pendiente* no hay paquetes, así que siempre coinciden.
- Los paquetes son **inmutables**: solo se agregan y se eliminan, nunca se editan.
- La migración de base de datos se genera, pero **se aplica a mano** en los ambientes (la app no migra sola).

### 2. Integración con CxP
- **Al agregar un paquete:** se crea una entrada CxP en el **período abierto**:
  - Moneda: la de la orden.
  - Monto: el del paquete.
  - Tipo: **`AutoPaquete`**.
  - Orden asociada: la del paquete. Además, la entrada queda **vinculada al paquete** para poder revertirlo.
  - Referencia: `"{Nombre de la orden} - {Descripción}"`. Si no hay descripción, solo `"{Nombre de la orden}"`.
- **Si no hay período abierto:** no se guarda el paquete y se muestra un mensaje. Aplica igual para eliminar.
- **Al eliminar un paquete:** se crea en el período abierto una entrada **`ReversoPaquete`** con el monto en **negativo**, la misma moneda y la referencia `"Reverso: {Orden} - {Descripción}"`. La entrada AutoPaquete original no se toca (puede estar en un mes cerrado).
- Solo se puede eliminar si la orden está **Activa** o **Entregando**.
- Guardar el paquete y crear su entrada CxP es una sola operación: o se hacen ambas, o ninguna. Lo mismo al eliminar con la reversión.
- La tabla de entradas CxP muestra los tipos nuevos con etiquetas legibles ("Auto-Paquete", "Reverso Paquete").

### 3. "Shipping CR Pendientes" (pantalla CxP)
- Solo cuentan las órdenes **Activas** o **Entregando**. Las demás (Pendiente, Entregada, Completada, Anulada) quedan fuera.
- Por orden: `máx(0, ShippingAmountToCR − Σ paquetes de la orden)`, en la moneda de la orden, **convertido a colones** con el tipo de cambio del período. Las órdenes en colones no se convierten.
- Si una orden tiene más paquetes que su estimado, **aporta 0**: no resta el pendiente de otras órdenes, porque el sobrecosto ya está en CxP como deuda.
- La Posición sigue usando este indicador; su fórmula no cambia.
- Con el tipo de cambio en 0 se mantiene el comportamiento actual: el indicador convertido muestra 0 y aparece el aviso.

### 4. UI de paquetes (pantalla de ítems / detalle de la orden)
- **Pendiente:** la sección de paquetes está **oculta**.
- **Activa o Entregando:** se muestran la lista, el formulario **Agregar paquete** y el botón **Eliminar** por fila (con confirmación). No se puede editar.
- **Otros estados** (Entregada, Completada, Anulada): la lista se ve en **solo lectura**, sin agregar ni eliminar.
- **Resumen** sobre la lista: "Envío a CR estimado", "Total en paquetes" y "Pendiente" (`máx(0, estimado − paquetes)`), en la moneda de la orden.
- Columnas de la lista: fecha de entrega, descripción y monto con signo de moneda.
- Las reglas se validan también en el servidor: agregar o eliminar fuera de Activa/Entregando se rechaza aunque se llame directo al handler.

### 5. Retiro de la regla del requerimiento 009 (descartada)
- Quitar el campo **"Shipping real a CR"** del diálogo de transición a Entregada. La transición a Entregada no pide nada extra.
- Quitar la creación de la entrada CxP **`AutoDelivered`** en la transición a Entregada.
- **Eliminar la columna `Order.ActualShippingAmountToCR`**, del modelo y de la base (migración).
- Las entradas `AutoDelivered` históricas, si existieran en períodos pasados, se conservan y se siguen mostrando con su etiqueta.
- **Documentar el descarte** en `brainstorm/09-cuentas-por-pagar.md` y `specs/009-cuentas-por-pagar/spec.md`: una nota que diga que la regla de shipping (FR-008 a FR-010 y el cálculo de "Shipping CR Pendientes" solo con Activas) queda reemplazada por el requerimiento 013.

### Fuera de alcance
- Editar paquetes.
- Paquetes en una moneda distinta a la de la orden.
- Reversión automática de CxP al **anular una orden**. Sigue la regla del 009: ajuste manual.
- Revertir o corregir las 2 órdenes que se pusieron en "Entregada" por fuera de la app.
- Autenticación o permisos por usuario (pendiente aparte).

## Open Questions
_(todas resueltas en la spec 013)_
- Entregando → Entregada: sin validación de paquetes en esta versión.
- Entradas manuales de CxP: siguen aceptando solo montos positivos; solo Reverso Paquete puede ser negativo.
- Montos negativos en la tabla CxP: con "−" y en rojo; restan del total de su moneda.

## Cierre (2026-09-28)
- Implementado en `specs/013-paquetes-orden`: PR #21 (→ develop) y PR #22 (→ main). Desplegado a producción con éxito y verificado por el usuario.
- Migraciones aplicadas manualmente en Azure antes del despliegue: `AddOrderPackages` y `AddOrderPackageTrackingNumber`.
- Decisiones tomadas durante el proceso:
  - FK OrderPackages → Orders en **Restrict**, para evitar rutas de cascada múltiples en SQL Server.
  - Las entradas Auto-Paquete y Reverso Paquete se pueden borrar desde CxP como cualquier otra; el paquete no se modifica.
  - Validación en el servidor de la fecha requerida y del monto redondeado a 2 decimales.
- Cambio posterior a pedido del usuario: campo opcional **"Número de tracking"** (`TrackingNumber`, máx. 100), en una migración separada.
- La regla de shipping del requerimiento 009 queda descartada y documentada en `brainstorm/09-cuentas-por-pagar.md` y `specs/009-cuentas-por-pagar/spec.md`.
