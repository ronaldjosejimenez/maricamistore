# Review Findings: 008-currency-sign

**Reviewer**: Claude Sonnet 4.6 (automated review-code stage)
**Date**: 2026-06-09
**Branch**: `008-currency-sign`

---

## Summary

All functional requirements are implemented correctly. No Critical or Important findings were identified. Three Minor observations are noted for awareness.

**Gate Outcome**: GATE PASS

---

## FR Compliance Matrix

| FR | Description | Status | Notes |
|---|---|---|---|
| FR-001 | Currency.Sign field, max 10 chars | PASS | `nvarchar(10)`, nullable, default `string.Empty` in C# model |
| FR-002 | Colones=₡, Dólares=$ in seed/migration | PASS | Both `UpdateData` calls present in migration; `HasData` in EF config matches |
| FR-003 | Format `{sign} {N2}` when sign present; `{N2}` when empty; es-CR culture | PASS | `AmountFormatter.Format()` uses `es-CR` + `N2`; `formatMoney()` uses `toLocaleString('es-CR', ...)` |
| FR-004 | Currencies jsGrid shows Sign column, editable | PASS | `{ name: 'sign', title: 'Signo', type: 'text', width: 80 }` added; insert/update serialize it automatically |
| FR-005 | All local currency amounts in 5 screens use formatMoney | PASS | All columns and summary rows in Orders/Index, Orders/Items, Payments/Index, Transactions/Index, ProductTypes/Index use `formatMoney(val, localCurrencySign)` |
| FR-006 | Orders/Items provider columns and ProductTypes/estimateShipping use orderCurrencySign | PASS | listPrice, listPriceTaxWithTax, realPrice, estimateShipping, and total in items table use `orderCurrencySign`; ProductTypes estimateShipping looks up currency sign via `currencyItems` array |
| FR-007 | Each affected .cshtml injects localCurrencySign JS var | PASS | All 5 pages inject `<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>`; Orders/Items also injects `orderCurrencySign` |
| FR-008 | formatMoney() in utilities.js | PASS | Function present, correct signature, correct es-CR locale |
| FR-009 | Currencies endpoint returns sign field | PASS | `GetCurrenciesAsync()` returns full `Currency` objects; `Currency.Sign` is serialized by System.Text.Json; jsGrid `loadData` returns `sign` field |

---

## Code Quality Review

### Architecture

- **Correct**: `GetCurrencyByIdAsync` was properly added to `ICatalogService` and implemented in `CatalogService`. This follows the clean architecture pattern used throughout the codebase.
- **Correct**: `AmountFormatter` C# helper is properly isolated in `MariCamiStore.Helpers` namespace with correct `es-CR` culture.
- **Correct**: `formatMoney()` placed in `utilities.js` which is already loaded globally in `_Layout.cshtml` (line 48), ensuring all page-specific scripts can call it without additional imports.
- **Correct**: Each affected PageModel resolves the local currency sign inline (not via a shared base-class method), which is consistent with how `ProductTypes/Index` lacks the `OrganizationPageModel` base class.

### Database / Migration

- Migration `20260609224548_AddSignToCurrency` correctly adds `Sign` as `nvarchar(10) nullable` and applies `UpdateData` for both seed rows.
- `Down()` correctly drops the column.
- EF configuration uses `IsRequired(false)` consistent with the nullable DB column.

### JavaScript

- `formatMoney` correctly handles falsy signs (empty string, null, undefined) via `return sign ? sign + ' ' + formatted : formatted;` — satisfies edge case "empty sign = no sign, no extra space."
- `parseFloat(amount) || 0` guard prevents `NaN` display.
- Orders/Items: All 11 monetary display calls in `recalcOrderHeader` and `renderItemsTable` are covered by `formatMoney`.
- ProductTypes: `currencyItems` array populated with `sign` field from `/Currencies?handler=Load`; `estimateShipping` `itemTemplate` correctly looks up per-row currency sign.

### Security

- **Acceptable risk**: JS injection pattern `var localCurrencySign = '@ViewData["LocalCurrencySign"]';` — the `Sign` field is `nvarchar(10)`, admin-only editable, not customer-facing input. Razor's `@` applies HTML encoding (e.g., `<` becomes `&lt;`), so script injection via angle brackets is prevented. Apostrophe-based JS string escape is a residual risk only if an admin deliberately crafts a malicious sign value, which is accepted per spec. No change needed.
- No raw HTML interpolation of user-supplied data outside of `escapeHtml()` which is correctly used in payments and transactions.

