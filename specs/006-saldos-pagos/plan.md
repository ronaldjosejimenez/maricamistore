# Implementation Plan: Pantalla Unificada Saldos + Pagos

**Branch**: `006-saldos-pagos` | **Date**: 2026-06-09 | **Spec**: [spec.md](spec.md)

## Summary

Unifica la pantalla de Pagos con la tabla de Saldos de clientes. Actualmente existen dos pantallas separadas (`/Payments/Index` y `/Reports/Saldos`). El cambio agrega la tabla de saldos debajo del formulario de pago, con filtro JS en tiempo real y refresco automático tras registrar un pago. La pantalla `/Reports/Saldos` y su ítem de menú son eliminados.

## Technical Context

**Language/Version**: C# 12 / .NET 8, Razor Pages, JavaScript (ES5+)

**Primary Dependencies**: ASP.NET Core Razor Pages, jQuery, AdminLTE (UI framework ya en uso)

**Storage**: SQL Server via Entity Framework Core (sin cambios de esquema)

**Testing**: Manual (no hay test suite automatizada en el proyecto)

**Target Platform**: Web server (Linux/Windows), ASP.NET Core

**Performance Goals**: Refresco de tabla < 2s post-pago; filtro JS < 100ms

**Constraints**: Sin cambios al modelo de datos ni a rutas existentes. Sin paginación server-side.

## File Structure

### Documentation (esta feature)

```text
specs/006-saldos-pagos/
├── plan.md              ← este archivo
└── spec.md
```

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `MariCamiStore/Pages/Payments/Index.cshtml` | Agregar sección de tabla de saldos |
| `MariCamiStore/Pages/Payments/Index.cshtml.cs` | Agregar `OnGetSaldosAsync` handler |
| `MariCamiStore/wwwroot/js/pages/payments/index.js` | Agregar `loadSaldos()`, filtro, refresco post-pago |
| `MariCamiStore/Services/PaymentService.cs` | Agregar `.OrderBy(r => r.CustomerName)` |
| `MariCamiStore/Pages/Shared/_Layout.cshtml` | Eliminar ítem de menú Saldos |

### Archivos a eliminar

| Archivo |
|---------|
| `MariCamiStore/Pages/Reports/Saldos.cshtml` |
| `MariCamiStore/Pages/Reports/Saldos.cshtml.cs` |

## Implementation Plan

### US1 — Ver Saldos Pendientes en Pantalla de Pagos

#### T001 — Agregar `OnGetSaldosAsync` en `Index.cshtml.cs`

Archivo: `MariCamiStore/Pages/Payments/Index.cshtml.cs`

Agregar después de `OnGetBalanceAsync`:

```csharp
public async Task<JsonResult> OnGetSaldosAsync()
{
    var rows = await paymentService.GetSaldosReportAsync();
    return new JsonResult(rows);
}
```

#### T002 — Agregar `.OrderBy` en `PaymentService.GetSaldosReportAsync`

Archivo: `MariCamiStore/Services/PaymentService.cs` línea ~79

Cambiar:
```csharp
)).ToList();
```
Por:
```csharp
)).OrderBy(r => r.CustomerName).ToList();
```

#### T003 — Agregar sección de saldos en `Index.cshtml`

Archivo: `MariCamiStore/Pages/Payments/Index.cshtml`

Insertar antes de `<script src="/js/pages/payments/index.js"></script>`:

```html
<section class="content mt-3">
    <div class="row">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h3 class="card-title mb-0">Clientes con Saldo Pendiente</h3>
                    <input type="text" id="saldos-filter" class="form-control form-control-sm w-25"
                           placeholder="Filtrar por cliente..." />
                </div>
                <div class="card-body p-0">
                    <div id="saldos-table-container">
                        <p class="p-3 text-muted">Cargando...</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>
```

#### T004 — Agregar `loadSaldos()` y filtro en `payments/index.js`

Archivo: `MariCamiStore/wwwroot/js/pages/payments/index.js`

Agregar al principio del archivo (antes del `$(function(){}`):

```javascript
var allSaldosData = [];

function loadSaldos() {
    $.get('?handler=Saldos', function (data) {
        allSaldosData = data;
        renderSaldos(data);
    }).fail(function () {
        $('#saldos-table-container').html('<p class="p-3 text-danger">Error al cargar los saldos.</p>');
    });
}

function renderSaldos(data) {
    if (!data || data.length === 0) {
        $('#saldos-table-container').html('<p class="p-3 text-muted">No hay saldos pendientes.</p>');
        return;
    }
    var filterVal = $('#saldos-filter').val().toLowerCase();
    var filtered = filterVal
        ? data.filter(function (r) { return r.customerName.toLowerCase().indexOf(filterVal) >= 0; })
        : data;

    var total = 0;
    var rows = filtered.map(function (r) {
        total += r.balance;
        return '<tr><td>' + r.customerName + '</td><td class="text-right">' + r.balance.toFixed(2) + '</td></tr>';
    }).join('');

    var html = '<table class="table table-sm table-bordered mb-0">' +
        '<thead><tr><th>Cliente</th><th class="text-right">Saldo Pendiente</th></tr></thead>' +
        '<tbody>' + rows +
        '<tr class="font-weight-bold"><td>Total</td><td class="text-right">' + total.toFixed(2) + '</td></tr>' +
        '</tbody></table>';
    $('#saldos-table-container').html(html);
}
```

Dentro del `$(function(){})`:
- Agregar al final del bloque existing: `loadSaldos();`
- Agregar listener del filtro: `$('#saldos-filter').on('input', function() { renderSaldos(allSaldosData); });`
- En el callback de pago exitoso (`if (r.success)`): agregar `loadSaldos();`

### US3 — Eliminar Pantalla Redundante de Saldos

#### T005 — Eliminar archivos de Reports/Saldos

```bash
rm MariCamiStore/Pages/Reports/Saldos.cshtml
rm MariCamiStore/Pages/Reports/Saldos.cshtml.cs
```

#### T006 — Eliminar ítem de menú Saldos de `_Layout.cshtml`

Archivo: `MariCamiStore/Pages/Shared/_Layout.cshtml` líneas 243-247

Eliminar el bloque:
```html
<li class="nav-item">
    <a asp-page="/Reports/Saldos" data-menu-group="menuFinance" class="nav-link">
        <i class="far fa-circle nav-icon"></i><p>Saldos</p>
    </a>
</li>
```

## Sequence of Changes

1. T002 — PaymentService (sin dependencias)
2. T001 — Index.cshtml.cs handler (depende de GetSaldosReportAsync existente)
3. T003 — Index.cshtml (sección HTML)
4. T004 — payments/index.js (loadSaldos, filtro, refresco)
5. T005 — Eliminar Reports/Saldos files
6. T006 — Eliminar menú item

Sin migraciones de base de datos. Sin cambios a IPaymentService.
