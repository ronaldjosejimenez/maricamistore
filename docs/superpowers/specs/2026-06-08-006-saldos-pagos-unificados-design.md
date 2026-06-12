# Design: Pantalla Unificada Saldos + Pagos

**Spec**: 006 | **Date**: 2026-06-08 | **Source**: `requerimientos/Mejoras varias fase 2.txt` ítem 3

---

## Objetivo

Unificar las pantallas `/Reports/Saldos` y `/Payments/Index` en una sola pantalla. Agregar filtro por cliente en la tabla de saldos. Refrescar la tabla al registrar un pago.

---

## Layout

```
┌─────────────────────────────────────────────────────┐
│  Registrar Pago                                     │
│  [Cliente ▼──────────────] [Monto ____] [Registrar] │
│  Saldo Global: 0.00   Saldo Esta Org: 0.00          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Clientes con Saldo Pendiente     [Filtrar: ______] │
│  Cliente            │  Saldo Pendiente              │
│  Ana González       │  ₡ 45,000.00                  │
│  Luis Mora          │  ₡ 12,500.00                  │
│  Total              │  ₡ 57,500.00                  │
└─────────────────────────────────────────────────────┘
```

---

## Cambios de Frontend

### `Payments/Index.cshtml`
- Agregar sección de tabla de saldos debajo de la sección de pago existente
- Input de filtro de texto sobre la tabla (`id="saldos-filter"`)
- Contenedor `<div id="saldos-table-container">` para render AJAX
- Eliminar referencia a `/Reports/Saldos` en navegación (`_Layout.cshtml`)

### `payments/index.js`
- Agregar función `loadSaldos()` → `GET ?handler=Saldos` → renderiza tabla ordenada por Nombre/Apodo ASC
- Filtro en cliente (JS): listener `#saldos-filter` input filtra filas visibles sin nueva llamada al servidor
- En callback de pago exitoso: llamar `loadSaldos()` para refrescar tabla
- Llamada inicial `loadSaldos()` al cargar la página

---

## Cambios de Backend

### `Payments/Index.cshtml.cs`
- Agregar `OnGetSaldosAsync()` → retorna `JsonResult` con lista de `SaldoReportRow`

### `PaymentService.GetSaldosReportAsync`
- Agregar `.OrderBy(r => r.CustomerName)` al resultado final (ordenado por Nombre/Apodo ASC)

### `Reports/Saldos.cshtml` + `Reports/Saldos.cshtml.cs`
- Eliminar ambos archivos (funcionalidad absorbida por `/Payments`)

### `Pages/Shared/_Layout.cshtml`
- Remover ítem de menú "Cuentas por Cobrar / Saldos"
- El ítem "Registro de Pagos" pasa a apuntar a `/Payments` (ya lo hace)

---

## Sin cambios

- `IPaymentService` y `PaymentService` — no requieren modificaciones
- Lógica de pago existente — sin cambios
- URL `/Payments` — se mantiene

---

## Notas

- El filtro opera sobre los datos ya cargados en memoria (JS), no hace llamadas adicionales al servidor
- Al registrar un pago, `loadSaldos()` recarga la tabla completa (refleja el saldo actualizado del cliente que pagó)
- `SaldoReportRow` ya existe en `IPaymentService.cs` — se reutiliza directamente en la respuesta JSON
