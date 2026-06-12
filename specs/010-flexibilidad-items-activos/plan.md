# Implementation Plan: Flexibilidad de Ítems en Órdenes Activas

**Branch**: `010-flexibilidad-items-activos` | **Date**: 2026-06-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/010-flexibilidad-items-activos/spec.md`

---

## Summary

Implement item-level reassignment and price adjustment in non-Pending orders, with:
- A generic "Sin Cliente" customer (`IsGeneric = true`) for speculative inventory
- A new `ReasignarItemAsync` service operation that creates differential transactions (Void+Charge for customer change; Charge/Payment for price adjustment to same customer)
- Visibility of negative balances ("Crédito a favor") in the Saldos screen
- Exclusion of `IsGeneric` customers from the payment registration dropdown

---

## Technical Context

**Language/Version**: C# / .NET 8, ASP.NET Core Razor Pages

**Primary Dependencies**: EF Core 8 (SQL Server), AdminLTE 3 UI

**Storage**: SQL Server — `Customers`, `OrderItems`, `Orders`, `Transactions` tables

**Testing**: Manual verification via browser (no automated test suite exists)

**Target Platform**: Web application (self-hosted)

**Performance Goals**: Reassignment response < 3 seconds (SC-002)

**Constraints**: `TotalToPayToSupplier` and related supplier cost fields must never change during reassignment (FR-012, SC-005)

**Scale/Scope**: Small business — tens of customers, hundreds of orders

---

## Project Structure

### Documentation (this feature)

```text
specs/010-flexibilidad-items-activos/
├── plan.md              ← this file
├── research.md          ← Phase 0 output (complete)
├── data-model.md        ← Phase 1 output
├── contracts/
│   └── items-reassign-handlers.md  ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit-tasks)
```

### Source Code

```text
MariCamiStore/
├── Model/
│   └── Customer.cs                                         [MODIFY] Add IsGeneric field
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/
│   │   └── CustomerEntityTypeConfiguration.cs             [MODIFY] Add IsGeneric config
│   └── Migrations/
│       └── <timestamp>_AddCustomerIsGeneric.cs            [CREATE] EF migration
├── Services/
│   ├── IOrderService.cs                                    [MODIFY] Add ReasignarItemAsync
│   ├── IPaymentService.cs                                  [MODIFY] Add IsGeneric to SaldoReportRow
│   ├── OrderService.cs                                     [MODIFY] Implement ReasignarItemAsync
│   └── PaymentService.cs                                   [MODIFY] Update GetSaldosReportAsync
└── Pages/
    ├── Orders/
    │   ├── Items.cshtml                                    [MODIFY] Reasignar button + modal
    │   └── Items.cshtml.cs                                 [MODIFY] OnPostReasignarAsync handler
    └── Payments/
        └── Index.cshtml                                    [MODIFY] Saldos table + dropdown filter
```

---

## Phase 1 — Data Model

### 1.1 `Customer.IsGeneric` field

**File**: `MariCamiStore/Model/Customer.cs`

Add at end of class:
```csharp
public bool IsGeneric { get; set; } = false;
```

### 1.2 EF Configuration

**File**: `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CustomerEntityTypeConfiguration.cs`

After the `Email` property configuration, add:
```csharp
builder.Property(c => c.IsGeneric)
    .IsRequired()
    .HasDefaultValue(false);
```

Do **not** add "Sin Cliente" to `HasData` — the migration handles it as a safe upsert.

### 1.3 Migration

Run from repo root:
```powershell
dotnet ef migrations add AddCustomerIsGeneric --project MariCamiStore --startup-project MariCamiStore
```

Then **manually edit** the generated migration's `Up()` method:
- Keep the generated `AddColumn` call for `IsGeneric`
- Replace any auto-generated `InsertData` call with the idempotent SQL from `data-model.md §4`

Full `Up()` content:
```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsGeneric",
    schema: "dbo",
    table: "Customers",
    nullable: false,
    defaultValue: false);

migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6')
BEGIN
    INSERT INTO [dbo].[Customers]
        ([Id],[NickName],[Name],[PhoneNumber],[Address],[LocationLink],[Email],[IsGeneric])
    VALUES
        ('84828E82-81CA-437D-B2F0-B9877EF044C6','Sin Cliente','Sin Cliente','0000-0000',NULL,NULL,NULL,1)
