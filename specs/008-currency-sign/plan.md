# Implementation Plan: Signo de Moneda + Formateo de Montos

**Feature**: 008-currency-sign
**Branch**: `008-currency-sign`
**Created**: 2026-06-09

---

## Summary

Add a `Sign` field (e.g., `₡`, `$`) to the `Currency` model, seed it for the two existing currencies, expose it through the catalog API, and use it in all amount-rendering pages so every monetary value shows `{sign} {amount}` (or just `{amount}` when sign is empty). A shared `formatMoney(amount, sign)` function in `utilities.js` centralizes the formatting. Five Razor pages inject the sign from the server as a JS global variable.

---

## Technical Context

| Concern | Detail |
|---|---|
| Language / Runtime | C# 12 / .NET 8, ASP.NET Core Razor Pages |
| ORM | Entity Framework Core 8 (SQL Server) |
| Migrations | EF Core Code-First; `dotnet ef migrations add` then `dotnet ef database update` |
| JS | Vanilla jQuery + jsGrid; no bundler/transpiler — files are served as-is from `wwwroot` |
| Culture for formatting | `es-CR` (`1.000.000,00` style). C#: `N2` with `new CultureInfo("es-CR")`. JS: `toLocaleString('es-CR', {minimumFractionDigits:2, maximumFractionDigits:2})` |
| Sign storage | `nvarchar(10)`, nullable in DB but defaults to `string.Empty` in C# model |
| Seed GUIDs | Colones = `63B4D953-66D5-409E-929D-6036111FB710`, Dólares = `63B4D953-66D5-409E-929D-6036111FB711` |
| No new services | Sign resolution uses `ICatalogService` (already injected) or `MariCamiStoreContext` directly |

---

## File Structure

### New Files

| File | Purpose |
|---|---|
| `MariCamiStore/Helpers/AmountFormatter.cs` | Static C# helper: `Format(decimal amount, string sign)` |
| `MariCamiStore/Infrastructure/Persistance/Migrations/YYYYMMDDHHMMSS_AddCurrencySign.cs` | EF migration: add `Sign` column + UPDATE seed rows |

### Modified Files

| File | Change |
|---|---|
| `MariCamiStore/Model/Currency.cs` | Add `public string Sign { get; set; } = string.Empty;` |
| `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CurrencyEntityTypeConfiguration.cs` | Add `Sign` property config + update `HasData` seed with sign values |
| `MariCamiStore/wwwroot/js/utilities.js` | Add `formatMoney(amount, sign)` function |
| `MariCamiStore/wwwroot/js/pages/currencies/index.js` | Add `sign` field to jsGrid |
| `MariCamiStore/Pages/Currencies/Index.cshtml` | (no change needed — jsGrid renders sign field via JS) |
| `MariCamiStore/Pages/Orders/Index.cshtml` | Add `<script>var localCurrencySign = '...';</script>` block |
| `MariCamiStore/Pages/Orders/Index.cshtml.cs` | Inject `ICatalogService`; resolve `LocalCurrencySign` in `OnGet` via `ViewData` |
| `MariCamiStore/wwwroot/js/pages/orders/index.js` | Replace `.toFixed(2)` amount renders with `formatMoney(...)` |
| `MariCamiStore/Pages/Orders/Items.cshtml` | Add `localCurrencySign` and `orderCurrencySign` to existing `<script>` block |
| `MariCamiStore/Pages/Orders/Items.cshtml.cs` | Resolve both currency signs and set as `ViewData`; expose on `ItemsModel` |
| `MariCamiStore/wwwroot/js/pages/orders/items.js` | Replace amount renders with `formatMoney(...)` (local vs order sign) |
| `MariCamiStore/Pages/Payments/Index.cshtml` | Add `<script>var localCurrencySign = '...';</script>` |
| `MariCamiStore/Pages/Payments/Index.cshtml.cs` | Inject `ICatalogService`; resolve and expose `LocalCurrencySign` |
| `MariCamiStore/wwwroot/js/pages/payments/index.js` | Replace `.toFixed(2)` with `formatMoney(...)` |
| `MariCamiStore/Pages/Transactions/Index.cshtml` | Add `<script>var localCurrencySign = '...';</script>` |
| `MariCamiStore/Pages/Transactions/Index.cshtml.cs` | Inject `ICatalogService`; resolve and expose `LocalCurrencySign` |
| `MariCamiStore/wwwroot/js/pages/transactions/index.js` | Replace `.toFixed(2)` with `formatMoney(...)` |
| `MariCamiStore/Pages/ProductTypes/Index.cshtml` | Add `<script>var localCurrencySign = '...';</script>`, inject `orderCurrencySign` via callback |
| `MariCamiStore/Pages/ProductTypes/Index.cshtml.cs` | Inject `ICatalogService`; resolve and expose `LocalCurrencySign` |
| `MariCamiStore/wwwroot/js/pages/product-types/index.js` | Use `formatMoney(...)` for `serviceFeeInLocal` (local sign) and `estimateShipping` (order/currency sign from loaded currency data) |

