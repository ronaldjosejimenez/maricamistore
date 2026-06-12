# Code Review: Módulo Cuentas por Pagar (CxP)

**Spec:** `specs/009-cuentas-por-pagar/spec.md`
**Date:** 2026-06-10
**Reviewer:** Claude (speckit.spex-gates.review-code)
**Result:** GATE PASS — 100% compliance after fix

## Compliance Summary

**Overall Score: 100%** (after FR-005 fix applied in this session)

| Category | Compliant | Total | % |
|---|---|---|---|
| Functional Requirements | 19 | 19 | 100% |
| Edge Cases | 7 | 7 | 100% |
| Acceptance Scenarios | 20 | 20 | 100% |
| Success Criteria | 5 | 5 | 100% |

---

## Detailed Review

### Functional Requirements

| FR | Description | Status | Location |
|---|---|---|---|
| FR-001 | PeriodControl table: month, year, TC, pagos, en cuenta, IsClosed | ✅ Compliant | Model/PeriodControl.cs |
| FR-002 | "período actual" = unique IsClosed=false | ✅ Compliant | CxPService.GetOpenPeriodAsync:17 |
| FR-003 | Close: mark closed, DeudaAPagar, new period, SaldoAnterior entry | ✅ Compliant | CxPService.ClosePeriodAsync:56–103 |
| FR-004 | Closed period blocks entry create/edit/delete | ✅ Compliant | CreateManualEntryAsync:222, DeleteEntryAsync:256, UpdatePeriodFieldsAsync:47 |
| FR-005 | TC defaults to Configuration.ExchangeRate | ✅ Fixed | OnGetAsync: ViewData["InitExchangeRate"]; Index.cshtml #init-tc value attribute |
| FR-006 | Manual entry: reference, currency, amount > 0 | ✅ Compliant | CxPService.CreateManualEntryAsync + modal |
| FR-007 | Active order → AutoActiva entry | ✅ Compliant | OrderService.TransitionOrderAsync + CreateAutoEntryAsync |
| FR-008 | Delivered dialog shows Shipping real a CR pre-filled | ✅ Compliant | Orders/Index.cshtml modal + orders/index.js:openTransitionModal |
| FR-009 | AutoDelivered entry with actual shipping, order currency | ✅ Compliant | OrderService Delivered block |
| FR-010 | ShippingAmountToCR (existing) not modified | ✅ Compliant | Order.cs:82 new field; original untouched |
| FR-011 | No open period → complete transition, log, no CxP entry | ✅ Compliant | OrderService try/catch + LogWarning |
| FR-012 | All 9 indicators computed (PorPagarPorMoneda, PorPagarEnColones, SaldosCobrar, PagosRealizados, DeudaAPagar, EnCuenta, PendienteDeRecoger, ShippingCRPendientes, Posición) | ✅ Compliant | CxPService.GetPeriodIndicatorsAsync:107–186 |
| FR-013 | Posición in bold + visually larger | ✅ Compliant | `<h3 class="font-weight-bold" id="posicion-value">` |
| FR-014 | TC, PagosRealizados, EnCuenta editable; disabled when closed | ✅ Compliant | index.js:87 `.prop('disabled', !isOpen)` |
| FR-015 | Panel (top) + per-currency tables (below) | ✅ Compliant | #cxp-panel + #cxp-tables-container |
| FR-016 | Columns: reference, type label, amount+sign, date, delete | ✅ Compliant | index.js TYPE_LABELS + table rows |
| FR-017 | Nav "Cuentas por Pagar" → /CxP | ✅ Compliant | _Layout.cshtml menuCxP section |
| FR-018 | Delete any entry from open period | ✅ Compliant | DeleteEntryAsync no type restriction |
| FR-019 | Init form: Mes, Año, TC; creates first PeriodControl | ✅ Compliant | #cxp-init-section + OnPostInitPeriodAsync |

### Edge Cases

| Edge Case | Status |
|---|---|
| No open period → show inline init form | ✅ loadPeriod() noPeriod branch |
| Delete SaldoAnterior → allowed, recalculates | ✅ No type restriction on delete |
| ActualShippingAmountToCR = 0 → entry created with 0 | ✅ No min validation on auto entries |
| TC = 0 → conversion fields = 0 + warning | ✅ ExchangeRateWarning + #tc-warning |
| Negative DeudaAPagar → close proceeds | ✅ No blocking check in ClosePeriodAsync |
| Empty period close → SaldoAnterior = 0 | ✅ Math produces 0 |
| CxP error during transition → order completes | ✅ try/catch in TransitionOrderAsync |

### Acceptance Scenarios

All 20 acceptance scenarios across US1–US5 verified compliant.

---

## Fix Applied During Review

**FR-005 — Init form default TC**

The `#init-tc` field in the init form was not pre-populated from `Configuration.ExchangeRate`.

Fix applied:
- `Pages/CxP/Index.cshtml.cs` OnGetAsync: added `ViewData["InitExchangeRate"] = config?.ExchangeRate ?? 0m`
- `Pages/CxP/Index.cshtml` `#init-tc`: added `value="@ViewData["InitExchangeRate"]"`

---

## Code Quality Notes

- `ILogger<CxPService>` injected but unused (CS9113 warning) — logger is used in OrderService; CxPService doesn't need it but the injection is harmless
- `GetOpenPeriodAsync()` called per handler invocation without caching — acceptable at current scale
- `CreateAutoEntryAsync` does not validate period is open — acceptable, internal method called only after period verified by caller

---

## Conclusion

**GATE PASS.** All 19 FRs, 7 edge cases, 20 acceptance scenarios, and 5 success criteria are correctly implemented. One minor deviation (FR-005 TC default) was identified and fixed during this review session. Build succeeds with 0 errors.
