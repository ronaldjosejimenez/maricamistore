# Implementation Plan: Total de Saldos, Filtro de Estados en Órdenes y Talla en Ítems

**Branch**: `011-mejoras-saldos-ordenes-talla` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/011-mejoras-saldos-ordenes-talla/spec.md`

## Summary

Three small, independent UI/data improvements:

1. **Payments – Saldos total**: add a bold `<tfoot>` "Total" row to the client-rendered Saldos table (`wwwroot/js/pages/payments/index.js`), computed from the currently filtered rows. Pure front-end change.
2. **Orders – status checkboxes**: replace the `#statusFilter` `<select>` with six server-rendered checkboxes (one per `OrderStatus`). The page reads an optional `statuses` query parameter to decide the initial selection; the selection travels in the "Items" link (`/Orders/Items?orderId=…&statuses=…`) and back through the "Volver" button. The load handler and `OrderService.GetOrdersAsync` switch from "empty = all" to explicit status lists (empty = none).
3. **OrderItem.Size**: new non-nullable `nvarchar(20)` column with default `''` (EF migration), exposed in the item DTOs and the create/edit item modal only. Server trims and validates length.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`), ASP.NET Core Razor Pages

**Primary Dependencies**: EF Core 10 (SQL Server), AdminLTE 3, jQuery, jsGrid

**Storage**: SQL Server — `OrderItems` table (new `Size` column)

**Testing**: No automated test project exists; verification = `dotnet build` + manual scenarios in [quickstart.md](quickstart.md)

**Target Platform**: Web app deployed to Azure App Service (`maricamistoreweb`) via GitHub Actions on push to `main`

**Project Type**: Single web application (`MariCamiStore/`)

**Performance Goals**: N/A (small business scale; filters are client-side or single indexed queries)

**Constraints**: Existing items must keep working after migration (default `''`); reassignment flow (feature 010) must not alter `Size`

**Scale/Scope**: Tens of customers, hundreds of orders

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled template — no ratified principles to check. Gate: **PASS (no constraints defined)**. Design follows existing project conventions (Razor Pages + page-specific JS in `wwwroot/js/pages/`, services in `Services/`, EF configurations in `Infrastructure/Persistance/EntityConfigurations/`).

Post-design re-check: **PASS** — no new projects, layers or dependencies introduced.

## Project Structure

### Documentation (this feature)

```text
specs/011-mejoras-saldos-ordenes-talla/
├── plan.md              ← this file
├── research.md          ← Phase 0
├── data-model.md        ← Phase 1
├── quickstart.md        ← Phase 1 (manual verification)
├── contracts/
│   └── page-handlers.md ← Phase 1
└── tasks.md             ← Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
MariCamiStore/
├── Model/
│   └── OrderItem.cs                                            [MODIFY] add Size
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/OrderItemEntityTypeConfiguration.cs [MODIFY] Size: required, max 20, default ''
│   └── Migrations/<timestamp>_AddOrderItemSize.cs              [CREATE] dotnet ef migrations add AddOrderItemSize
├── Services/
│   ├── IOrderService.cs                                        [MODIFY] GetOrdersAsync(IReadOnlyCollection<string> statuses); OrderItemWithCustomerDto + Size
│   └── OrderService.cs                                         [MODIFY] explicit status filter (empty = none); project Size
├── Pages/
│   ├── Orders/
│   │   ├── Index.cshtml                                        [MODIFY] remove <select>, render status checkboxes
│   │   ├── Index.cshtml.cs                                     [MODIFY] OnGet parses ?statuses; OnGetLoad(statuses)
│   │   ├── Items.cshtml                                        [MODIFY] Talla input (maxlength 20); Volver carries statuses
│   │   └── Items.cshtml.cs                                     [MODIFY] OnGet(statuses) → ReturnStatuses; DTO Size; trim/validate
│   └── Payments/Index.cshtml                                   [NO CHANGE]
└── wwwroot/js/pages/
    ├── orders/index.js                                         [MODIFY] checkbox-driven load, abort stale requests, Items link with statuses
    ├── orders/items.js                                         [MODIFY] read/write/reset #item-size
    └── payments/index.js                                       [MODIFY] tfoot Total row, no-match row