END
ELSE
BEGIN
    UPDATE [dbo].[Customers]
    SET [IsGeneric] = 1
    WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6'
END
");
```

`Down()` only drops the column (does not delete "Sin Cliente").

### 1.4 `SaldoReportRow` extension

**File**: `MariCamiStore/Services/IPaymentService.cs`

```diff
-public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance);
+public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric);
```

---

## Phase 2 — Backend Services

### 2.1 `IOrderService` — Add interface method

**File**: `MariCamiStore/Services/IOrderService.cs`

Add to the interface (after `DeleteOrderAsync`):
```csharp
Task<(bool Success, string? Error)> ReasignarItemAsync(
    Guid itemId,
    Guid newCustomerId,
    decimal newAgreedPriceInLocal);
```

### 2.2 `OrderService` — Implement `ReasignarItemAsync`

**File**: `MariCamiStore/Services/OrderService.cs`

Add the following method. All transaction fields match the pattern in `BuildTransaction` (line 342).

```csharp
public async Task<(bool Success, string? Error)> ReasignarItemAsync(
    Guid itemId, Guid newCustomerId, decimal newAgreedPriceInLocal)
{
    var item = await context.OrderItems.FindAsync(itemId);
    if (item == null) return (false, "Ítem no encontrado.");

    var order = await context.Orders.FindAsync(item.OrderId);
    if (order == null) return (false, "Orden no encontrada.");

    var allowed = new[] {
        OrderStatus.Active.Key, OrderStatus.Delivering.Key,
        OrderStatus.Delivered.Key, OrderStatus.Completed.Key
    };
    if (!allowed.Contains(order.Status))
        return (false, $"No se puede reasignar en estado {order.Status}.");

    bool customerChanged = item.CustomerId != newCustomerId;
    bool priceChanged    = item.AgreedPriceInLocal != newAgreedPriceInLocal;
    if (!customerChanged && !priceChanged)
        return (true, null); // no-op (FR-009)

    var config  = context.Configurations.FirstOrDefault();
    var txDate  = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                      DateTime.UtcNow, "Central America Standard Time");
    var truncDesc = item.ProductDescription.Length > 100
                      ? item.ProductDescription[..100]
                      : item.ProductDescription;

    if (customerChanged)
    {
        // FR-006: Void old customer's charge + Charge new customer
        var description = $"Reasignación – {order.NameOfOrder} – {truncDesc}";

        context.Transactions.Add(new Transaction
        {
            Id                      = Guid.NewGuid(),
            OrganizationId          = order.OrganizationId,
            SourceId                = item.Id,
            Source                  = TransactionSource.OrderItem.Key,
            CustomerId              = item.CustomerId,
            TransactionType         = TransactionType.Void.Key,
            TransactionDescription  = description,
            TransactionAmount       = item.AgreedPriceInLocal,
            TransactionDate         = txDate,
            Status                  = TransactionStatus.Applied.Key,
            CurrencyId              = config?.LocalCurrencyId ?? Guid.Empty
        });

        context.Transactions.Add(new Transaction
        {
            Id                      = Guid.NewGuid(),
            OrganizationId          = order.OrganizationId,
            SourceId                = item.Id,
            Source                  = TransactionSource.OrderItem.Key,
            CustomerId              = newCustomerId,
            TransactionType         = TransactionType.Charge.Key,
            TransactionDescription  = description,
            TransactionAmount       = newAgreedPriceInLocal,
            TransactionDate         = txDate,
            Status                  = TransactionStatus.Applied.Key,
            CurrencyId              = config?.LocalCurrencyId ?? Guid.Empty
        });

        item.CustomerId = newCustomerId;
    }
    else // same customer, price changed
    {
        var diff      = Math.Abs(newAgreedPriceInLocal - item.AgreedPriceInLocal);
        bool isUp     = newAgreedPriceInLocal > item.AgreedPriceInLocal;
        var txType    = isUp ? TransactionType.Charge.Key : TransactionType.Payment.Key;
        var txDesc    = isUp
                          ? $"Ajuste de precio – {truncDesc}"           // FR-007
                          : $"Descuento por ajuste de precio – {truncDesc}"; // FR-008

        context.Transactions.Add(new Transaction
        {
            Id                      = Guid.NewGuid(),
            OrganizationId          = order.OrganizationId,
            SourceId                = item.Id,
            Source                  = TransactionSource.OrderItem.Key,
            CustomerId              = item.CustomerId,
            TransactionType         = txType,
            TransactionDescription  = txDesc,
            TransactionAmount       = diff,
            TransactionDate         = txDate,
            Status                  = TransactionStatus.Applied.Key,
            CurrencyId              = config?.LocalCurrencyId ?? Guid.Empty
        });
    }

    item.AgreedPriceInLocal = newAgreedPriceInLocal;
    item.UpdatedAt          = DateTime.UtcNow;

    // FR-010/011: Recalculate agreed-price totals ONLY (supplier totals unchanged)
    var otherTotal = await context.OrderItems
        .Where(i => i.OrderId == item.OrderId && i.Id != itemId)
        .SumAsync(i => i.AgreedPriceInLocal);
    order.TotalAgreedPriceInLocal = Math.Round(otherTotal + newAgreedPriceInLocal, 2);
    order.EstimatedProfitInLocal  = Math.Round(
        order.TotalAgreedPriceInLocal - order.TotalOfTheOrder * order.ExchangeRate, 2);
    order.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();
    return (true, null);
}
```

### 2.3 `PaymentService` — Update `GetSaldosReportAsync`

**File**: `MariCamiStore/Services/PaymentService.cs`

Two changes in `GetSaldosReportAsync()`:

**Change 1** — filter `Balance != 0` (was `> 0`):
```diff
-        .Where(r => r.Balance > 0)
+        .Where(r => r.Balance != 0)
```

**Change 2** — include `IsGeneric` in customer dictionary and `SaldoReportRow`:
```csharp
// Replace:
var customers = await context.Customers
    .Where(c => customerIds.Contains(c.Id))
    .ToDictionaryAsync(c => c.Id, c => c.NickName ?? c.Name ?? c.Id.ToString());

