# Implementation Plan: Paquetes de Envío por Orden (OrderPackage)

**Branch**: `013-paquetes-orden` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/013-paquetes-orden/spec.md`

## Summary

Add an `OrderPackage` entity (immutable, per order, in the order's currency). Adding a package creates an `AutoPaquete` CxP entry in the open period; deleting it creates a negative `ReversoPaquete` entry — each in one atomic `SaveChanges`. Rework "Shipping CR Pendientes" to `Σ over Active+Delivering orders of max(0, ShippingAmountToCR − Σ packages)` converted to colones. Add a "Paquetes de Envío" card to `Orders/Items` with state-based behavior. Retire the 009 shipping rule: remove the "Shipping real a CR" transition field, the `AutoDelivered` creation and the `Order.ActualShippingAmountToCR` column. One EF migration, applied manually.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery)

**Primary Dependencies**: EF Core 10 (SQL Server), AdminLTE 3 / Bootstrap 4, Font Awesome

**Storage**: SQL Server — new table `OrderPackages`; `CxPEntries` gets nullable `OrderPackageId`; `Orders.ActualShippingAmountToCR` dropped

**Testing**: No automated test project; `dotnet build` + manual [quickstart.md](quickstart.md)

**Target Platform**: Azure App Service (deploy on merge to `main`); DB migration applied manually beforehand

**Project Type**: Single web application (`MariCamiStore/`)

**Constraints**: Atomic package + CxP entry (FR-009); server-side state validation (FR-004); no automatic migrations at startup; never run `dotnet ef database update` from the pipeline

**Scale/Scope**: Tens of orders, few packages per order

## Constitution Check

`.specify/memory/constitution.md` is the unfilled template — no ratified principles. Gate: **PASS**. Post-design: **PASS** (follows existing layering: Model → EntityConfiguration → Service → PageModel handler → page JS).

## Project Structure

### Documentation (this feature)

```text
specs/013-paquetes-orden/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/page-handlers.md
└── tasks.md
```

### Source Code

```text
MariCamiStore/
├── Model/
│   ├── OrderPackage.cs                                         [CREATE]
│   ├── CxPEntry.cs                                             [MODIFY] + Guid? OrderPackageId
│   ├── CxPEntryType.cs                                         [CREATE] string constants (AutoActiva, AutoDelivered, SaldoAnterior, Manual, AutoPaquete, ReversoPaquete)
│   └── Order.cs                                                [MODIFY] − ActualShippingAmountToCR
├── Infrastructure/Persistance/
│   ├── MariCamiStoreContext.cs                                 [MODIFY] DbSet<OrderPackage>, apply config, query filter via Order.OrganizationId
│   ├── EntityConfigurations/OrderPackageEntityTypeConfiguration.cs [CREATE]
│   ├── EntityConfigurations/CxPEntryEntityTypeConfiguration.cs [MODIFY] OrderPackageId FK (SetNull) + index
│   ├── EntityConfigurations/OrderEntityTypeConfiguration.cs    [MODIFY] − ActualShippingAmountToCR
│   └── Migrations/<ts>_AddOrderPackages.cs                     [CREATE] dotnet ef migrations add AddOrderPackages
├── Services/
│   ├── IOrderPackageService.cs / OrderPackageService.cs        [CREATE] list+summary, add, delete (atomic with CxP)
│   ├── CxPService.cs                                           [MODIFY] new Shipping CR Pendientes calc
│   ├── IOrderService.cs / OrderService.cs                      [MODIFY] − ActualShippingAmountToCR from TransitionOrderDto; − AutoDelivered block
├── Extensions/ApplicationExtensions.cs                         [MODIFY] register IOrderPackageService
├── Pages/Orders/
│   ├── Items.cshtml / Items.cshtml.cs                          [MODIFY] "Paquetes de Envío" card + handlers
│   └── Index.cshtml                                            [MODIFY] − actual-shipping-group
└── wwwroot/js/pages/
    ├── orders/items.js                                         [MODIFY] load/add/delete packages
    ├── orders/index.js                                         [MODIFY] − actual shipping logic
    └── cxp/index.js                                            [MODIFY] labels for new types; negative amounts "−" red
