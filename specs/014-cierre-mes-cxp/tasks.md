---
description: "Task list for 014-cierre-mes-cxp"
---

# Tasks: Cierre de Mes de CxP — Reglas, Vista Previa y Protecciones

**Input**: `specs/014-cierre-mes-cxp/` (plan.md, spec.md, data-model.md, contracts/cxp-handlers.md, research.md, quickstart.md)

**Tests**: No automated test project; verification = `dotnet build` + quickstart.md (manual).

**Paths**: relative to repository root; web project in `MariCamiStore/`. **Never run `dotnet ef database update`.**

Stories: US1 carry-over rules (P1), US2 preview dialog (P1), US3 validations (P2), US4 double-close (P2), US5 panel visuals (P3).

---

## Phase 1: Setup

- [ ] T001 Run `dotnet build MariCamiStore/MariCamiStore.csproj` to confirm a clean baseline (note existing warnings).

## Phase 2: Foundational (shared math; blocks US1/US2)

- [ ] T002 In `MariCamiStore/Services/CxPService.cs` extract from `GetPeriodIndicatorsAsync` (plan D1): a private `Task<decimal> ComputeShippingPendingAsync(decimal exchangeRate, Guid localCurrencyId)` containing the existing Active+Delivering per-order `max(0, ShippingAmountToCR − Σ packages)` logic (returns 0 when exchangeRate == 0), and a private static `(decimal Deuda, decimal Pendiente, decimal Posicion) ComputeDerived(decimal porPagarEnColones, decimal pagos, decimal enCuenta, decimal saldosPorCobrar, decimal shipping)`. Make `GetPeriodIndicatorsAsync` use both; its output MUST stay identical (FR-001).
- [ ] T003 In `MariCamiStore/Services/CxPService.cs` add `internal static (decimal SaldoAnterior, decimal ProposedEnCuenta) CarryOver(decimal deuda, decimal enCuenta)` per plan D2 (`debt = max(0, deuda)`; `saldo = max(0, debt − enCuenta)`; `enCuentaNueva = max(0, enCuenta − debt)`), and a private helper `ProposedExchangeRateAsync(PeriodControl period)` returning `config.ExchangeRate > 0 ? config.ExchangeRate : period.ExchangeRate`.
- [ ] T004 In `MariCamiStore/Services/ICxPService.cs` add `public static class CxPMessages { public const string ExchangeRateMustBePositive = "El tipo de cambio debe ser mayor a cero."; public const string ValueCannotBeNegative = "El valor no puede ser negativo."; public const string PeriodAlreadyClosed = "Este mes ya fue cerrado."; }`.

## Phase 3: US1 — Carry-over rules (P1)

**Independent Test**: quickstart.md §4 + §6.

- [ ] T005 [US1] In `MariCamiStore/Services/ICxPService.cs` change `Task ClosePeriodAsync(Guid periodId)` to `Task ClosePeriodAsync(Guid periodId, decimal newExchangeRate, decimal newEnCuenta)`.
- [ ] T006 [US1] Rewrite `ClosePeriodAsync` in `MariCamiStore/Services/CxPService.cs` per plan D4: validate `newExchangeRate > 0` / `newEnCuenta >= 0` (throw `InvalidOperationException` with `CxPMessages`); load period by id — missing or closed → throw `PeriodAlreadyClosed`; recompute indicators; `CarryOver(indicators.DeudaAPagar, period.EnCuenta)`; close period; add new period (next month/year, `ExchangeRate = newExchangeRate`, `PagosRealizados = 0`, `EnCuenta = newEnCuenta`); add `SaldoAnterior` entry only if `saldo > 0` (colones, reference "Saldo anterior", `CxPEntryType.SaldoAnterior`); single `SaveChangesAsync`; catch `DbUpdateException` → throw `PeriodAlreadyClosed`.

## Phase 4: US2 — Preview dialog (P1)

**Independent Test**: quickstart.md §3, §5, §6, §8. Depends on Phase 2 and T006.

