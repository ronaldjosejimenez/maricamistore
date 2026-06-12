# Tasks: Mejoras Fase 1

**Input**: Design documents from `specs/002-mejoras-fase1/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: No test tasks — out of scope per plan.md.

**Organization**: Tasks grouped by user story to enable independent implementation and testing. No new migrations or project setup required — the project is fully initialized.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: User story this task belongs to (US1–US6 from spec.md)

---

## Phase 1: Setup

No project initialization needed — the .NET 10 Razor Pages project is fully operational. Skip to Foundational phase.

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: The `BaseSettings` change is the only cross-cutting prerequisite. It unblocks US1 (default org from config) without blocking any other story.

**⚠️ Complete this before starting US1**

- [x] T001 Add `Guid? DefaultOrganizationId` property to `MariCamiStore/Settings.cs`
- [x] T002 [P] Add `"DefaultOrganizationId": null` key to `MariCamiStore/appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json`

**Checkpoint**: Settings model updated — US1 can now begin.

---

## Phase 3: User Story 1 — Organización Cargada Automáticamente (Priority: P1) 🎯 MVP

**Goal**: When `DefaultOrganizationId` is set in appsettings, the app loads that organization into session automatically on first request — no manual selection required.

**Independent Test**: Set a valid `DefaultOrganizationId` in `appsettings.Development.json`, restart the app, navigate to `/Orders` — the orders grid should load without being redirected to Organizations.

### Implementation

- [x] T003 [US1] Inject `IOptions<BaseSettings>` into `CurrentOrganizationService` constructor and add fallback logic: if session has no org AND `DefaultOrganizationId` is configured, write it to session and return it — in `MariCamiStore/Services/CurrentOrganizationService.cs`

**Checkpoint**: Default org auto-loads. User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 — Validación de Organización en Pantallas (Priority: P1)

**Goal**: All screens that depend on an active organization redirect the user to Organizations with a clear message if no org is selected.

**Independent Test**: Clear the session (or start with no default org), navigate to `/Configurations`, `/Payments`, and `/Reports/Saldos` — all should redirect to Organizations with an instructive message. Navigate to `/Currencies` — should load normally (no guard needed).

### Implementation

- [x] T004 [US2] Create abstract base class `OrganizationPageModel : PageModel` that checks `ICurrentOrganizationService.OrganizationId == Guid.Empty` on `OnGet()` and redirects to `/Organizations/Index` with an error query parameter — in `MariCamiStore/Pages/Shared/OrganizationPageModel.cs`
- [x] T005 [P] [US2] Change `Configurations/Index.cshtml.cs` to inherit `OrganizationPageModel` instead of `PageModel`; remove any existing manual org guard if present
- [x] T006 [P] [US2] Change `Payments/Index.cshtml.cs` to inherit `OrganizationPageModel` instead of `PageModel`; remove any existing manual org guard if present
- [x] T007 [P] [US2] Change `Reports/Saldos.cshtml.cs` to inherit `OrganizationPageModel` instead of `PageModel`; remove any existing manual org guard if present
- [x] T008 [P] [US2] Change `Orders/Index.cshtml.cs` to inherit `OrganizationPageModel`; remove the existing manual `RedirectToPage("/Organizations/Index")` guard from `OnGet()`
- [x] T009 [P] [US2] Change `Orders/Items.cshtml.cs` to inherit `OrganizationPageModel`; remove the existing manual redirect from `OnGetAsync()`

**Checkpoint**: All dependent screens guard org. User Story 2 is fully functional and testable independently.

---

## Phase 5: User Story 6 — Mejoras en Formulario de Órdenes (Priority: P1)

**Goal**: The Order creation form shows Supplier first, auto-suggests a name on supplier selection, pre-loads exchange rate and tax from org configuration, and blocks item modifications when the order is not Pending.

**Independent Test**: Open the New Order modal — Supplier is first field with no default; select a supplier; the name field auto-fills as `{SupplierName}-{DD}-{MM}-{YYYY}`; exchange rate and tax are pre-loaded from org config. Open an Active order's items page — the jsGrid's insert row is hidden.

### Implementation

- [x] T010 [US6] Inject `ICatalogService` into `Orders/Index.cshtml.cs` constructor and add `OnGetConfigurationAsync()` handler that returns `{ exchangeRate, taxPercentage }` from the active org's configuration — in `MariCamiStore/Pages/Orders/Index.cshtml.cs` (this file was already modified in T008; add the handler alongside the OrganizationPageModel inheritance)
- [x] T011 [US6] Move the Supplier `<select>` group to be the first field inside the order modal form body in `MariCamiStore/Pages/Orders/Index.cshtml`
- [x] T012 [US6] Modify `MariCamiStore/wwwroot/js/pages/orders/index.js`: in `openNewOrder()`, call `GET ?handler=Configuration` and pre-fill `#order-exchange-rate` and `#order-tax` from the response; add file-scope `var userEditedName = false` flag reset in `openNewOrder()`
- [x] T013 [US6] Modify `MariCamiStore/wwwroot/js/pages/orders/index.js`: add `$('#order-name').on('input', ...)` to set `userEditedName = true`; add `$('#order-supplier').on('change', ...)` that builds the suggested name `{SupplierName}-{DD}-{MM}-{YYYY}` and sets it only when `!$('#order-id').val() && !userEditedName`
- [x] T014 [US6] Verify that `MariCamiStore/wwwroot/js/pages/orders/items.js` disables the jsGrid insert row when the order status is not `"Pending"` — already correctly implemented (items.js uses `inserting: isPending`)