brainstorm/09-cuentas-por-pagar.md, specs/009-cuentas-por-pagar/spec.md  [MODIFY] discard note (FR-021)
```

## Design Decisions

### D1 — Entity
`OrderPackage { Guid Id; Guid OrderId; DateTime DeliveryDate; decimal Amount; Guid CurrencyId; string? Description; DateTime CreatedAt; Order Order; }` (no `UpdatedAt`: immutable). Config: table `OrderPackages` in default schema; `Amount decimal(18,2)`; `DeliveryDate` `date` column type; `Description` max 500, optional; FK `OrderId` → Orders **Cascade** (orders are deletable only in Pending, when no packages exist); FK `CurrencyId` → Currencies **Restrict**; index on `OrderId`. Query filter `p => p.Order.OrganizationId == currentOrg` (same pattern as `OrderItem`).

### D2 — CxP link and types
`CxPEntry.OrderPackageId Guid?` → FK to `OrderPackages` with **SetNull** (deleting the package clears the link, FR-009a); index. `CxPEntryType` static class with string constants; replace the string literals `"AutoActiva"`, `"AutoDelivered"` etc. where touched. `Type` column (max 30) fits the new values.

### D3 — OrderPackageService (atomic operations)
New scoped service injecting `MariCamiStoreContext` and `ICxPService` (only `GetOpenPeriodAsync`).
- `GetByOrderAsync(orderId)` → `OrderPackagesDto { EstimatedShipping, TotalPackages, Pending = max(0, est − total), CanManage, List<OrderPackageDto> Packages }` (packages ordered by DeliveryDate, CreatedAt).
- `AddAsync(orderId, deliveryDate, amount, description)`:
  1. load order (query filter applies); not found → error.
  2. status ∉ {Active, Delivering} → `"Solo se pueden agregar paquetes en órdenes Activas o Entregando."`.
  3. amount ≤ 0 → `"El monto debe ser mayor a cero."`; description trimmed, empty → null, > 500 → error.
  4. open period null → `"No hay un período CxP abierto."`.
  5. `context.OrderPackages.Add(pkg)` (CurrencyId = order.CurrencyId, Amount rounded 2) + `context.CxPEntries.Add(entry{ Type=AutoPaquete, Amount, CurrencyId, OrderId, OrderPackageId=pkg.Id, Reference=BuildReference(order.NameOfOrder, desc) })` → **one** `SaveChangesAsync` (EF wraps it in a transaction → atomic, FR-009).
- `DeleteAsync(packageId)`: load package + order; status check → `"Solo se pueden eliminar paquetes en órdenes Activas o Entregando."`; open period check; add `ReversoPaquete` entry (Amount = −pkg.Amount, no OrderPackageId, Reference = "Reverso: " + BuildReference(...)); remove package; one `SaveChangesAsync`. The existing AutoPaquete entry's FK becomes NULL via SetNull (FR-009a) and is otherwise untouched (FR-007).
- `BuildReference(name, desc)` = `string.IsNullOrWhiteSpace(desc) ? name : $"{name} - {desc}"`, truncated to 500 (CxPEntry.Reference max).

### D4 — Shipping CR Pendientes (CxPService.GetPeriodIndicatorsAsync)
Replace the Active-only query with:
```csharp
var shippingRows = await context.Orders
    .Where(o => o.Status == OrderStatus.Active.Key || o.Status == OrderStatus.Delivering.Key)
    .Select(o => new { o.CurrencyId, o.ShippingAmountToCR,
        Packages = context.OrderPackages.Where(p => p.OrderId == o.Id).Sum(p => (decimal?)p.Amount) ?? 0m })
    .ToListAsync();
shipping = shippingRows.Sum(r => {
    var pending = Math.Max(0m, r.ShippingAmountToCR - r.Packages);
    return r.CurrencyId == localCurrencyId ? pending : pending * period.ExchangeRate; });
```
Kept inside the existing `exchangeRateWarning` branch (TC = 0 → 0, FR-013). Posición formula unchanged.

### D5 — Items page UI
- New card "Paquetes de Envío" (`card card-info`) between "Artículos" and "Historial de Estado", rendered only when `!isPending` (server-side `@if`), FR-014.
- Summary row: Envío a CR estimado / Total en paquetes / Pendiente (order currency sign).
- Add form (inline row) rendered only when status is Active/Delivering: date (`type=date`, default today via JS), amount (`number step=0.01 min=0.01`), description (`maxlength=500`), button "Agregar". Error div.
- Table: Fecha de entrega | Descripción | Monto | (Eliminar button if canManage). Empty → "Sin paquetes registrados.".
- JS (`items.js`): `loadPackages()` (GET `?handler=Packages&orderId=`), `addPackage()` / `deletePackage(id)` via existing POST helper with antiforgery token; `confirm('¿Eliminar este paquete? Se registrará un reverso en CxP.')`; buttons disabled during request and re-enabled on completion (FR-017a); reload list+summary after success (FR-017). `var canManagePackages` emitted from Razor.
- Handlers in `ItemsModel`: `OnGetPackagesAsync(Guid orderId)`, `OnPostAddPackageAsync([FromBody] AddPackageRequest)`, `OnPostDeletePackageAsync([FromBody] DeletePackageRequest)` returning `{ success, error }` like existing handlers.

### D6 — CxP entries table
`TYPE_LABELS` += `AutoPaquete: 'Auto-Paquete'`, `ReversoPaquete: 'Reverso Paquete'`. Amount cell: if `e.amount < 0` → `<span class="text-danger">−' + formatMoney(Math.abs(e.amount), sign) + '</span>'`. Subtotal likewise when negative. Delete button unchanged for all types (FR-009b).

### D7 — Retire 009 rule
- `Order.ActualShippingAmountToCR` + its EF config removed → migration drops the column (and its default constraint; EF handles it).
- `TransitionOrderDto` loses `ActualShippingAmountToCR`; `TransitionOrderAsync` loses the Delivered block (AutoActiva block stays).
- `Orders/Index.cshtml`: remove `#actual-shipping-group`; `orders/index.js`: remove show/hide + payload field; `openTransitionModal` no longer needs `shippingAmountToCR` param.
- `AutoDelivered` label kept in `TYPE_LABELS` (FR-020).
- Discard notes appended to `brainstorm/09-cuentas-por-pagar.md` and `specs/009-cuentas-por-pagar/spec.md` (FR-021).

### D8 — Migration
`dotnet ef migrations add AddOrderPackages --output-dir Infrastructure/Persistance/Migrations` from `MariCamiStore/`. Review `Up`/`Down`. **Never** run `database update` in the pipeline; the user applies it manually before deploying to `main`.

## Complexity Tracking

A new service (`OrderPackageService`) instead of growing `OrderService` keeps package rules cohesive; no other added complexity.
