---
description: "Task list for 015-filtro-organizacion-pagos"
---

# Tasks: Filtro de Organización en Payments

**Input**: `specs/015-filtro-organizacion-pagos/` (plan.md, spec.md, data-model.md, contracts/payments-handlers.md, research.md, quickstart.md)

**Tests**: No automated test project; verification = `dotnet build` + quickstart.md (manual).

**Paths**: relative to repository root; web project in `MariCamiStore/`. No migrations in this feature.

Stories: US1 combo + default (P1), US2 saldos/balance by chosen org (P1), US3 register payment for chosen org (P1), US4 block registration on "Todas" (P2).

---

## Phase 1: Setup

- [ ] T001 Run `dotnet build MariCamiStore/MariCamiStore.csproj` to confirm a clean baseline.

## Phase 2: Foundational (shared service signature changes; blocks all stories)

- [ ] T002 In `MariCamiStore/Services/IPaymentService.cs`: change `Task<List<SaldoReportRow>> GetSaldosReportAsync()` to `Task<List<SaldoReportRow>> GetSaldosReportAsync(Guid? organizationId)`; change `Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId)` to `Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId, Guid? organizationId)`; change `Task<CustomerBalanceDto?> RegisterPaymentAsync(Guid customerId, decimal amount)` to `Task<(bool Success, string? Error, CustomerBalanceDto? Balance)> RegisterPaymentAsync(Guid customerId, decimal amount, Guid? organizationId)`.
- [ ] T003 In `MariCamiStore/Services/PaymentService.cs` `CalcBalanceAsync`: add a `Guid? explicitOrganizationId = null` parameter; when provided, query `context.Transactions.IgnoreQueryFilters().Where(t => t.CustomerId == customerId && t.OrganizationId == explicitOrganizationId.Value)`; keep the existing `ignoreOrgFilter` behavior when `explicitOrganizationId` is null (plan D2).

## Phase 3: US1 — Organization combo with session default (P1)

**Independent Test**: quickstart.md §1.

- [ ] T004 [US1] In `MariCamiStore/Pages/Payments/Index.cshtml.cs`: inject `IOrganizationService organizationService`; in `OnGetAsync`, after the existing config/currency lookup, set `ViewData["Organizations"] = await organizationService.GetOrganizationsAsync();` and `ViewData["CurrentOrganizationId"] = currentOrg.OrganizationId;` (use the protected `CurrentOrg` from `OrganizationPageModel` or the injected `ICurrentOrganizationService`, matching existing constructor param name `currentOrg`).
- [ ] T005 [US1] In `MariCamiStore/Pages/Payments/Index.cshtml`: add a `<div class="form-group">` with `<label>Organización</label><select id="payment-org-filter" class="form-control">` above the "Registrar Pago" `card-header`, rendering `<option value="">Todas</option>` then `@foreach (var o in (List<MariCamiStore.Model.Organization>)ViewData["Organizations"]!) { <option value="@o.Id" selected="@(o.Id == (Guid)ViewData["CurrentOrganizationId"]!)">@o.Name</option> }`.

## Phase 4: US2 — Saldos and balance scoped to the chosen organization (P1)

**Independent Test**: quickstart.md §3, §6, §7. Depends on T002/T003.

- [ ] T006 [US2] In `MariCamiStore/Services/PaymentService.cs` `GetSaldosReportAsync(Guid? organizationId)`: when `organizationId` is null keep the current query unchanged (global); when it has a value, add `.Where(t => t.OrganizationId == organizationId.Value)` to the existing `IgnoreQueryFilters()` query before grouping (plan D2). Keep the rest of the method (customer lookup, projection, ordering) unchanged.
- [ ] T007 [US2] In `MariCamiStore/Services/PaymentService.cs` `GetCustomerBalanceAsync(Guid customerId, Guid? organizationId)`: keep `globalBalance = await CalcBalanceAsync(customerId, ignoreOrgFilter: true)`; compute `orgBalance` as `organizationId.HasValue ? await CalcBalanceAsync(customerId, ignoreOrgFilter: false, explicitOrganizationId: organizationId) : globalBalance` (FR-008: "Todas" → orgBalance == globalBalance).
- [ ] T008 [US2] In `MariCamiStore/Pages/Payments/Index.cshtml.cs`: change `OnGetSaldosAsync()` to `OnGetSaldosAsync(Guid? organizationId)` passing it through; change `OnGetBalanceAsync(Guid customerId)` to `OnGetBalanceAsync(Guid customerId, Guid? organizationId)` passing it through.
- [ ] T009 [US2] In `MariCamiStore/wwwroot/js/pages/payments/index.js`: add `function currentOrgFilter() { var v = $('#payment-org-filter').val(); return v ? v : null; }`; a module-level `var saldosRequest = null;` and `var balanceRequest = null;`; change `loadSaldos()` to abort `saldosRequest` if in flight, then `saldosRequest = $.get('?handler=Saldos' + (currentOrgFilter() ? '&organizationId=' + encodeURIComponent(currentOrgFilter()) : ''), ...)` (ignore `statusText === 'abort'` in `.fail()`); change the customer-change balance `$.get` similarly (abort `balanceRequest`, append `&organizationId=...` when set).
- [ ] T010 [US2] In `MariCamiStore/wwwroot/js/pages/payments/index.js`: add a `change` handler on `#payment-org-filter` that calls `loadSaldos()` and, if `$('#payment-customer').val()` is non-empty, reloads the balance card for that customer with the new filter (reuse the same code path as the existing customer-change handler, e.g. extract it into a named function `loadBalance(customerId)` used by both).