**Checkpoint**: Order form improved. User Story 6 is fully functional and testable independently.

---

## Phase 6: User Story 3 — CRUD de Organizaciones (Priority: P2)

**Goal**: Administrators can create, edit, and delete organizations through the UI. Deletion is blocked if the organization has related orders or configurations, with a friendly error message.

**Independent Test**: Create a new organization, edit its name, then try to delete it — should succeed since it has no data. Then assign it orders/config and try again — should fail with a clear message.

### Implementation

- [x] T015 [US3] Create `IOrganizationService` interface with `GetOrganizationsAsync`, `CreateOrganizationAsync`, `UpdateOrganizationAsync`, `DeleteOrganizationAsync` signatures — in `MariCamiStore/Services/IOrganizationService.cs`
- [x] T016 [US3] Implement `OrganizationService` using EF Core: `DeleteOrganizationAsync` checks `Orders` and `Configurations` tables for references before deleting, returning `(false, "mensaje amigable")` if any exist — in `MariCamiStore/Services/OrganizationService.cs`
- [x] T017 [US3] Register `IOrganizationService → OrganizationService` as scoped in `MariCamiStore/Extensions/ApplicationExtensions.cs`
- [x] T018 [US3] Refactor `Organizations/Index.cshtml.cs`: remove direct `MariCamiStoreContext` injection; inject `IOrganizationService`; keep `OnGetLoadAsync` and `OnPostSetActiveAsync`; add `OnPostInsertAsync`, `OnPostUpdateAsync`, `OnPostDeleteAsync` handlers following the contracts in `specs/002-mejoras-fase1/contracts/organization-handlers.md` — in `MariCamiStore/Pages/Organizations/Index.cshtml.cs`
- [x] T019 [US3] Update `MariCamiStore/wwwroot/js/pages/organizations/index.js` (create if it doesn't exist) to add standard jsGrid Insert/Update/Delete functionality alongside the existing SetActive button per row, following the handler naming in `specs/002-mejoras-fase1/contracts/organization-handlers.md`

**Checkpoint**: Organizations CRUD is fully functional. User Story 3 is testable independently.

---

## Phase 7: User Story 5 — Corrección de Visualización de Moneda (Priority: P2)

**Goal**: Currency fields in Configurations and ProductTypes grids display the abbreviation (e.g., "USD", "CRC") instead of raw GUIDs.

**Independent Test**: Open the Configurations grid and the ProductTypes grid — currency columns show abbreviation text in the cells and in the inline edit dropdown.

### Implementation

- [x] T020 [P] [US5] Modify `MariCamiStore/wwwroot/js/pages/product-types/index.js`: load currencies from `/Currencies?handler=Load` on init, map to `{ id, text: abbreviation }`, call `initGrid()` only after load, replace `currencyId` text column with jsGrid `select` field using the currency items
- [x] T021 [P] [US5] Modify `MariCamiStore/wwwroot/js/pages/configurations/index.js`: load currencies on init (same pattern as T020), replace `localCurrencyId` and `orderCurrencyIdDefault` text columns with jsGrid `select` fields showing abbreviation — following the contract in `specs/002-mejoras-fase1/contracts/configuration-handlers.md`

**Checkpoint**: Currency fields show abbreviation. User Story 5 is testable independently.

---

## Phase 8: User Story 4 — Configuración Única por Organización (Priority: P2)

**Goal**: The Configurations grid disables the insert row when a configuration already exists for the active organization, and shows a clear informational banner explaining that only editing is allowed.

**Independent Test**: Go to Configurations with a config already saved — insert row is hidden, banner is visible. Delete the config (or switch to an org without one) — insert row appears, banner is hidden.

### Implementation

- [x] T022 [US4] Add `<div id="config-banner" class="alert alert-info" style="display:none">Esta organización ya tiene una configuración. Solo puede editarla.</div>` above the jsGrid in `MariCamiStore/Pages/Configurations/Index.cshtml`
- [x] T023 [US4] Modify `MariCamiStore/wwwroot/js/pages/configurations/index.js`: after `loadData` resolves, toggle jsGrid `inserting` option and `#config-banner` visibility based on whether the result has 0 or ≥1 records — implemented together with T021

**Checkpoint**: One-per-org configuration enforced in UI. User Story 4 is testable independently.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Error handling review and integration validation across all changes.

- [x] T024 [P] Review `OrganizationService.cs` and `CurrentOrganizationService.cs` for exception handling — OrganizationService has ILogger<T> with try/catch; CurrentOrganizationService has no throwing paths
- [x] T025 [P] Verify `Organizations/Index.cshtml` has the jsGrid markup and antiforgery token — markup added in T019; antiforgery token is in _Layout.cshtml globally
- [x] T026 Build succeeded (0 errors) — runtime smoke test pending manual execution

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 2 (Foundational)**: No dependencies — start immediately
- **Phase 3 (US1)**: Requires Phase 2 (T001, T002)
- **Phase 4 (US2)**: No dependencies on other phases — can start in parallel with Phase 3
- **Phase 5 (US6)**: Requires Phase 4 to complete T008 first (same file); then T010–T014 proceed
- **Phase 6 (US3)**: No dependencies — can start any time after Phase 2
- **Phase 7 (US5)**: No dependencies — can start any time
- **Phase 8 (US4)**: Requires Phase 7 (T021) — same file modification
- **Phase 9 (Polish)**: Requires all previous phases

### User Story Dependencies

- **US1 (P1)**: Requires T001/T002 (foundational)
- **US2 (P1)**: Independent — can start after project is checked out
- **US6 (P1)**: Requires T008 (US2) to be done first (same file); then independent
- **US3 (P2)**: Independent — no dependency on P1 stories
- **US5 (P2)**: Independent — no dependency on other stories
- **US4 (P2)**: Requires T021 (US5) — same file modification; do US5 first, then add US4 on top

### Within Each User Story

- Services before page handlers
- Page handlers before JavaScript
- HTML markup before JavaScript that references it

### Parallel Opportunities

- T001 and T002 can run in parallel (different files)
- T005, T006, T007, T008, T009 can all run in parallel (different files) once T004 is done
- T020 and T021 can run in parallel (different files)
- T024 and T025 can run in parallel

---

## Parallel Example: Phase 4 (US2)

```text
After T004 completes:
  Parallel: T005 (Configurations)
  Parallel: T006 (Payments)
  Parallel: T007 (Reports/Saldos)
  Parallel: T008 (Orders/Index)
  Parallel: T009 (Orders/Items)
All five files are independent — no conflicts.
```

---

## Implementation Strategy

### MVP First (P1 Stories Only)

1. Complete Phase 2 (T001, T002)
2. Complete Phase 3 (US1 — T003)
3. Complete Phase 4 (US2 — T004–T009)
4. Complete Phase 5 (US6 — T010–T014)
5. **STOP and VALIDATE**: All P1 stories working
6. Proceed to P2 stories

### Incremental Delivery (All Stories)

1. Phase 2 → Foundation ready
2. Phase 3 + 4 → Auto-org + Guards working
3. Phase 5 → Order form improved
4. Phase 6 → Org CRUD added
5. Phase 7 → Currency display fixed
6. Phase 8 → Config one-per-org enforced
7. Phase 9 → Polish + smoke test

---

## Notes

- [P] tasks = different files, no shared state — safe to parallelize
- [Story] label maps each task to the user story it delivers
- Modify `Orders/Index.cshtml.cs` only once (T008 + T010 in sequence, not in parallel)
- Modify `configurations/index.js` only once (T021 + T023 in sequence: US5 first, then US4 on top)
- Commit after each phase checkpoint to maintain a clean, runnable state at each increment
