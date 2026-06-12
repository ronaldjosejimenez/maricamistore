# Brainstorm: Pantalla Unificada Saldos + Pagos

**Fecha**: 2026-06-08  
**Fuente**: `requerimientos/Mejoras varias fase 2.txt` ítem 3

---

## Problema

Actualmente existen dos pantallas separadas:
- `/Payments/Index` — registrar pagos
- `/Reports/Saldos` — ver saldos de clientes

El usuario quiere una sola pantalla con:
1. El formulario de pago (parte superior, ya existe)
2. La tabla de saldos de clientes (parte inferior, moverla desde Saldos)
3. Un filtro de texto sobre la tabla de saldos
4. Que la tabla se refresque automáticamente después de registrar un pago

---

## Layout propuesto

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

## Cambios necesarios

### Frontend — `Payments/Index.cshtml`
- Agregar sección de tabla de saldos debajo del formulario de pago existente
- Input de filtro de texto (`id="saldos-filter"`) sobre la tabla
- Contenedor `<div id="saldos-table-container">` para render AJAX

### JavaScript — `payments/index.js`
- Agregar función `loadSaldos()` → `GET ?handler=Saldos` → renderiza tabla ordenada por Nombre/Apodo ASC
- Filtro JS: listener en `#saldos-filter` filtra filas visibles sin llamada adicional al servidor
- En callback de pago exitoso: llamar `loadSaldos()` para refrescar la tabla
- Llamada inicial `loadSaldos()` al cargar la página

### Backend — `Payments/Index.cshtml.cs`
- Agregar `OnGetSaldosAsync()` → retorna `JsonResult` con lista de `SaldoReportRow`

### Backend — `PaymentService.GetSaldosReportAsync`
- Agregar `.OrderBy(r => r.CustomerName)` para ordenar por Nombre/Apodo ASC

### Eliminar
- `Reports/Saldos.cshtml` y `Reports/Saldos.cshtml.cs` — funcionalidad absorbida
- Ítem de menú "Cuentas por Cobrar / Saldos" en `_Layout.cshtml`

---

## Notas

- `SaldoReportRow` ya existe en `IPaymentService.cs`, se reutiliza directamente
- El filtro opera sobre datos ya cargados (JS), sin llamadas adicionales
- La URL `/Payments` se mantiene sin cambios
- IPaymentService y PaymentService no requieren cambios de interfaz
