# Handler Contracts: Pages/CxP/Index

**Page**: `/CxP/Index`
**PageModel**: `MariCamiStore.Pages.CxP.IndexModel`
**Base class**: `OrganizationPageModel`

---

## GET Handlers

### `OnGetAsync()` → `IActionResult`

Renders the page. Loads currency catalog and local currency sign for the view.

**Side effects**: None (read-only).

**Response**: Razor Page render or redirect if no org selected.

---

### `OnGetPeriodAsync()` → `JsonResult`

Returns indicators and metadata for the current open period.

**Returns**:
```json
{
  "periodId": "guid",
  "transactionMonth": 6,
  "transactionYear": 2026,
  "exchangeRate": 530.00,
  "porPagarPorMoneda": {
    "<currencyId>": { "currencyName": "Dólares", "sign": "$", "amount": 1500.00 }
  },
  "porPagarEnColones": 795000.00,
  "saldosPorCobrar": 245000.00,
  "pagosRealizados": 100000.00,
  "deudaAPagar": 695000.00,
  "enCuenta": 50000.00,
  "pendienteDeRecoger": 645000.00,
  "shippingCRPendientesDeAplicar": 53000.00,
  "posicion": -453000.00,
  "isClosed": false
}
```

**When no open period**: returns `{ "noPeriod": true }`.

---

### `OnGetEntriesAsync()` → `JsonResult`

Returns all CxP entries for the current open period, grouped by currency.

**Returns**:
```json
[
  {
    "currencyId": "guid",
    "currencyName": "Dólares",
    "sign": "$",
    "entries": [
      {
        "id": "guid",
        "reference": "Orden ABC",
        "type": "AutoActiva",
        "amount": 350.00,
        "createdAt": "2026-06-05T10:30:00Z"
      }
    ],
    "total": 350.00
  }
]
```

**When no open period**: returns `[]`.

---

## POST Handlers

### `OnPostInitPeriodAsync([FromBody] InitPeriodRequest)` → `JsonResult`

Creates the first `PeriodControl` when none exists.

**Request body**:
```json
{ "month": 6, "year": 2026, "exchangeRate": 530.00 }
```

**Validation**:
- `month` ∈ [1, 12]
- `year` ≥ 2020
- `exchangeRate` > 0
- No open period must exist

**Returns**:
```json
{ "success": true }
// or
{ "success": false, "error": "<message>" }
```

---

### `OnPostAddEntryAsync([FromBody] CreateManualCxPEntryRequest)` → `JsonResult`

Creates a manual `CxPEntry` in the open period.

**Request body**:
```json
{ "currencyId": "guid", "amount": 25000.00, "reference": "Factura proveedor local" }
```

**Validation**:
- Open period must exist and not be closed
- `currencyId` must exist in catalog
- `amount` > 0
- `reference` non-empty, max 500 chars

**Returns**:
```json
{ "success": true }
// or
{ "success": false, "error": "<message>" }
```

---

### `OnPostDeleteEntryAsync([FromBody] DeleteEntryRequest)` → `JsonResult`

Deletes a `CxPEntry` from the open period.

**Request body**:
```json
{ "entryId": "guid" }
```

**Validation**:
- Entry must belong to the current open period (not a closed period)
- Entry must exist

**Returns**:
```json
{ "success": true }
// or
{ "success": false, "error": "<message>" }
```

---

### `OnPostUpdatePeriodAsync([FromBody] UpdatePeriodFieldsRequest)` → `JsonResult`

Updates editable fields of the open period.

**Request body**:
```json
{ "exchangeRate": 535.00, "pagosRealizados": 150000.00, "enCuenta": 75000.00 }
```

**Validation**:
- Open period must exist and not be closed
- `exchangeRate` ≥ 0
- `pagosRealizados` ≥ 0
- `enCuenta` ≥ 0

**Returns**:
```json
{ "success": true }
// or
{ "success": false, "error": "<message>" }
```

---

### `OnPostClosePeriodAsync()` → `JsonResult`

Closes the current open period and creates the next one.

**Request body**: None (no params needed).

**Logic**:
1. Load open period
2. Calculate `DeudaAPagar = PorPagarEnColones - PagosRealizados`
3. Set `IsClosed = true`
4. Determine next month/year (if month=12, next is month=1 year+1)
5. Create new `PeriodControl` with `ExchangeRate` from `Configuration`
6. Create `CxPEntry` in new period: `Type=SaldoAnterior`, `Amount=DeudaAPagar`, `CurrencyId=LocalCurrencyId`

**Validation**:
- Open period must exist and not already be closed

**Returns**:
```json
{ "success": true }
// or
{ "success": false, "error": "<message>" }
```

---

## Transition Endpoint Change (existing — `Orders/Items.cshtml.cs`)

### `OnPostTransitionAsync` (existing handler, modified)

When `toStatus = "Delivered"`, the request body must include `actualShippingAmountToCR`.

**Modified request** (only relevant when toStatus = Delivered):
```json
{
  "orderId": "guid",
  "toStatus": "Delivered",
  "transitionDate": "2026-06-10T...",
  "notes": null,
  "justification": null,
  "actualShippingAmountToCR": 75.00
}
```

`actualShippingAmountToCR` defaults to 0 / `ShippingAmountToCR` when not provided (backward-compatible).