---

## Implementation Detail

### TASK-01: Currency model — add Sign property

**File**: `MariCamiStore/Model/Currency.cs`

```csharp
public class Currency
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;   // NEW
}
```

---

### TASK-02: EF configuration — map Sign property and update seed data

**File**: `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CurrencyEntityTypeConfiguration.cs`

Add property mapping after `Abbreviation`:
```csharp
builder.Property(c => c.Sign)
    .HasMaxLength(10)
    .IsRequired(false);
```

Update `HasData` to include signs:
```csharp
builder.HasData(
    new Currency
    {
        Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB710"),
        Name = "Colones",
        Abbreviation = "COL",
        Sign = "₡"
    },
    new Currency
    {
        Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711"),
        Name = "Dolares",
        Abbreviation = "USD",
        Sign = "$"
    });
```

---

### TASK-03: EF migration — add Sign column and seed UPDATE

Run:
```
dotnet ef migrations add AddCurrencySign --project MariCamiStore
```

The generated migration will contain `AddColumn` for `Sign` plus `UpdateData` for both currency rows. Verify the `Up()` method includes:

```csharp
migrationBuilder.AddColumn<string>(
    name: "Sign",
    schema: "dbo",
    table: "Currencies",
    type: "nvarchar(10)",
    maxLength: 10,
    nullable: true,
    defaultValue: null);

migrationBuilder.UpdateData(
    schema: "dbo",
    table: "Currencies",
    keyColumn: "Id",
    keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb710"),
    column: "Sign",
    value: "₡");

migrationBuilder.UpdateData(
    schema: "dbo",
    table: "Currencies",
    keyColumn: "Id",
    keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb711"),
    column: "Sign",
    value: "$");
```

Apply with: `dotnet ef database update --project MariCamiStore`

---

### TASK-04: AmountFormatter C# helper (new file)

**File**: `MariCamiStore/Helpers/AmountFormatter.cs`

```csharp
using System.Globalization;

namespace MariCamiStore.Helpers;

/// <summary>Formats monetary amounts with an optional currency sign.</summary>
public static class AmountFormatter
{
    private static readonly CultureInfo EsCr = new("es-CR");

    /// <summary>
    /// Returns "{sign} {amount:N2}" when sign is non-empty,
    /// otherwise returns "{amount:N2}". Culture: es-CR.
    /// </summary>
    public static string Format(decimal amount, string sign)
    {
        var formatted = amount.ToString("N2", EsCr);
        return string.IsNullOrWhiteSpace(sign)
            ? formatted
            : $"{sign} {formatted}";
    }
}
```

Note: This helper is available for server-side rendering (e.g., Items.cshtml header values). It is not used in the current scope since all amount rendering in the affected pages is done client-side via JavaScript, but it is created so future server-side rendering has a single, culture-correct helper.

---

### TASK-05: formatMoney() in utilities.js

**File**: `MariCamiStore/wwwroot/js/utilities.js`

Append after the existing `ParseDataSourceToJson` function:

```javascript
/**
 * Formats a monetary amount with an optional currency sign.
 * Returns "{sign} {amount}" when sign is non-empty, else just the formatted amount.
 * Uses es-CR locale (e.g. 1.000,00).
 * @param {number} amount
 * @param {string} sign
 * @returns {string}
 */
function formatMoney(amount, sign) {
    var formatted = (parseFloat(amount) || 0).toLocaleString('es-CR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    return sign ? sign + ' ' + formatted : formatted;
}
```

`utilities.js` is already included in `_Layout.cshtml` (verify this) so `formatMoney` will be globally available. If it is not in the layout, add it.

---

### TASK-06: Currencies jsGrid — add Sign field

**File**: `MariCamiStore/wwwroot/js/pages/currencies/index.js`

Add `sign` field to the `fields` array in `jsGrid` init (after `abbreviation`):

```javascript
fields: [
    { name: 'id', type: 'text', visible: false },
    { name: 'name', title: 'Nombre', type: 'text', width: 200, validate: 'required' },
    { name: 'abbreviation', title: 'Abreviación', type: 'text', width: 100, validate: 'required' },
    { name: 'sign', title: 'Signo', type: 'text', width: 80 },  // NEW
    { type: 'control' }
]
```

The `sign` field is sent automatically via `insertItem` / `updateItem` because jsGrid serializes all field values. The backend `OnPostInsertAsync` and `OnPostUpdateAsync` handlers already bind `[FromBody] Currency item`, and `Currency.Sign` is now a mapped property — no backend change needed for the CRUD.

---

### TASK-07: Sign resolution helper pattern (used in Tasks 08–12)

In each affected `PageModel`, add a private async method to resolve the local currency sign:

```csharp
private async Task<string> GetLocalCurrencySignAsync()
{
    var config = await catalogService.GetConfigurationAsync();
    if (config == null) return string.Empty;
    var currency = await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId);
    return currency?.Sign ?? string.Empty;
}
```

**Note**: `ICatalogService` does not currently have `GetCurrencyByIdAsync`. Add it:

- **`ICatalogService.cs`**: Add `Task<Currency?> GetCurrencyByIdAsync(Guid id);`
- **`CatalogService.cs`**: Add `public Task<Currency?> GetCurrencyByIdAsync(Guid id) => context.Currencies.FindAsync(id).AsTask();`

Alternatively (simpler, no interface change), use `context.Currencies.FindAsync(id)` directly in the PageModels. However, to keep clean architecture, add the method to `ICatalogService`.

For `Orders/Items.cshtml.cs` (which needs both local and order currency sign), also resolve the order's currency sign:
```csharp
var orderCurrency = await catalogService.GetCurrencyByIdAsync(Order!.CurrencyId);
var orderCurrencySign = orderCurrency?.Sign ?? string.Empty;
```

---

### TASK-08: Orders/Index — inject local sign

**File**: `MariCamiStore/Pages/Orders/Index.cshtml.cs`

`IndexModel` already injects `ICatalogService`. Change `OnGet` to async and resolve sign:

```csharp
public async Task<IActionResult> OnGetAsync()
{
    var guard = CheckOrganization();
    if (guard != null) return guard;
    ViewData["LocalCurrencySign"] = await GetLocalCurrencySignAsync();
    return Page();
}
```

**File**: `MariCamiStore/Pages/Orders/Index.cshtml`

Add before the `<script src="...">` tag:
```html
<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>
```

**File**: `MariCamiStore/wwwroot/js/pages/orders/index.js`

In `loadGrid`, update the jsGrid field definitions for monetary columns to use `itemTemplate`:

```javascript
{
    name: 'totalOfTheOrder',
    title: 'Total Orden',
    width: 120,
    itemTemplate: function (val) { return formatMoney(val, localCurrencySign); }
},
{
    name: 'estimatedProfitInLocal',
    title: 'Ganancia Est.',
    width: 120,
    itemTemplate: function (val) { return formatMoney(val, localCurrencySign); }
},
```

> jsGrid does not natively format `number` fields with custom locales. Switching from `type: 'number'` to using `itemTemplate` for display-only columns is the correct approach.

---

### TASK-09: Orders/Items — inject local + order signs

