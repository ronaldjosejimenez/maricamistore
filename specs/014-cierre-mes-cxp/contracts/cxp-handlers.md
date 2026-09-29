# CxP Handler Contracts: 014-cierre-mes-cxp

All on `/CxP` (IndexModel). POSTs require the antiforgery header.

## GET ?handler=ClosePreview&periodId={guid}[&exchangeRate={n}][&enCuenta={n}]
Returns (no writes):
```json
{
  "closing": { /* CxPPeriodIndicatorsDto: porPagarEnColones, porPagarPorMoneda, saldosPorCobrar, shippingCRPendientesDeAplicar,
                 deudaAPagar, enCuenta, pendienteDeRecoger, posicion, exchangeRate, pagosRealizados, transactionMonth, transactionYear */ },
  "newPeriod": {
    "month": 10, "year": 2026,
    "exchangeRate": 520.0, "proposedExchangeRate": 520.0,
    "pagosRealizados": 0,
    "enCuenta": 200000.0, "proposedEnCuenta": 200000.0,
    "saldoAnterior": null,
    "indicators": { /* same shape as closing indicators, for the new month */ }
  },
  "errors": [ "El tipo de cambio debe ser mayor a cero." ]
}
```
If the period is not open: `{ "error": "Este mes ya fue cerrado.", "alreadyClosed": true }`.

## POST ?handler=ClosePeriod
Body: `{ "periodId": "…", "exchangeRate": 520.0, "enCuenta": 200000.0 }`
Response: `{ "success": true }` | `{ "success": false, "error": "El tipo de cambio debe ser mayor a cero." | "El valor no puede ser negativo." | "Este mes ya fue cerrado.", "alreadyClosed": true|false }`

## POST ?handler=UpdatePeriod (changed validation)
TC ≤ 0 → "El tipo de cambio debe ser mayor a cero."; Pagos/EnCuenta < 0 → "El valor no puede ser negativo."

## POST ?handler=InitPeriod (message aligned)
TC ≤ 0 → "El tipo de cambio debe ser mayor a cero."
