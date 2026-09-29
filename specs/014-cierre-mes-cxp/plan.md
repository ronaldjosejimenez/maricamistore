# Implementation Plan: Cierre de Mes de CxP — Reglas, Vista Previa y Protecciones

**Branch**: `014-cierre-mes-cxp` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/014-cierre-mes-cxp/spec.md`

## Summary

Replace the month-close carry-over rules (Saldo anterior = max(0, Pendiente de Recoger); new En Cuenta = max(0, EnCuenta − max(0, Deuda))), put a server-computed **two-column preview dialog** in front of the close (new month TC and En Cuenta editable, indicators recalculated server-side), enforce **TC > 0 / no negatives** everywhere, protect against **double close** (disabled button + close-by-id + filtered unique index "one open period per organization"), and restyle panel column 1 (pastel red Deuda, new pastel green En Cuenta, pastel yellow Pendiente). One small migration (index only).

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery)

**Primary Dependencies**: EF Core 10 (SQL Server), AdminLTE 3 / Bootstrap 4, Font Awesome

**Storage**: SQL Server — `PeriodControls` gets a filtered unique index; `CxPEntries` unchanged (Saldo anterior value rule changes)

**Testing**: No automated test project; `dotnet build` + manual [quickstart.md](quickstart.md)

**Target Platform**: Azure App Service; migration applied manually before deploy

**Project Type**: Single web application (`MariCamiStore/`)

**Constraints**: Indicator formulas unchanged (FR-001); close atomic (FR-006); server recomputes on confirm (FR-012); never run `dotnet ef database update` from the pipeline

**Scale/Scope**: One open period per organization; tens of entries per period

## Constitution Check

`.specify/memory/constitution.md` is the unfilled template — no ratified principles. Gate: **PASS**. Post-design: **PASS** (existing layering; no new dependencies).

## Project Structure

### Documentation (this feature)

```text
specs/014-cierre-mes-cxp/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/cxp-handlers.md
└── tasks.md
```

### Source Code

```text
MariCamiStore/
├── Services/
│   ├── ICxPService.cs        [MODIFY] + ClosePreviewDto records, GetClosePreviewAsync, ClosePeriodAsync(periodId, newTc, newEnCuenta), CxPValidation messages
│   └── CxPService.cs         [MODIFY] extract indicator math + shipping-pending helper; preview; new close rules; TC>0 in UpdatePeriodFields
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/PeriodControlEntityTypeConfiguration.cs  [MODIFY] filtered unique index OrganizationId WHERE IsClosed = 0
│   └── Migrations/<ts>_OneOpenPeriodPerOrganization.cs              [CREATE]
├── Pages/CxP/
│   ├── Index.cshtml           [MODIFY] column 1 (3 pastel boxes), preview modal (2 columns), min attrs, page <style>
│   └── Index.cshtml.cs        [MODIFY] OnGetClosePreviewAsync, OnPostClosePeriodAsync(ClosePeriodRequest), validations
└── wwwroot/js/pages/cxp/index.js  [MODIFY] preview flow (load, debounce recompute, confirm), En Cuenta box, client validation
```

## Design Decisions

### D1 — Shared indicator math
Refactor `GetPeriodIndicatorsAsync` so the formulas live in one private helper used by both the panel and the preview:
- `ComputeShippingPendingAsync(decimal exchangeRate, Guid localCurrencyId)` → existing D4 (013) logic (Active+Delivering, per-order max(0, …), convert unless local; returns 0 when TC == 0).
- `static Derived Compute(decimal porPagarEnColones, decimal pagos, decimal enCuenta, decimal saldosPorCobrar, decimal shipping)` → `Deuda = porPagar − pagos; Pendiente = Deuda − enCuenta; Posicion = saldos + enCuenta − Deuda − shipping` (FR-001 unchanged).

### D2 — Close rules (pure function)
`static (decimal saldoAnterior, decimal proposedEnCuenta) CarryOver(decimal deuda, decimal enCuenta)`:
- `debtToPay = Math.Max(0, deuda)`
- `saldoAnterior = Math.Max(0, debtToPay − enCuenta)` (== max(0, Pendiente) when deuda ≥ 0)
- `proposedEnCuenta = Math.Max(0, enCuenta − debtToPay)`
Proposed TC for new month: `config.ExchangeRate > 0 ? config.ExchangeRate : period.ExchangeRate` (FR-005).

### D3 — Preview (server-computed)
`GetClosePreviewAsync(Guid periodId, decimal? newExchangeRate, decimal? newEnCuenta)` → `ClosePreviewDto`:
- `Closing`: the current `CxPPeriodIndicatorsDto` (read-only column).
- `NewPeriod`: `{ Month, Year, ExchangeRate (arg ?? proposed), ProposedExchangeRate, PagosRealizados = 0, EnCuenta (arg ?? proposed), ProposedEnCuenta, SaldoAnterior (null when 0), Indicators }` where Indicators are computed as if the new period existed: `porPagarEnColones = saldoAnterior ?? 0` (only entry, in colones), `porPagarPorMoneda` = {colones: saldo} or empty, `saldosPorCobrar` = same live value, `shipping = ComputeShippingPendingAsync(newTc)`, then `Compute(...)`.
- `Errors`: validation messages for the arguments (TC ≤ 0, EnCuenta < 0) so the dialog can show them without throwing.
No writes. JS calls it on open (no args) and on TC/EnCuenta input (debounced 400 ms) with the edited values.

### D4 — Close (atomic, server recompute)
`ClosePeriodAsync(Guid periodId, decimal newExchangeRate, decimal newEnCuenta)`:
1. Validate `newExchangeRate > 0`, `newEnCuenta >= 0` → `InvalidOperationException` with FR-015 messages.
2. Load period by **id**; if missing or `IsClosed` → `InvalidOperationException("Este mes ya fue cerrado.")` (FR-017). Never falls back to "the open period".
3. Recompute indicators now (FR-012); `CarryOver(deuda, enCuenta)`.
4. `period.IsClosed = true`; add new `PeriodControl` (next month, `ExchangeRate = newExchangeRate`, `PagosRealizados = 0`, `EnCuenta = newEnCuenta`); add Saldo anterior `CxPEntry` only when `saldoAnterior > 0`.
5. One `SaveChangesAsync` (atomic, FR-006). Catch `DbUpdateException` (unique index violation from a concurrent close) → `InvalidOperationException("Este mes ya fue cerrado.")` (FR-018).

### D5 — One open period per organization (DB)
`builder.HasIndex(p => p.OrganizationId).IsUnique().HasFilter("[IsClosed] = 0").HasDatabaseName("IX_PeriodControls_OneOpenPerOrganization");` → migration `OneOpenPeriodPerOrganization`. Existing `(OrganizationId, Month, Year)` unique index stays. Pre-deploy check query documented in quickstart.

### D6 — Validations (FR-013..015)
- Shared messages: `"El tipo de cambio debe ser mayor a cero."`, `"El valor no puede ser negativo."`.
- `OnPostUpdatePeriodAsync`: TC ≤ 0 → first message; Pagos/EnCuenta < 0 → second. Same checks inside `UpdatePeriodFieldsAsync` (defense in depth).
- `OnPostInitPeriodAsync`: keep TC ≤ 0 rejection; align message.
- HTML: `#tc-input` `min="0.01"`, `#pagos-input`/`#en-cuenta-input` `min="0"`; JS validates before POST and shows the message in `#panel-error`.

