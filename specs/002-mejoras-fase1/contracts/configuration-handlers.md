# Contracts: Configuration & Currency Display

## `Pages/Configurations/Index` — UI Behavior Changes

### Load Configuration
`GET /Configurations?handler=Load` (existing, unchanged)

Response (config exists):
```json
[{
  "id": "guid",
  "organizationId": "guid",
  "taxPercentage": 13.0,
  "exchangeRate": 530.0,
  "exchangeRateMargin": 2.5,
  "localCurrencyId": "guid",
  "orderCurrencyIdDefault": "guid",
  "productTypeIdDefault": "guid or null"
}]
```

Response (no config for this org):
```json
[]
```

### Upsert Configuration
`POST /Configurations?handler=Upsert` (existing, unchanged)

Request: configuration object (same shape as load)
Response: upserted configuration object

### UI Behavior — Insert vs Edit Mode
- When `loadData` returns an empty array: show jsGrid with `inserting: true` (allow creating the first config)
- When `loadData` returns one record: set jsGrid `inserting: false` (only editing is allowed)
- Display a banner/message when in edit-only mode: "Esta organización ya tiene una configuración. Solo puede editarla."

---

## Currency Columns — Select Field Contract

### Currencies Endpoint (existing)
`GET /Currencies?handler=Load`

Response shape used by jsGrid select fields:
```json
[{ "id": "guid", "name": "Colón Costarricense", "abbreviation": "CRC" }]
```

### In `configurations/index.js`
Replace `localCurrencyId` and `orderCurrencyIdDefault` text columns with jsGrid `select` fields:
```javascript
{
  name: 'localCurrencyId',
  title: 'Moneda Local',
  type: 'select',
  items: currencyItems,   // [{ id: 'guid', text: 'CRC' }, ...]
  valueField: 'id',
  textField: 'text',
  width: 120
}
```
`currencyItems` is loaded from `/Currencies?handler=Load` on page init, mapped to `{ id, text: abbreviation }`.

### In `product-types/index.js`
Replace `currencyId` text column with jsGrid `select` field using the same `currencyItems` array.
