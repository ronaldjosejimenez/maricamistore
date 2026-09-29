# Data Model: 014-cierre-mes-cxp

## PeriodControl (unchanged columns)

| Field | Rule (new/confirmed) |
|-------|----------------------|
| ExchangeRate | > 0 on update, init and close |
| PagosRealizados | ≥ 0; new period starts at 0 |
| EnCuenta | ≥ 0; new period = value confirmed in the preview (proposed max(0, EnCuenta − max(0, Deuda))) |
| IsClosed | at most one `false` per OrganizationId |

**New index** `IX_PeriodControls_OneOpenPerOrganization`: unique on `OrganizationId` filtered `[IsClosed] = 0`. Existing unique `(OrganizationId, TransactionMonth, TransactionYear)` kept.

Migration `OneOpenPeriodPerOrganization`: `CreateIndex(... unique: true, filter: "[IsClosed] = 0")`; Down drops it.

## CxPEntry "SaldoAnterior" (rule change only)

Created in the new period only when `max(0, max(0, Deuda) − EnCuenta) > 0`, amount = that value, currency = local (colones), reference "Saldo anterior".

## ClosePreview (not persisted)

- Closing: current period indicators.
- NewPeriod: month/year, exchange rate (edited or proposed), pagos 0, en cuenta (edited or proposed), saldo anterior (or none), indicators computed with the same formulas.