**File**: `MariCamiStore/Pages/Orders/Items.cshtml.cs`

In `OnGetAsync`, after loading `Order`, resolve both signs:

```csharp
public async Task<IActionResult> OnGetAsync(Guid orderId)
{
    var guard = CheckOrganization();
    if (guard != null) return guard;
    Order = await orderService.GetOrderAsync(orderId);
    if (Order == null) return NotFound();

    ViewData["LocalCurrencySign"] = await GetLocalCurrencySignAsync();
    var orderCurrency = await catalogService.GetCurrencyByIdAsync(Order.CurrencyId);
    ViewData["OrderCurrencySign"] = orderCurrency?.Sign ?? string.Empty;

    return Page();
}
```

**File**: `MariCamiStore/Pages/Orders/Items.cshtml`

Extend the existing `<script>` block at the bottom (currently has `orderId`, `isPending`, etc.):

```html
<script>
    var orderId = '@order.Id';
    var isPending = @(isPending ? "true" : "false");
    var orderStatus = '@order.Status';
    var exchangeRate = @order.ExchangeRate;
    var taxPercentage = @order.TaxPercentage;
    var shippingAmountIntern = @order.ShippingAmountIntern;
    var discountAmount = @order.DiscountAmount;
    var orderCurrencyId = '@order.CurrencyId';
    var localCurrencySign = '@ViewData["LocalCurrencySign"]';   // NEW
    var orderCurrencySign = '@ViewData["OrderCurrencySign"]';   // NEW
</script>
```

**File**: `MariCamiStore/wwwroot/js/pages/orders/items.js`

Update `recalcOrderHeader` to use `formatMoney` with `localCurrencySign`:
```javascript
$('#order-total-agreed').text(formatMoney(totalAgreedPriceInLocal, localCurrencySign));
$('#h-subtotal').text(formatMoney(totalWithoutTaxes, localCurrencySign));
$('#h-taxes').text(formatMoney(taxesAmount, localCurrencySign));
$('#h-ship-cr').text(formatMoney(shippingToCR, localCurrencySign));
$('#h-total-supplier').text(formatMoney(totalToSupplier, localCurrencySign));
$('#h-total').text(formatMoney(totalOrder, localCurrencySign));
$('#h-profit').text(formatMoney(profit, localCurrencySign));
```

Update `renderItemsTable` column rendering:
- **Local sign columns** (`serviceFeeInLocal`, `agreedPriceInLocal`):
  ```javascript
  $tr.append($('<td>').text(formatMoney(item.serviceFeeInLocal, localCurrencySign)));
  $tr.append($('<td>').text(formatMoney(item.agreedPriceInLocal, localCurrencySign)));
  ```
- **Order/provider sign columns** (`listPrice`, `listPriceTaxWithTax`, `estimateShipping`, `total`):
  ```javascript
  $tr.append($('<td>').text(formatMoney(item.listPrice, orderCurrencySign)));
  $tr.append($('<td>').text(formatMoney(item.listPriceTaxWithTax, orderCurrencySign)));
  $tr.append($('<td>').text(formatMoney(total, orderCurrencySign)));
  $tr.append($('<td>').text(formatMoney(item.estimateShipping, orderCurrencySign)));
  ```
- **Subtotal and Grand Total rows** in tbody: use `localCurrencySign`:
  ```javascript
  $('<td>').text(formatMoney(round2(groupSubtotal), localCurrencySign))
  $('<td>').text(formatMoney(round2(grandTotal), localCurrencySign))
  ```

Note: `realPrice` is an internal entry field that is NOT in the display scope per FR-005/FR-006. Leave it as `.toFixed(2)` for now, or apply `orderCurrencySign` if preferred for consistency. Based on spec FR-006 wording ("columnas de proveedor"), apply `orderCurrencySign` to `realPrice` as well since it's a provider-currency value.

---

### TASK-10: Payments/Index — inject local sign

**File**: `MariCamiStore/Pages/Payments/Index.cshtml.cs`

Add `ICatalogService catalogService` to constructor injection. Change `OnGet` to async:

