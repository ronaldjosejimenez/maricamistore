# Contracts: Payment & Report Handlers

---

## Payment Registry — `Pages/Payments/Index`

### Get Customer Balance
`GET /Payments?handler=Balance&customerId={guid}`

Response:
```json
{
  "customerId": "guid",
  "customerName": "María González",
  "globalBalance": 54750.0,
  "orgBalance": 54750.0
}
```

`globalBalance` = Sum of all Charge transactions for this customer across ALL organizations minus all Payment and Void transactions.
`orgBalance` = Same calculation scoped to the active organization only.

### Register Payment
`POST /Payments?handler=RegisterPayment`

Request:
```json
{
  "customerId": "guid",
  "amount": 20000.0
}
```

Validation:
- `customerId` required
- `amount` > 0

Response:
```json
{
  "success": true,
  "globalBalance": 34750.0,
  "orgBalance": 34750.0
}
```

Error response:
```json
{ "success": false, "error": "El monto debe ser mayor a cero." }
```

**Side effect**: Creates one `Transaction` record:
```
Type: Payment
Amount: amount
CustomerId: customerId
OrganizationId: active org
SourceId: null
CurrencyId: LocalCurrencyId from Configuration
TransactionDate: server now (Costa Rica local time)
TransactionDescription: "Pago – {CustomerName}"
Status: Active
```

---

## Saldos Report — `Pages/Reports/Saldos`

**Rendered server-side** as a static HTML table on `OnGetAsync`. No AJAX.

Query logic:
```sql
SELECT c.Id, c.FirstName + ' ' + c.LastName AS FullName,
       SUM(CASE WHEN t.TransactionType = 'Charge'  THEN t.TransactionAmount ELSE 0 END)
     - SUM(CASE WHEN t.TransactionType = 'Payment' THEN t.TransactionAmount ELSE 0 END)
     - SUM(CASE WHEN t.TransactionType = 'Void'    THEN t.TransactionAmount ELSE 0 END)
       AS Balance
FROM Transactions t
JOIN Customers c ON t.CustomerId = c.Id
GROUP BY c.Id, c.FirstName, c.LastName
HAVING Balance > 0
ORDER BY Balance DESC
```

*Note*: Balance calculation is global (all organizations) — consistent with the customer-centric design (balances are not org-scoped).

Response shape (server-rendered table columns):
- Nombre completo
- Total Adeudado (formatted in local currency)