- [ ] T007 [US2] In `MariCamiStore/Services/ICxPService.cs` add records `ClosePreviewNewPeriodDto(int Month, int Year, decimal ExchangeRate, decimal ProposedExchangeRate, decimal PagosRealizados, decimal EnCuenta, decimal ProposedEnCuenta, decimal? SaldoAnterior, CxPPeriodIndicatorsDto Indicators)` and `ClosePreviewDto(CxPPeriodIndicatorsDto Closing, ClosePreviewNewPeriodDto NewPeriod, List<string> Errors)`; add `Task<ClosePreviewDto> GetClosePreviewAsync(Guid periodId, decimal? newExchangeRate, decimal? newEnCuenta)` to the interface.
- [ ] T008 [US2] Implement `GetClosePreviewAsync` in `MariCamiStore/Services/CxPService.cs` per plan D3: period missing/closed → throw `PeriodAlreadyClosed`; closing = `GetPeriodIndicatorsAsync`; proposals via `CarryOver` and `ProposedExchangeRateAsync`; effective TC/EnCuenta = args ?? proposed; collect `Errors` (TC ≤ 0, EnCuenta < 0) without throwing; new indicators: `porPagarEnColones = saldo`, `porPagarPorMoneda` = { local currency: saldo } when saldo > 0 else empty, same `SaldosPorCobrar`, `shipping = ComputeShippingPendingAsync(effectiveTc > 0 ? effectiveTc : 0, local)`, derived via `ComputeDerived`, `IsClosed = false`, `ExchangeRateWarning = effectiveTc <= 0`, `PeriodId = Guid.Empty`. No writes.
- [ ] T009 [US2] In `MariCamiStore/Pages/CxP/Index.cshtml.cs`: add `OnGetClosePreviewAsync(Guid periodId, decimal? exchangeRate, decimal? enCuenta)` returning the DTO, or `{ error, alreadyClosed = true }` on `PeriodAlreadyClosed`; replace `OnPostClosePeriodAsync()` with `OnPostClosePeriodAsync([FromBody] ClosePeriodRequest req)` (`record ClosePeriodRequest(Guid PeriodId, decimal ExchangeRate, decimal EnCuenta)` in `ICxPService.cs`) calling `ClosePeriodAsync(req.PeriodId, req.ExchangeRate, req.EnCuenta)` and returning `{ success, error, alreadyClosed = error == CxPMessages.PeriodAlreadyClosed }`. It MUST NOT look up "the open period".
- [ ] T010 [US2] In `MariCamiStore/Pages/CxP/Index.cshtml` replace the body of `#modal-close-period` (make dialog `modal-lg`) per plan D8: left column "Mes que cierra (`#close-closing-title`)" with `#close-closing-table`; right column "Mes nuevo (`#close-new-title`)" with `#close-new-tc` (number, step 0.01, min 0.01) + hint `#close-new-tc-hint`, Pagos Realizados ₡0 text, `#close-new-encuenta` (number, step 0.01, min 0) + hint `#close-new-encuenta-hint`, "Entradas iniciales" `#close-new-entries`, `#close-new-table`; keep `#close-error`, Cancelar and `#btn-confirm-close` ("Confirmar cierre").
- [ ] T011 [US2] In `MariCamiStore/wwwroot/js/pages/cxp/index.js`: `#btn-close-period` click → reset, disable confirm, open modal, `loadClosePreview()` (GET `?handler=ClosePreview&periodId=` + currentPeriodId [+ edited values]); render closing table (Total por pagar colonizado, each Por pagar en {Moneda}, Saldos por Cobrar a Clientes, Shipping CR Pendientes, Deuda a Pagar, En Cuenta, Pendiente de Recoger, Posición, Tipo de Cambio, Pagos Realizados) and new-month table (same indicator rows) with `formatMoney`/negative display; fill inputs with values only on first load, hints "Propuesto: …"; entries text "Saldo anterior: ₡X" or "Sin saldo anterior"; show `errors` in `#close-error` and keep confirm disabled while errors exist; `input` on the two fields → debounce 400 ms → `loadClosePreview(tc, enCuenta)`; on `alreadyClosed` show message and `window.location.reload()` after 1.5 s.
- [ ] T012 [US2] In `MariCamiStore/wwwroot/js/pages/cxp/index.js` confirm handler: client-validate (TC > 0, EnCuenta ≥ 0) with `CxP` messages, disable `#btn-confirm-close`, POST `ClosePeriod` `{ periodId: currentPeriodId, exchangeRate, enCuenta }`; success → hide modal + reload; `alreadyClosed` → message + reload; other error → show and re-enable.