## Phase 5: US3 — Register payment for the chosen organization (P1)

**Independent Test**: quickstart.md §4. Depends on T002.

- [ ] T011 [US3] In `MariCamiStore/Services/PaymentService.cs` `RegisterPaymentAsync(Guid customerId, decimal amount, Guid? organizationId)`: if `organizationId == null` return `(false, "Seleccione una organización específica para registrar el pago.", null)` before touching the database (FR-013); otherwise keep existing logic but set `transaction.OrganizationId = organizationId.Value` (instead of `currentOrg.OrganizationId`) and return `(true, null, await GetCustomerBalanceAsync(customerId, organizationId))` on success, or `(false, "Cliente no encontrado.", null)` when the customer lookup fails (preserve existing message).
- [ ] T012 [US3] In `MariCamiStore/Pages/Payments/Index.cshtml.cs`: add `Guid? OrganizationId` to `PaymentRequest`; in `OnPostRegisterPaymentAsync`, after the existing customer/amount validation, if `request.OrganizationId == null` return `new JsonResult(new { success = false, error = "Seleccione una organización específica para registrar el pago." })` (client-side guard should already prevent this — defense in depth per plan D3); otherwise call `var (success, error, balance) = await paymentService.RegisterPaymentAsync(request.CustomerId, request.Amount, request.OrganizationId);` and return `new JsonResult(new { success, error, balance })`.
- [ ] T013 [US3] In `MariCamiStore/wwwroot/js/pages/payments/index.js` register-payment click handler: include `organizationId: currentOrgFilter()` in the POST body; keep existing amount/customer client validation as the first checks.

## Phase 6: US4 — Block registration when "Todas" is selected (P2)

**Independent Test**: quickstart.md §2, §5, §8. Depends on T005 (markup), T009 (JS helpers).

- [ ] T014 [US4] In `MariCamiStore/Pages/Payments/Index.cshtml`: add `<small id="payment-org-required-hint" class="text-danger d-block mt-1" style="display:none;">Seleccione una organización específica para poder registrar un pago.</small>` right after `#btn-register-payment`.
- [ ] T015 [US4] In `MariCamiStore/wwwroot/js/pages/payments/index.js`: add `function updateRegisterButtonState() { var blocked = currentOrgFilter() === null; $('#btn-register-payment').prop('disabled', blocked); $('#payment-org-required-hint').toggle(blocked); }`; call it once on page load (`$(function(){ ... updateRegisterButtonState(); ... })`) and inside the `#payment-org-filter` `change` handler from T010.

## Phase 7: Polish

- [ ] T016 `dotnet build MariCamiStore/MariCamiStore.csproj`: 0 errors, no new warnings; `node --check MariCamiStore/wwwroot/js/pages/payments/index.js`.
- [ ] T017 Verify by code reading against quickstart.md §1–9: every new/changed element id used in `payments/index.js` exists once in `Index.cshtml`; `RegisterPaymentAsync` never writes a transaction when `organizationId` is null; `GetSaldosReportAsync(null)`/`GetCustomerBalanceAsync(_, null)` produce output identical in shape and value to the pre-change behavior (regression check for FR-007/FR-008).

---

## Dependencies

T001 → T002 → T003 → (T004 → T005) and (T006, T007 → T008 → T009 → T010) and (T011 → T012 → T013); T014 → T015 (needs T009's `currentOrgFilter`, T010's change handler). All JS tasks (T009, T010, T013, T015) touch the same file and run in that order. Polish last.

## Parallel

T004/T005 (US1, page files) and T011 (US3, service) can proceed in parallel with T006/T007 (US2, service) once T002/T003 land, since they touch different methods in the same two files but don't depend on each other's output — coordinate to avoid edit conflicts if run by different agents; otherwise sequential is simplest given the small file count.

## Implementation Strategy

MVP = US1 + US2 (see and scope balances by any organization). Then US3 (register against the chosen org) and US4 (guard rail on "Todas") close the loop.