return rows.Select(r => new SaldoReportRow(
    r.CustomerId,
    customers.GetValueOrDefault(r.CustomerId, r.CustomerId.ToString()),
    r.Balance
)).OrderBy(r => r.CustomerName).ToList();

// With:
var customers = await context.Customers
    .Where(c => customerIds.Contains(c.Id))
    .ToDictionaryAsync(c => c.Id, c => new {
        Name      = c.NickName != "" ? c.NickName : (c.Name ?? c.Id.ToString()),
        IsGeneric = c.IsGeneric
    });

return rows.Select(r => {
    var cust = customers.GetValueOrDefault(r.CustomerId);
    return new SaldoReportRow(
        r.CustomerId,
        cust?.Name ?? r.CustomerId.ToString(),
        r.Balance,
        cust?.IsGeneric ?? false
    );
}).OrderBy(r => r.CustomerName).ToList();
```

---

## Phase 3 — Frontend: Customer Dropdown Filtering

### 3.1 Locate customer list endpoint

During implementation, identify the AJAX endpoint that populates:
- The customer dropdown in the item add/edit modal (Items.cshtml)
- The customer dropdown in the payment registration form (Payments/Index.cshtml)

Likely candidates: a handler in a shared page or `ICatalogService`. Search for `OnGetCustomers` or similar.

### 3.2 Item modal dropdown — include "Sin Cliente"

The item add/edit modal dropdown must include all customers (including `IsGeneric = true`). "Sin Cliente" appears in alphabetical position "S" (FR-002). No filtering required for this dropdown.

If the endpoint currently filters out "Sin Cliente" for any reason, ensure it is included.

### 3.3 Payment form dropdown — exclude IsGeneric customers

The customer dropdown in the payment registration form must exclude customers where `IsGeneric = true` (FR-003).

Approach: In the endpoint that serves customers for the payment form, add a `.Where(c => !c.IsGeneric)` filter, or add a boolean parameter (`excludeGeneric`) to distinguish the two use cases.

---

## Phase 4 — Frontend: Orders/Items Page

### 4.1 "Reasignar" button (Items.cshtml)

Add a "Reasignar" button to each row in the items table. The button is visible **only** when `Order.Status ∈ {Active, Delivering, Delivered, Completed}` (FR-013).

```html
@if (Model.Order.Status != "Pending" && Model.Order.Status != "Voided")
{
    <button class="btn btn-xs btn-warning btn-reasignar"
            data-item-id="@item.Id"
            data-customer-id="@item.CustomerId"
            data-customer-name="@item.CustomerDisplayName"
            data-price="@item.AgreedPriceInLocal">
        Reasignar
    </button>
}
```

The `data-*` attributes pre-populate the modal.

### 4.2 Reassignment modal (Items.cshtml)

Add one modal to the page (shared across all row buttons):

```html
<div class="modal fade" id="modalReasignar" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Reasignar Ítem</h5>
        <button type="button" class="close" data-dismiss="modal">&times;</button>
      </div>
      <div class="modal-body">
        <input type="hidden" id="reasignarItemId" />
        <div class="form-group">
          <label>Cliente</label>
          <select class="form-control" id="reasignarCustomerId"></select>
        </div>
        <div class="form-group">
          <label>Precio Acordado</label>
          <input type="number" class="form-control" id="reasignarPrecio" min="0" step="0.01" />
        </div>
        <div class="alert alert-warning d-none" id="reasignarPrecioWarning">
          El precio es ₡0. ¿Está seguro que desea continuar?
        </div>
        <div class="text-danger" id="reasignarError"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
        <button class="btn btn-warning" id="btnConfirmarReasignar">Confirmar</button>
      </div>
    </div>
  </div>