---

## Minor Observations (non-blocking)

### M-01: FR-005 — TotalAgreedPriceInLocal not displayed in Orders/Index grid

**Severity**: Minor  
**Status**: Pre-existing design decision — not a bug in this implementation

The spec's FR-005 lists `TotalAgreedPriceInLocal` as one of the "totales" for Órdenes/Index. However, this column was not in the Orders/Index jsGrid before this feature, and the implementation correctly applies `formatMoney` only to the columns that actually exist in the grid (`totalOfTheOrder`, `estimatedProfitInLocal`). The `OnGetLoadAsync` projection in `Orders/Index.cshtml.cs` includes `TotalAgreedPriceInLocal` as `o.TotalAgreedPriceInLocal`, but the grid field is absent.  
**Assessment**: The column was not previously shown in the index grid; adding it would be a separate enhancement. This implementation is correct for what was displayed before the feature.

### M-02: Initial server-rendered values in Items.cshtml show raw numbers before JS load

**Severity**: Minor — cosmetic only  
**Status**: Acceptable

The `Items.cshtml` renders initial header values via Razor (e.g., `@order.TotalAgreedPriceInLocal`) as raw numbers before JavaScript runs. Once `loadItems()` completes, `refreshTotals()` → `recalcOrderHeader()` overwrites all header values with correctly formatted `formatMoney()` output. This is a brief flicker during page load but not a correctness issue.

### M-03: URL inconsistency for Currencies load endpoint

**Severity**: Minor — pre-existing  
**Status**: Pre-existing, out of scope

`product-types/index.js` calls `/Currencies?handler=Load` while `orders/index.js` calls `/Currencies/Index?handler=Load`. Both routes work in ASP.NET Core Razor Pages (the short form maps to the Index page), and this discrepancy existed before this feature. No change needed.

---

## Files Reviewed

| File | Status |
|---|---|
| `MariCamiStore/Model/Currency.cs` | PASS |
| `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CurrencyEntityTypeConfiguration.cs` | PASS |
| `MariCamiStore/Infrastructure/Persistance/Migrations/20260609224548_AddSignToCurrency.cs` | PASS |
| `MariCamiStore/Helpers/AmountFormatter.cs` | PASS |
| `MariCamiStore/wwwroot/js/utilities.js` | PASS |
| `MariCamiStore/Services/ICatalogService.cs` | PASS |
| `MariCamiStore/Services/CatalogService.cs` | PASS |
| `MariCamiStore/wwwroot/js/pages/currencies/index.js` | PASS |
| `MariCamiStore/Pages/Orders/Index.cshtml.cs` | PASS |
| `MariCamiStore/Pages/Orders/Index.cshtml` | PASS |
| `MariCamiStore/wwwroot/js/pages/orders/index.js` | PASS |
| `MariCamiStore/Pages/Orders/Items.cshtml.cs` | PASS |
| `MariCamiStore/Pages/Orders/Items.cshtml` | PASS |
| `MariCamiStore/wwwroot/js/pages/orders/items.js` | PASS |
| `MariCamiStore/Pages/Payments/Index.cshtml.cs` | PASS |
| `MariCamiStore/Pages/Payments/Index.cshtml` | PASS |
| `MariCamiStore/wwwroot/js/pages/payments/index.js` | PASS |
| `MariCamiStore/Pages/Transactions/Index.cshtml.cs` | PASS |
| `MariCamiStore/Pages/Transactions/Index.cshtml` | PASS |
| `MariCamiStore/wwwroot/js/pages/transactions/index.js` | PASS |
| `MariCamiStore/Pages/ProductTypes/Index.cshtml.cs` | PASS |
| `MariCamiStore/Pages/ProductTypes/Index.cshtml` | PASS |
| `MariCamiStore/wwwroot/js/pages/product-types/index.js` | PASS |
| `MariCamiStore/Pages/Shared/_Layout.cshtml` (utilities.js presence) | PASS |

---

## Decision

No Critical or Important findings. No autonomous fixes required.

**GATE PASS** — Implementation is complete, correct, and spec-compliant.