```csharp
public class IndexModel(IPaymentService paymentService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        ViewData["LocalCurrencySign"] = await GetLocalCurrencySignAsync();
        return Page();
    }
    // ... rest unchanged
}
```

**File**: `MariCamiStore/Pages/Payments/Index.cshtml`

Add before `<script src="...">`:
```html
<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>
```

**File**: `MariCamiStore/wwwroot/js/pages/payments/index.js`

Replace `.toFixed(2)` with `formatMoney(...)`:

```javascript
// In customer change handler:
$('#balance-global').text(formatMoney(r.globalBalance, localCurrencySign));
$('#balance-org').text(formatMoney(r.orgBalance, localCurrencySign));

// In register payment success handler:
$('#balance-global').text(formatMoney(r.balance.globalBalance, localCurrencySign));
$('#balance-org').text(formatMoney(r.balance.orgBalance, localCurrencySign));

// In renderSaldos:
return '<tr><td>' + escapeHtml(r.customerName) + '</td><td class="text-right">'
    + formatMoney(r.balance, localCurrencySign) + '</td></tr>';
// ...
'<tr class="font-weight-bold"><td>Total</td><td class="text-right">'
    + formatMoney(total, localCurrencySign) + '</td></tr>'
```

---

### TASK-11: Transactions/Index — inject local sign

**File**: `MariCamiStore/Pages/Transactions/Index.cshtml.cs`

Add `ICatalogService catalogService` to constructor injection. Change `OnGet` to async:

```csharp
public class IndexModel(ITransactionService txService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        ViewData["LocalCurrencySign"] = await GetLocalCurrencySignAsync();
        return Page();
    }
    // ... rest unchanged
}
```

**File**: `MariCamiStore/Pages/Transactions/Index.cshtml`

Add before `<script src="...">`:
```html
<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>
```

**File**: `MariCamiStore/wwwroot/js/pages/transactions/index.js`

In `renderTransactions`, replace the `transactionAmount` cell:
```javascript
'<td class="text-right">' + formatMoney(r.transactionAmount, localCurrencySign) + '</td>' +
```

In the `totalRow`:
```javascript
'<td class="text-right">' + formatMoney(total, localCurrencySign) + '</td>' +
```

---

### TASK-12: ProductTypes/Index — inject local sign

ProductTypes use `serviceFeeInLocal` (local sign) and `estimateShipping` (order/currency sign — the currency is per product type, loaded from the currency dropdown). Since jsGrid renders both columns directly from data and we need to know which currency sign to use per row (each product type may have a different currency), the approach is:

- For `serviceFeeInLocal`: use `localCurrencySign` (this is always local currency).
- For `estimateShipping`: use the currency sign corresponding to the row's `currencyId`. The currency data is already loaded in `currencyItems` array (used for the currency dropdown). Extend `currencyItems` to include the `sign` field and look it up per row.

**File**: `MariCamiStore/Pages/ProductTypes/Index.cshtml.cs`

Add `ICatalogService` is already injected. Add `OnGet` async to resolve local sign:

```csharp
public class IndexModel(ICatalogService catalogService) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null
            ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId)
            : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;
        return Page();
    }
    // ... rest unchanged
}
```

Note: `ProductTypes/IndexModel` does not inherit `OrganizationPageModel`, so `GetLocalCurrencySignAsync()` is implemented inline (or the class is refactored to inherit it). Use inline for simplicity.

**File**: `MariCamiStore/Pages/ProductTypes/Index.cshtml`

Add before `<script src="...">`:
```html
<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>
```

**File**: `MariCamiStore/wwwroot/js/pages/product-types/index.js`

In the currency load callback, store sign on each currency item:
```javascript
$.get('/Currencies?handler=Load', function (data) {
    currencyItems = data.map(function (c) {
        return { id: c.id, text: c.abbreviation, sign: c.sign || '' };
    });
    initGrid();
});
```

In `initGrid`, update `serviceFeeInLocal` and `estimateShipping` fields to use `itemTemplate`:

```javascript
{
    name: 'serviceFeeInLocal',
    title: 'Servicio (Local)',
    width: 130,
    validate: 'required',
    itemTemplate: function (val) {
        return formatMoney(val, localCurrencySign);
    }
},
{
    name: 'estimateShipping',
    title: 'Envío Estimado',
    width: 130,
    validate: 'required',
    itemTemplate: function (val, item) {
        var curr = currencyItems.find(function (c) { return c.id === item.currencyId; });
        var sign = curr ? curr.sign : '';
        return formatMoney(val, sign);
    }
},
```

> jsGrid passes `(value, item)` to `itemTemplate`, so `item.currencyId` is available to look up the sign.

---

### TASK-13: Verify utilities.js is in _Layout.cshtml

**File**: Check `MariCamiStore/Pages/Shared/_Layout.cshtml` (or wherever scripts are bundled).

If `utilities.js` is not already referenced globally, add:
```html
<script src="/js/utilities.js"></script>
```
This must appear before any page-specific scripts that call `formatMoney`.

---

### TASK-14: ICatalogService — add GetCurrencyByIdAsync

**File**: `MariCamiStore/Services/ICatalogService.cs`

Add:
```csharp
Task<Currency?> GetCurrencyByIdAsync(Guid id);
```

**File**: `MariCamiStore/Services/CatalogService.cs`

Add:
```csharp
public async Task<Currency?> GetCurrencyByIdAsync(Guid id) =>
    await context.Currencies.FindAsync(id);
```

---

## Sequence of Tasks

Tasks are ordered to respect dependencies:

| # | Task | File(s) | Depends On |
|---|---|---|---|
| 01 | Add `Sign` to `Currency` model | `Currency.cs` | — |
| 02 | Map `Sign` in EF config + update seed | `CurrencyEntityTypeConfiguration.cs` | 01 |
| 03 | Generate + apply EF migration | Migration file | 02 |
| 04 | Create `AmountFormatter.cs` helper | `Helpers/AmountFormatter.cs` | 01 |
| 05 | Add `formatMoney()` to `utilities.js` | `utilities.js` | — |
| 06 | Verify `utilities.js` in layout | `_Layout.cshtml` | 05 |
| 07 | Add `GetCurrencyByIdAsync` to service | `ICatalogService.cs`, `CatalogService.cs` | 01 |
| 08 | Currencies jsGrid — add Sign field | `currencies/index.js` | 01, 03 |
| 09 | Orders/Index — sign injection + JS | `Orders/Index.cshtml(.cs)`, `orders/index.js` | 05, 07 |
| 10 | Orders/Items — sign injection + JS | `Orders/Items.cshtml(.cs)`, `orders/items.js` | 05, 07 |
| 11 | Payments/Index — sign injection + JS | `Payments/Index.cshtml(.cs)`, `payments/index.js` | 05, 07 |
| 12 | Transactions/Index — sign injection + JS | `Transactions/Index.cshtml(.cs)`, `transactions/index.js` | 05, 07 |
| 13 | ProductTypes/Index — sign injection + JS | `ProductTypes/Index.cshtml(.cs)`, `product-types/index.js` | 05, 07 |

---

## Acceptance Verification Checklist

After implementation:

1. Run `dotnet ef database update` — must succeed without errors.
2. Navigate to `/Currencies` — Sign column visible (`₡`, `$`). Edit a currency, change Sign, save — value persists.
3. Navigate to `/Orders` — `Total Orden` and `Ganancia Est.` columns show `₡ 45.000,00` style.
4. Navigate to `/Orders/Items?orderId=...` — header summary fields show local sign; table provider-currency columns show order sign.
5. Navigate to `/Payments` — Saldo Global and Saldo Org show local sign; saldos table rows and Total row show local sign.
6. Navigate to `/Transactions` — Monto column and Total row show local sign.
7. Navigate to `/ProductTypes` — `Servicio (Local)` shows local sign; `Envío Estimado` shows the sign of each row's currency.
8. Set a currency Sign to empty — amounts display without sign or extra space.
9. Set `LocalCurrencyId` config to a currency with no sign — all pages show unformatted numbers.