## Phase 5: US3 — Validations (P2)

**Independent Test**: quickstart.md §2, §5.

- [ ] T013 [US3] In `MariCamiStore/Pages/CxP/Index.cshtml.cs` `OnPostUpdatePeriodAsync`: TC ≤ 0 → `CxPMessages.ExchangeRateMustBePositive`; Pagos or EnCuenta < 0 → `CxPMessages.ValueCannotBeNegative`. `OnPostInitPeriodAsync`: use `ExchangeRateMustBePositive` for TC ≤ 0.
- [ ] T014 [US3] In `MariCamiStore/Services/CxPService.cs` `UpdatePeriodFieldsAsync` and `InitializePeriodAsync`: same guards (throw `InvalidOperationException` with `CxPMessages`) — defense in depth.
- [ ] T015 [US3] In `MariCamiStore/Pages/CxP/Index.cshtml`: `#tc-input` `min="0.01"`, `#pagos-input` and `#en-cuenta-input` `min="0"`, `#init-tc` `min="0.01"`. In `MariCamiStore/wwwroot/js/pages/cxp/index.js` `savePeriodFields()` and the init handler: validate before POST and show the messages in `#panel-error` / `#init-error`.

## Phase 6: US4 — Double-close protection (P2)

**Independent Test**: quickstart.md §7. (Button disable + close-by-id are done in T009/T011/T012.)

- [ ] T016 [US4] In `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/PeriodControlEntityTypeConfiguration.cs` add `builder.HasIndex(p => p.OrganizationId).IsUnique().HasFilter("[IsClosed] = 0").HasDatabaseName("IX_PeriodControls_OneOpenPerOrganization");` (keep the existing composite unique index).
- [ ] T017 [US4] From `MariCamiStore/`: `dotnet ef migrations add OneOpenPeriodPerOrganization --output-dir Infrastructure/Persistance/Migrations`; verify `Up` only creates that filtered unique index and `Down` drops it. Do NOT update any database.

## Phase 7: US5 — Panel visuals (P3)

**Independent Test**: quickstart.md §1.

- [ ] T018 [US5] In `MariCamiStore/Pages/CxP/Index.cshtml`: add a page `<style>` block with `.cxp-pastel-red { background-color:#f8d7da !important; color:#721c24 !important; }`, `.cxp-pastel-green { background-color:#d4edda !important; color:#155724 !important; }`, `.cxp-pastel-yellow { background-color:#fff3cd !important; color:#856404 !important; }` (icon spans inherit color). Column 1: Deuda a Pagar box `bg-danger` → `cxp-pastel-red`; insert new box "En Cuenta (₡)" (`info-box cxp-pastel-green`, icon `fas fa-piggy-bank`, number `#en-cuenta-indicator` bold) between Deuda and Pendiente; Pendiente `bg-warning` → `cxp-pastel-yellow`.
- [ ] T019 [US5] In `MariCamiStore/wwwroot/js/pages/cxp/index.js` `loadPeriod()`: set `$('#en-cuenta-indicator').text(formatMoney(data.enCuenta, '₡'))` next to the other indicators.

## Phase 8: Polish

- [ ] T020 `dotnet build MariCamiStore/MariCamiStore.csproj`: 0 errors, no new warnings; `node --check MariCamiStore/wwwroot/js/pages/cxp/index.js`.
- [ ] T021 Verify by code reading against quickstart.md §1–8: every new element ID used in `cxp/index.js` exists once in `Index.cshtml`; no handler closes "the open period" implicitly; panel indicator output unchanged for the same data (T002 refactor).

---

## Dependencies

T001 → T002–T004 → T005 → T006 → T007 → T008 → T009 → T010 → T011 → T012. T013–T015 after T004 (T015 touches the same JS file as T011/T012 — do after them). T016 → T017 (independent of US1–US3). T018 → T019 (T019 same JS file; do after T012/T015). Polish last.

## Parallel

T016/T017 (EF config + migration) are the only tasks in files no other task touches; they can run anytime after T001. All other tasks share `CxPService.cs`, `Index.cshtml(.cs)` or `cxp/index.js` and run sequentially.

## Implementation Strategy

MVP = US1 + US2 (new rules behind the preview). Then US3 validations, US4 index, US5 visuals. Migration applied manually after the pre-deploy check in quickstart.md.