</div>
```

### 4.3 `OnPostReasignarAsync` handler (Items.cshtml.cs)

Add request record and handler:
```csharp
public record ReasignarRequest(Guid ItemId, Guid NewCustomerId, decimal NewAgreedPriceInLocal);

public async Task<JsonResult> OnPostReasignarAsync([FromBody] ReasignarRequest request)
{
    var (success, error) = await orderService.ReasignarItemAsync(
        request.ItemId, request.NewCustomerId, request.NewAgreedPriceInLocal);
    return new JsonResult(new { success, error });
}
```

### 4.4 JavaScript logic (Items.cshtml)

Add JS (in `@section Scripts` or inline):

```javascript
// Populate customer dropdown once (or on modal open)
async function loadCustomersForReasignar() {
    // Reuse whatever customer-list endpoint already exists;
    // no IsGeneric filtering needed here — "Sin Cliente" must appear
    const resp = await fetch('/path/to/customer-list');
    const customers = await resp.json();
    const sel = document.getElementById('reasignarCustomerId');
    sel.innerHTML = customers.map(c =>
        `<option value="${c.id}">${c.displayName}</option>`).join('');
}

// Open modal on button click
document.querySelectorAll('.btn-reasignar').forEach(btn => {
    btn.addEventListener('click', async () => {
        document.getElementById('reasignarItemId').value = btn.dataset.itemId;
        document.getElementById('reasignarPrecio').value = btn.dataset.price;
        document.getElementById('reasignarError').textContent = '';
        document.getElementById('reasignarPrecioWarning').classList.add('d-none');
        await loadCustomersForReasignar();
        // Set current customer as selected
        document.getElementById('reasignarCustomerId').value = btn.dataset.customerId;
        $('#modalReasignar').modal('show');
    });
});

// Price=0 warning (FR-015)
document.getElementById('reasignarPrecio').addEventListener('change', function() {
    const warn = document.getElementById('reasignarPrecioWarning');
    this.value === '0' || this.value === ''
        ? warn.classList.remove('d-none')
        : warn.classList.add('d-none');
});