### D7 — Handlers
- `GET ?handler=ClosePreview&periodId=&exchangeRate=&enCuenta=` → `ClosePreviewDto` JSON (or `{ error }` if period not open → "Este mes ya fue cerrado.").
- `POST ?handler=ClosePeriod` body `ClosePeriodRequest(Guid PeriodId, decimal ExchangeRate, decimal EnCuenta)` → `{ success, error, alreadyClosed }`.

### D8 — Preview modal UI
Replace the confirmation modal body with `modal-lg` two columns:
- Left "Mes que cierra (MM/YYYY)": table of read-only values (Total por pagar colonizado, each Por pagar en {Moneda}, Saldos por Cobrar a Clientes, Shipping CR Pendientes, Deuda a Pagar, En Cuenta, Pendiente de Recoger, Posición, Tipo de Cambio, Pagos Realizados).
- Right "Mes nuevo (MM/YYYY)": inputs `#close-new-tc` (min 0.01) and `#close-new-encuenta` (min 0) with "Propuesto: …" hints, Pagos Realizados ₡0 (text), "Entradas iniciales": Saldo anterior amount or "Sin saldo anterior", and the new-month indicators table.
- `#close-error`; "Cancelar" / "Confirmar cierre" (`#btn-confirm-close`, disabled while loading preview, while invalid, and while posting).
- On `alreadyClosed` → show message, then `window.location.reload()` after a short delay.

### D9 — Panel column 1 (FR-019/020)
Column 1 order: Deuda a Pagar, **En Cuenta (₡)** (`#en-cuenta-indicator`, icon `fas fa-piggy-bank`), Pendiente de Recoger. Replace `bg-danger`/`bg-warning` with page-scoped classes in a `<style>` block in `Index.cshtml`: `.cxp-pastel-red { background:#f8d7da; color:#721c24 }`, `.cxp-pastel-green { background:#d4edda; color:#155724 }`, `.cxp-pastel-yellow { background:#fff3cd; color:#856404 }` (Bootstrap alert palette → readable contrast); icon span inherits color. `loadPeriod()` fills `#en-cuenta-indicator` from `data.enCuenta`.

## Complexity Tracking

None beyond the shared-math refactor, which is required to keep panel and preview identical (FR-010).
