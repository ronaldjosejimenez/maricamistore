# Contracts: Order & OrderItem Handlers

---

## Order Dashboard — `Pages/Orders/Index`

### Load Orders
`GET /Orders?handler=Load&statusFilter=PendingActive`

Response:
```json
[{
  "id": "guid",
  "nameOfOrder": "Orden Amazon Jun",
  "supplierName": "Amazon",
  "status": "Pending",
  "statusLabel": "Pendiente",
  "totalOfTheOrder": 45000.0,
  "estimatedProfitInLocal": 12000.0,
  "createdAt": "2026-06-03T10:00:00"
}]
```

### Create Order
`POST /Orders?handler=Create`

Request:
```json
{
  "nameOfOrder": "Orden Amazon Jun",
  "supplierId": "guid",
  "exchangeRate": 530.0,
  "taxPercentage": 13.0,
  "shippingAmountIntern": 0.0,
  "discountAmount": 0.0
}
```

Response: created order object (same shape as load item).

### Transition Status
`POST /Orders?handler=Transition`

Request:
```json
{
  "orderId": "guid",
  "toStatus": "Active",
  "transitionDate": "2026-06-03",
  "notes": "optional notes",
  "justification": "required only for Voided"
}
```

Response:
```json
{ "success": true, "newStatus": "Active", "newStatusLabel": "Activa" }
```

Error response (validation failure):
```json
{ "success": false, "error": "Se requiere justificación para anular una orden." }
```

---

## Order Items Editor — `Pages/Orders/Items`

URL: `/Orders/Items?orderId={guid}`

### Load Items
`GET /Orders/Items?handler=Load&orderId={guid}`

Response:
```json
[{
  "id": "guid",
  "customerId": "guid",
  "customerName": "María González",
  "productDescription": "Vestido floral",
  "productLink": "https://...",
  "productSourceCode": "B08XYZ",
  "productTypeId": "guid",
  "productTypeName": "Ropa",
  "listPrice": 25.00,
  "listPriceTax": 3.25,
  "realPrice": 22.00,
  "estimateShipping": 2500.0,
  "serviceFeeInLocal": 1500.0,
  "agreedPriceInLocal": 18225.0
}]
```

### Get ProductType values (for auto-fill)
`GET /Orders/Items?handler=ProductType&id={productTypeId}`

Response:
```json
{ "estimateShipping": 2500.0, "serviceFeeInLocal": 1500.0, "currencyId": "guid" }
```

### Insert Item
`POST /Orders/Items?handler=Insert`

Request: item fields (all from the grid row)
Response: created item object

### Update Item
`POST /Orders/Items?handler=Update`

Request: item fields including `id`
Response: updated item object

### Delete Item
`POST /Orders/Items?handler=Delete`

Request: `{ "id": "guid" }`
Response: `{ "success": true }`

### Update Order Header Totals (called after any item change)
`POST /Orders/Items?handler=UpdateTotals`

Request:
```json
{
  "orderId": "guid",
  "shippingAmountToCR": 7500.0,
  "totalWithoutTaxes": 66.0,
  "taxesAmount": 9.75,
  "totalToPayToSupplier": 75.75,
  "totalOfTheOrder": 83.25,
  "estimatedProfitInLocal": 54675.0
}
```

Response: `{ "success": true }`
