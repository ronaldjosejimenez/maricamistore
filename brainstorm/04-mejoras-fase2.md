---
name: mejoras-fase2
description: Segunda ronda de mejoras — Order Items (agrupado, checklist entrega, reactive pricing), Pagos/Transacciones (pantallas unificadas), símbolo de moneda global.
metadata:
  type: project
---

# Brainstorm: Mejoras Fase 2

**Date:** 2026-06-06
**Status:** active

## Problem Framing

Con la Fase 1 y el refactor de Orden+Items ya implementados, se necesitan tres grupos de mejoras incrementales sobre el sistema MariCamiStore:

1. **Order Items** (`Orders/Items`): faltan ayudas visuales (agrupación por cliente, contador, totales), comportamientos reactivos en el formulario de ítem, y una funcionalidad de checklist de recepción para órdenes en entrega.
2. **Pagos y Transacciones**: las pantallas de Saldos y Registro de Pago están desconectadas; no existe pantalla para transacciones manuales ni para consultar el historial completo de transacciones. Además el balance calculado no descuenta correctamente los pagos.
3. **Símbolo de moneda**: el catálogo de monedas carece del campo `Symbol`; los montos en todas las pantallas no llevan identificador de moneda, lo que confunde cuando hay múltiples monedas.

## Approaches Considered

### A: 3 specs pequeños en secuencia (Elegido)
- **004-order-items-mejoras**: Mejoras de UI en `Orders/Items` — agrupación, ordenamiento, contador, reactive pricing, checklist de recepción, URL más ancho.
- **005-pagos-transacciones**: Nueva pantalla "Cuentas por Cobrar" (Saldos + Registro Pago + filtro cliente) + nueva pantalla "Transacciones" (Registro Manual + Consulta + Filtros + Sumatoria) + fix de balance.
- **006-moneda-simbolos**: Nuevo campo `Symbol` en Currency, actualización de seeds, formateo global de montos con símbolo correspondiente en todas las pantallas.
- Pros: Cada spec es enfocado y manejable. Se pueden hacer en secuencia sin riesgo cruzado. El spec 006 hace la limpieza visual al final cuando todos los datos ya existen.
- Cons: Tres pipelines completos de specify → plan → implement.

### B: 2 specs medianos
- Combinar Order Items + Moneda en un spec; separar Pagos/Transacciones.
- Cons: El req de símbolo de moneda afecta TODAS las pantallas (incluyendo las nuevas de pagos) — mezclarlo en el spec de Order Items crea dependencia artificial entre los dos specs.

### C: 1 spec grande (004-mejoras-fase2)
- Todo en un solo spec.
- Cons: Spec demasiado largo, mayor riesgo de implementación parcial, difícil de revisar y hacer rollback.

## Decision

**Enfoque A: 3 specs en secuencia.** Orden recomendado: 004 → 005 → 006.

## Key Requirements

### Spec 004 — Order Items Mejoras

**UI / Visualización:**
- El grid de ítems debe agruparse por cliente (NickName), con una fila de subtotal de `AgreedPriceInLocal` al final de cada grupo.
- El grid debe ordenarse por defecto: primero por NickName del cliente (A→Z), luego por `CreatedAt` (descendente dentro del grupo).
- Mostrar el conteo total de ítems de la orden visible en la pantalla.
- El campo `ProductLink` (URL) en la grilla y en el modal debe tener un ancho mayor para acomodar URLs largas.

**Formulario de ítem — Reactive Pricing:**
- Mientras el usuario escribe en `ListPrice`, el campo `RealPrice` debe seguir automáticamente ese valor (como comportamiento por defecto).
- Si el usuario edita `RealPrice` manualmente, deja de seguir a `ListPrice` (para ese ítem).
- Nota: `RealPrice = ListPrice` por defecto ya está documentado en la spec 003, pero falta la reactividad mientras se escribe (actualmente solo se copia al guardar o al abrir el modal).

**Checklist de Recepción (req 1.5):**
- Agregar campo `IsReceived` (boolean, default `false`) a la tabla `OrderItems`.
- Cuando el estado de la orden sea `Delivering` o `Delivered`, mostrar un checkbox por ítem para marcar recepción.
- Al marcar el checkbox, la fila cambia de color (ej. verde/resaltado) para indicar que fue verificado.
- El checkbox debe ser editable (toggle: se puede marcar y desmarcar).
- En estados distintos a `Delivering`/`Delivered`, el campo es solo lectura (no se muestra checkbox).
- Color de fila al marcar: clase AdminLTE `table-success` (verde estándar).

### Spec 005 — Pagos, Transacciones y Fix de Balance

**Pantalla A: Cuentas por Cobrar (reemplaza Saldos + Pagos actuales):**
- Unifica `Reports/Saldos` y `Payments/Index` en una sola pantalla.
- Incluye un filtro prominente/obligatorio por cliente.
- Muestra el saldo del cliente seleccionado (fórmula: Σ Cargo - Σ Pago - Σ Void).
- Permite registrar un pago (o cargo) para el cliente seleccionado directamente en la misma pantalla.
- Puede también mostrar la lista de todos los saldos (sin filtro de cliente activo) como vista secundaria.

**Pantalla B: Transacciones (nueva pantalla):**
- Sección de Registro Manual: captura Monto, Cliente, Tipo de transacción.
- Sección de Consulta: grid con columnas Orden (link via `SourceId` si no es null), Cliente, Tipo, Descripción, Monto, Fecha.
- Filtros: Rango de fechas, Cliente, Tipo de transacción.
- Sumatoria del Monto según los filtros activos visible bajo la grilla.

**Fix de Balance (req 5):**
- Investigar por qué los pagos (`TransactionType = "Payment"`) no se descuentan del saldo.
- Posibles causas: `IgnoreQueryFilters()` mezclando orgs, mismatch de strings de tipo, o datos con tipo incorrecto.
- Corregir para que el balance refleje correctamente: Cargo - Pago - Void.

### Spec 006 — Símbolo de Moneda y Formateo Global

**Modelo:**
- Agregar campo `Symbol` (string, máx 5 chars) al modelo `Currency` y su configuración de EF.
- Migración de DB.

**Seed Data:**
- Actualizar la migración de seeds para incluir `Symbol` en las monedas existentes (ej. CRC → `₡`, USD → `$`).
- Re-insertar / actualizar los registros de moneda con el símbolo correcto.

**Formateo Global:**
- En `Orders/Items`: campos de moneda de la orden (ListPrice, RealPrice, ListPriceTaxWithTax) usan el símbolo de la moneda de la orden; campos locales (AgreedPriceInLocal, ServiceFeeInLocal) usan el símbolo de la moneda local de la configuración.
- En `Orders/Index`: totales de la orden con símbolo de la moneda de la orden; `EstimatedProfitInLocal` con símbolo local.
- En `Payments/Cuentas por Cobrar`: saldo con símbolo de moneda local.
- En `Transacciones`: monto con símbolo de la moneda de la transacción.
- En `Reports/Saldos` (si sobrevive o es absorbido por 005): símbolo local.
- En `ProductTypes`: ServiceFeeInLocal y EstimateShipping con sus símbolos.

## Open Questions

- ¿La pantalla "Cuentas por Cobrar" del spec 005 reemplaza las URLs `/Payments` y `/Reports/Saldos` con una nueva URL, o mantiene alguna de las existentes?
- ¿El fix de balance (req 5) aplica también a la pantalla A de Cuentas por Cobrar o solo a la pantalla de Saldos existente?
- ¿La sumatoria en la pantalla de Transacciones debe incluir todos los tipos (Cargo, Pago, Void) o solo un tipo seleccionado?
