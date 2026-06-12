# Contracts: Catalog CRUD Handlers

All catalog pages follow the same jsGrid handler pattern.
Anti-forgery token required on all POST handlers (injected by Razor Pages automatically).

---

## Common Pattern

```
GET  /Page                        → OnGetAsync()          → Page()
GET  /Page?handler=Load           → OnGetLoadAsync()       → JsonResult([])
POST /Page?handler=Insert         → OnPostInsertAsync()    → JsonResult(entity)
POST /Page?handler=Update         → OnPostUpdateAsync()    → JsonResult(entity)
POST /Page?handler=Delete         → OnPostDeleteAsync()    → JsonResult({success})
```

---

## Configurations

**Page**: `/Configurations`

`OnGetLoadAsync()` → `JsonResult<Configuration>`
```json
{ "id": "guid", "taxPercentage": 13.0, "exchangeRate": 530.0, "localCurrencyId": "guid" }
```

`OnPostInsertAsync(Configuration input)` / `OnPostUpdateAsync(Configuration input)`
- Upsert: if a record exists for the org, update it; otherwise insert
- Returns updated record

*No delete handler* — Configuration is a single system record per organization.

---

## Currencies

**Page**: `/Currencies`

`OnGetLoadAsync()` → `JsonResult<List<Currency>>`
```json
[{ "id": "guid", "code": "USD", "name": "US Dollar", "symbol": "$" }]
```

`OnPostInsertAsync(Currency input)` / `OnPostUpdateAsync(Currency input)` / `OnPostDeleteAsync(Guid id)`

---

## ProductTypes

**Page**: `/ProductTypes`

`OnGetLoadAsync()` → `JsonResult<List<ProductTypeDto>>`
```json
[{
  "id": "guid",
  "name": "Ropa",
  "description": "Prendas de vestir",
  "estimateShipping": 2500.0,
  "serviceFeeInLocal": 1500.0,
  "currencyId": "guid",
  "currencyCode": "USD"
}]
```

`OnPostInsertAsync` / `OnPostUpdateAsync` / `OnPostDeleteAsync`

---

## Suppliers

**Page**: `/Suppliers`

`OnGetLoadAsync()` → `JsonResult<List<Supplier>>`
```json
[{ "id": "guid", "name": "Amazon", "website": "https://amazon.com" }]
```

`OnPostInsertAsync` / `OnPostUpdateAsync` / `OnPostDeleteAsync`

---

## Customers

**Page**: `/Customers`

`OnGetLoadAsync()` → `JsonResult<List<Customer>>`
```json
[{ "id": "guid", "firstName": "María", "lastName": "González", "email": "m@g.com", "phone": "8888-0000" }]
```

`OnPostInsertAsync` / `OnPostUpdateAsync` / `OnPostDeleteAsync`
*Note*: No org filter — global entity.

---

## Session / Organization Selector

**Endpoint**: `POST /api/session/organization` (minimal API or Razor Page handler on Layout)

Request body: `{ "organizationId": "guid" }`
Response: `{ "success": true }`
Effect: Sets `HttpContext.Session["ActiveOrganizationId"] = organizationId.ToString()`