```

**Structure Decision**: Single existing web project; all changes are in-place modifications plus one EF migration.

## Design Decisions (see research.md for rationale)

### D1 — Status selection representation

- Canonical form: comma-separated `OrderStatus.Key` list, e.g. `Pending,Active`.
- A shared static helper `OrderStatusFilter.Parse(string? raw)` (in `Model/OrderStatus.cs` or a small helper in `Pages/Orders/`) returns the selection:
  - `raw == null` (parameter absent) → default `[Pending, Active]`.
  - `raw` present, trimmed empty → `[]` (explicit empty selection).
  - `raw` present with values → keep only valid keys (case-sensitive match against `OrderStatus.List()`), de-duplicated, in `OrderStatus.List()` order; if values were given but none valid → default `[Pending, Active]`.
- `Index.cshtml.cs OnGetAsync` detects absence via `Request.Query.ContainsKey("statuses")` (model binding cannot distinguish absent vs empty) and exposes `SelectedStatuses` for server-side rendering of `checked`.

### D2 — Load handler & service

- `OnGetLoadAsync(string? statuses)`: parse with the same helper, but here an absent parameter is never sent by the client; the client **short-circuits** an empty selection (returns `[]` to jsGrid without an AJAX call).
- `IOrderService.GetOrdersAsync(IReadOnlyCollection<string> statuses)`: empty collection → returns empty list (no "all" fallback). Only caller is `Orders/Index`.

### D3 — Stale responses (FR-013)

- `orders/index.js` keeps the last `jqXHR`; at the start of every load (including the empty-selection short-circuit) it calls `abort()` on the pending one; jsGrid `onError` ignores aborted requests. jsGrid `loadData` returns the new promise; the aborted request never updates the grid.

### D4 — Carrying selection to Items and back (FR-010/011)

- Items link: `/Orders/Items?orderId={id}&statuses={encodeURIComponent(current)}`.
- `ItemsModel.OnGetAsync(Guid orderId, string? statuses)` stores `statuses` in `ViewData["ReturnStatuses"]`.
- "Volver": href built manually — `ViewData["ReturnStatuses"] is string rs ? "/Orders?statuses=" + Uri.EscapeDataString(rs) : "/Orders"` (manual build keeps an explicit empty `statuses=`, which a tag helper could drop). When null → `/Orders` → defaults (menu behavior).

### D5 — Saldos total (FR-001..005a)

- In `renderSaldos`: keep "No hay saldos registrados." when `data` is empty (no total). Otherwise compute `total = filtered.reduce((s, r) => s + r.balance, 0)`; when `filtered` is empty render one `<tr><td colspan="2" class="text-muted">Ningún cliente coincide con el filtro</td></tr>`. Always render `<tfoot><tr class="font-weight-bold"><td>Total</td><td class="text-right">…</td></tr></tfoot>` with `(total < 0 ? '−' : '') + formatMoney(Math.abs(total), localCurrencySign)`, no badge. Rounding: sum then `Math.round(total * 100) / 100` to avoid float noise.

### D6 — Size field (FR-014..019)

- Entity: `public string Size { get; set; } = string.Empty;`
- EF: `.IsRequired().HasMaxLength(20).HasDefaultValue(string.Empty)` → migration adds `Size nvarchar(20) NOT NULL DEFAULT N''` (existing rows get `''`).
- `OrderItemDto` gets `string? Size`; Insert/Update handlers: `var size = (dto.Size ?? string.Empty).Trim(); if (size.Length > 20) return new JsonResult(new { error = "La talla no puede superar 20 caracteres." });`
- `OrderItemWithCustomerDto` gets `Size` so the edit modal can prefill; the items grid does **not** render it.
- Reassign flow (`ReasignarItemAsync`) loads the tracked entity and only changes customer/price → `Size` preserved (verify, no code change expected).

## Complexity Tracking

No constitution violations; section not applicable.