// Confirm button
document.getElementById('btnConfirmarReasignar').addEventListener('click', async () => {
    const precio = parseFloat(document.getElementById('reasignarPrecio').value);
    if (isNaN(precio) || precio < 0) {
        document.getElementById('reasignarError').textContent = 'Precio inválido.';
        return;
    }
    // Price=0 gate: warning must be visible and user clicked Confirmar (implicit acceptance)

    const payload = {
        itemId: document.getElementById('reasignarItemId').value,
        newCustomerId: document.getElementById('reasignarCustomerId').value,
        newAgreedPriceInLocal: precio
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const resp = await fetch('?handler=Reasignar', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(payload)
    });
    const data = await resp.json();

    if (data.success) {
        $('#modalReasignar').modal('hide');
        loadItems(); // reuse existing reload function
    } else {
        document.getElementById('reasignarError').textContent = data.error ?? 'Error desconocido.';
    }
});
```

---

## Phase 5 — Frontend: Payments/Index Saldos

### 5.1 Saldos table — negative balances + IsGeneric label

The JS that renders the saldos table must:

1. **Show all rows** where `balance != 0` (the backend already filters this after Phase 2)
2. **Positive balance row** → badge `"Saldo pendiente"` (existing style, no change)
3. **Negative balance row** → badge `"Crédito a favor"` with a distinct color (e.g., `badge-success` or `text-success`)
4. **IsGeneric customer row** → append `" (Especulativo)"` to the customer name cell (FR-018)

Example JS update in the saldos render loop:
```javascript
function renderSaldoRow(row) {
    const isNegative = row.balance < 0;
    const badge = isNegative
        ? '<span class="badge badge-success">Crédito a favor</span>'
        : '<span class="badge badge-warning">Saldo pendiente</span>';
    const name = row.isGeneric
        ? `${row.customerName} <em>(Especulativo)</em>`
        : row.customerName;
    const balanceDisplay = Math.abs(row.balance).toLocaleString('es-CR', {
        style: 'currency', currency: 'CRC'
    });
    return `<tr class="${isNegative ? 'table-success' : ''}">
        <td>${name}</td>
        <td>${isNegative ? '-' : ''}${balanceDisplay}</td>
        <td>${badge}</td>
    </tr>`;
}
```

### 5.2 Payment form dropdown — exclude IsGeneric

In the customer dropdown for payment registration, ensure the list endpoint used excludes `IsGeneric = true` customers (Phase 3.3 result). No UI change needed beyond pointing to the filtered endpoint.

---

## Execution Order

```
Phase 1 (Data model) → Phase 2 (Backend) → Phase 3 (Dropdown filtering) → Phase 4 (Items UI) → Phase 5 (Saldos UI)
```

Dependencies:
- Phase 2 requires Phase 1 to compile (Customer.IsGeneric must exist)
- Phase 3 can run in parallel with Phase 4 and 5 after Phase 2 is complete
- Phase 4 and 5 require Phase 2 (handlers + service changes)
- All phases must complete before end-to-end testing

---

## Verification Checklist

After implementation, verify each FR manually:

- [ ] FR-001: Run app fresh, check "Sin Cliente" exists in Customers table with `IsGeneric = 1`
- [ ] FR-002: Open item add/edit modal → "Sin Cliente" appears in customer dropdown at alphabetical position "S"
- [ ] FR-003: Open payment registration form → "Sin Cliente" is NOT in customer dropdown
- [ ] FR-004/005: "Reasignar" button visible on Active/Delivering/Delivered/Completed items; absent on Pending/Voided
- [ ] FR-006: Reassign item from Customer A to Customer B → Void(A, old price) + Charge(B, new price) in Transactions table
- [ ] FR-007: Same customer, increase price → Charge(diff) with "Ajuste de precio" description
- [ ] FR-008: Same customer, decrease price → Payment(diff) with "Descuento" description
- [ ] FR-009: No change → no new transactions, no error
- [ ] FR-010/011: Order `TotalAgreedPriceInLocal` and `EstimatedProfitInLocal` updated after reassignment
- [ ] FR-012: `TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount` unchanged
- [ ] FR-013/014: "Reasignar" button opens modal with prefilled customer + price
- [ ] FR-015: Price = 0 shows warning before confirm
- [ ] FR-016: Saldos table shows both positive and negative balances; balance = 0 not shown
- [ ] FR-017: Single table, alphabetical by customer name; negative rows show "Crédito a favor" with distinct style
- [ ] FR-018: "Sin Cliente" shows "(Especulativo)" in saldos table
