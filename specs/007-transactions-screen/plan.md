# Implementation Plan: Pantalla de Transacciones

**Branch**: `007-transactions-screen` | **Date**: 2026-06-09 | **Spec**: [spec.md](spec.md)

## Summary

Nueva pantalla `/Transactions` con tabla filtrable de transacciones (server-side), total condicional por tipo, y modal de entrada manual. Requiere nuevo servicio `ITransactionService`/`TransactionService`, nueva página Razor, nuevo JS, y registro en DI y menú.

## Technical Context

**Language/Version**: C# 12 / .NET 8, Razor Pages, JavaScript (ES5+), jQuery

**Primary Dependencies**: ASP.NET Core, Entity Framework Core, AdminLTE

**Storage**: SQL Server — queries a Transactions + join con OrderItems, Orders, Customers (sin cambio de esquema)

**Testing**: Manual

**Constraints**: Sin paginación. Sin cambio al modelo de datos. Global query filter de Transaction ya aplica OrganizationId.

## File Structure

### Nuevos archivos

| Archivo | Descripción |
|---------|-------------|
| `MariCamiStore/Services/ITransactionService.cs` | Interfaz del servicio con DTOs |
| `MariCamiStore/Services/TransactionService.cs` | Implementación con EF Core |
| `MariCamiStore/Pages/Transactions/Index.cshtml` | Página Razor — tabla + filtros + modal |
| `MariCamiStore/Pages/Transactions/Index.cshtml.cs` | Page model con handlers Load y CreateManual |
| `MariCamiStore/wwwroot/js/pages/transactions/index.js` | JS: loadTransactions, filtros, modal, total |

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `MariCamiStore/Extensions/ApplicationExtensions.cs` | Registrar `ITransactionService → TransactionService` |
| `MariCamiStore/Pages/Shared/_Layout.cshtml` | Agregar ítem "Transacciones" en menú Finanzas |

## Implementation Detail

### ITransactionService.cs

```csharp
using MariCamiStore.Model;

namespace MariCamiStore.Services;

public record TransactionDto(
    Guid Id,
    string? OrderName,
    string? CustomerName,
    string TransactionType,
    string TransactionDescription,
    decimal TransactionAmount,
    DateTime TransactionDate);

public record TransactionFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? CustomerId,
    string? TransactionType);

public record ManualTransactionRequest(
    Guid CustomerId,
    string TransactionType,
    decimal Amount,
    string? Description);

public interface ITransactionService
{
    Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
    Task CreateManualTransactionAsync(ManualTransactionRequest request);
}
```

### TransactionService.cs

Clave: EF Core left join usando query syntax para resolver `SourceId → OrderItem → Order.NameOfOrder`.

```csharp
public async Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
{
    var query = from t in context.Transactions
                join c in context.Customers on t.CustomerId equals c.Id into cg
                from c in cg.DefaultIfEmpty()
                join oi in context.OrderItems.IgnoreQueryFilters() on t.SourceId equals (Guid?)oi.Id into oig
                from oi in oig.DefaultIfEmpty()
                join o in context.Orders.IgnoreQueryFilters() on (Guid?)oi.OrderId equals (Guid?)o.Id into og
                from o in og.DefaultIfEmpty()
                select new { t, c, o };

    if (filter.DateFrom.HasValue)   query = query.Where(x => x.t.TransactionDate >= filter.DateFrom.Value);
    if (filter.DateTo.HasValue)     query = query.Where(x => x.t.TransactionDate <= filter.DateTo.Value);
    if (filter.CustomerId.HasValue) query = query.Where(x => x.t.CustomerId == filter.CustomerId.Value);
    if (!string.IsNullOrEmpty(filter.TransactionType))
        query = query.Where(x => x.t.TransactionType == filter.TransactionType);

    var rows = await query.OrderByDescending(x => x.t.TransactionDate).ToListAsync();

    return rows.Select(x => new TransactionDto(
        x.t.Id,
        x.o?.NameOfOrder,
        x.c != null ? (x.c.NickName ?? x.c.Name) : null,
        x.t.TransactionType,
        x.t.TransactionDescription,
        x.t.TransactionAmount,
        x.t.TransactionDate
    )).ToList();
}

public async Task CreateManualTransactionAsync(ManualTransactionRequest request)
{
    var customer = await context.Customers.FindAsync(request.CustomerId)
        ?? throw new InvalidOperationException("Cliente no encontrado.");

    var config = await context.Configurations.FirstOrDefaultAsync();
    var crTimeZone = "Central America Standard Time";
    var txDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, crTimeZone);
    var description = string.IsNullOrWhiteSpace(request.Description)
        ? $"{request.TransactionType} manual – {customer.NickName ?? customer.Name}"
        : request.Description;

    var transaction = new Transaction
    {
        Id = Guid.NewGuid(),
        OrganizationId = currentOrg.OrganizationId,
        SourceId = null,
        Source = TransactionSource.Manual.Key,
        CustomerId = request.CustomerId,
        TransactionType = request.TransactionType,
        TransactionDescription = description,
        TransactionAmount = request.Amount,
        TransactionDate = txDate,
        Status = TransactionStatus.Applied.Key,
        CurrencyId = config?.LocalCurrencyId ?? Guid.Empty
    };

    context.Transactions.Add(transaction);
    await context.SaveChangesAsync();
}
```

### Pages/Transactions/Index.cshtml.cs

```csharp
namespace MariCamiStore.Pages.Transactions;

public class IndexModel(ITransactionService txService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public IActionResult OnGet() => CheckOrganization() ?? Page();

    public async Task<JsonResult> OnGetLoadAsync(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? customerId,
        [FromQuery] string? transactionType)
    {
        var filter = new TransactionFilterDto(dateFrom, dateTo, customerId, transactionType);
        var rows = await txService.GetTransactionsAsync(filter);
        return new JsonResult(rows);
    }

    public async Task<JsonResult> OnPostCreateManualAsync([FromBody] ManualTransactionRequest request)
    {
        if (request.CustomerId == Guid.Empty || request.Amount <= 0 || string.IsNullOrEmpty(request.TransactionType))
            return new JsonResult(new { success = false, error = "Cliente, tipo y monto son requeridos." });
        try
        {
            await txService.CreateManualTransactionAsync(request);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
```

### Index.cshtml — layout

```
<section class="content-header"> ... Transacciones ... [Nueva Transacción btn] </section>
<section class="content">
  <!-- Filtros row -->
  <div class="row">
    <input type="date" id="filter-from">
    <input type="date" id="filter-to">
    <select id="filter-customer"> ... </select>
    <select id="filter-type"> Payment/Charge/Void + empty </select>
    <button id="btn-filter">Filtrar</button>
  </div>
  <!-- Tabla -->
  <div id="transactions-table-container"></div>
</section>

<!-- Modal -->
<div id="modal-new-tx" class="modal fade">
  <!-- Cliente, Tipo, Monto, Descripción (opcional) -->
  <!-- Cancelar / Guardar -->
</div>
<script src="/js/pages/transactions/index.js"></script>
```

### JavaScript — index.js

- `loadTransactions()`: lee filtros del DOM → `GET ?handler=Load&dateFrom=...` → renderiza tabla con columnas Orden, Cliente, Tipo, Descripción, Monto, Fecha
- Total: si `#filter-type` tiene valor seleccionado, agregar fila Total; si no, omitir
- Modal: `#btn-nueva-tx` abre modal; `#btn-guardar-tx` valida y llama `POST ?handler=CreateManual`; en éxito cierra modal y llama `loadTransactions()`
- Llamada inicial `loadTransactions()` al cargar la página

## Sequence

1. T001: `ITransactionService.cs` (interface + DTOs)
2. T002: `TransactionService.cs` (implementación)
3. T003: Registrar en `ApplicationExtensions.cs`
4. T004: `Index.cshtml` (HTML + modal)
5. T005: `Index.cshtml.cs` (page model + handlers)
6. T006: `transactions/index.js`
7. T007: Agregar ítem de menú en `_Layout.cshtml`

Sin migraciones de base de datos. Sin cambio a modelos existentes.
