# Tasks: Control de Metas de Ventas

**Input**: `specs/016-control-metas-ventas/` (spec.md, plan.md, data-model.md, contracts/, research.md)
**Tests**: No automated test project in the repo; verification is `dotnet build` + [quickstart.md](quickstart.md). Pure-function check for the algorithm is part of T008.

Format: `- [ ] T### [P?] [US?] Description (path)` — `[P]` = can run in parallel (different files, no dependency).

## Phase 1: Setup (shared foundations)

- [ ] T001 [P] Create `Salesperson` entity (Id, Name, NickName, PhoneNumber, Email, IsActive=true) in `MariCamiStore/Model/Salesperson.cs`
- [ ] T002 [P] Create `SalesGoal` entity (Id, SalespersonId, OrganizationId, Month, Year, GoalAmount, ActualAmount, CompliancePercentage, CurrencyId, nav props, Days) in `MariCamiStore/Model/SalesGoal.cs`
- [ ] T003 [P] Create `SalesGoalDay` entity (Id, SalesGoalId, DayOfMonth, ProposedAmount, GoalAmount, ActualAmount, CompliancePercentage) in `MariCamiStore/Model/SalesGoalDay.cs`
- [ ] T004 [P] Add `DefaultMonthlyGoal` (decimal, default 4 000 000) to `MariCamiStore/Model/Configuration.cs`
- [ ] T005 [P] Create `CostaRicaClock` helper (`Now()`, `Today()` using "Central America Standard Time") in `MariCamiStore/Helpers/CostaRicaClock.cs`

## Phase 2: Foundational (blocks all user stories)

- [ ] T006 Add EF configurations `SalespersonEntityTypeConfiguration`, `SalesGoalEntityTypeConfiguration` (unique index Salesperson+Org+Year+Month, decimal(18,2)/(9,1), FK to Currency/Salesperson), `SalesGoalDayEntityTypeConfiguration` (unique SalesGoal+Day, cascade) in `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/`
- [ ] T007 Update `ConfigurationEntityTypeConfiguration` (column `DefaultMonthlyGoal` decimal(18,2) default 4000000; set it in both `HasData` seeds) in `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/ConfigurationEntityTypeConfiguration.cs`
- [ ] T008 Implement `DailyGoalDistributor.Distribute(year, month, goal)` (weekday weights 19/22/26/29/36/51/36 × ramp 0.94→1.06, 1 000-unit largest-remainder, residue to heaviest day, Σ exact, no negatives, goal ≤ 0 → zeros) with constants at the top, in `MariCamiStore/Helpers/DailyGoalDistributor.cs`; verify with a throwaway snippet per quickstart step 10
- [ ] T009 Register DbSets (`Salespeople`, `SalesGoals`, `SalesGoalDays`), apply the three configurations, add org query filters for `SalesGoal` and `SalesGoalDay` in `MariCamiStore/Infrastructure/Persistance/MariCamiStoreContext.cs`
- [ ] T010 Generate migration `AddSalesGoals` (`dotnet ef migrations add AddSalesGoals --project MariCamiStore`), review that existing `Configurations` rows get 4 000 000, and check the snapshot in `MariCamiStore/Infrastructure/Persistance/Migrations/`
- [ ] T011 Define `ISalesGoalService` and DTOs (`GoalDto`, `GoalDayDto`, `HistoryRowDto`, request records) in `MariCamiStore/Services/ISalesGoalService.cs`; register in `MariCamiStore/Extensions/ApplicationExtensions.cs`

**Checkpoint**: `dotnet build` passes; migration applies.

## Phase 3: User Story 4 — Catálogo de vendedores (P1)

- [ ] T012 [US4] Add salespeople methods (`GetSalespeopleAsync`, active-only variant, `CreateSalespersonAsync`, `UpdateSalespersonAsync`; Name required) to `MariCamiStore/Services/ICatalogService.cs` and `CatalogService.cs`
- [ ] T013 [US4] Create `MariCamiStore/Pages/Salespeople/Index.cshtml` and `Index.cshtml.cs` (Load/Insert/Update handlers per contract; no Delete)
- [ ] T014 [US4] Create `MariCamiStore/wwwroot/js/pages/salespeople/index.js` (jsGrid: Nombre, Apodo, Teléfono, Email, Activo checkbox; delete button disabled)
- [ ] T015 [US4] Add "Vendedores" item to Catálogos menu in `MariCamiStore/Pages/Shared/_Layout.cshtml`

## Phase 4: User Story 5 — Meta mensual por defecto (P2)

- [ ] T016 [US5] Copy `DefaultMonthlyGoal` in `CatalogService.UpsertConfigurationAsync` and reject values ≤ 0 (`MariCamiStore/Services/CatalogService.cs`)
- [ ] T017 [US5] Add "Meta mensual por defecto" number column (required) to `MariCamiStore/wwwroot/js/pages/configurations/index.js`; make the Configurations page surface the server validation error

## Phase 5: User Story 2 — Creación bajo demanda (P1)

- [ ] T018 [US2] Implement `SalesGoalService.GetOrCreateCurrentAsync(salespersonId)`: CR month/year, defaults from Configuration (fallback 4 000 000 and local currency), master + all days via `DailyGoalDistributor`, single `SaveChanges`, catch `DbUpdateException` → re-read (idempotent); returns `GoalDto` in `MariCamiStore/Services/SalesGoalService.cs`
- [ ] T019 [US2] Implement shared `BuildDto` in `SalesGoalService` (weekday names in Spanish, dd/MM/yyyy dates, `todayPercentage`, `todayStatus` 70/90 thresholds inclusive-yellow, `isCurrentMonth`, divide-by-zero → 0, 1-decimal rounding)

## Phase 6: User Story 1 — Mes Actual (P1) 🎯 MVP with US4+US2

- [ ] T020 [US1] Implement `UpdateDayAsync` (actual and/or goal; validations FR-023/FR-026; recompute day %, master actual and %; return `GoalDto`) in `MariCamiStore/Services/SalesGoalService.cs`
- [ ] T021 [US1] Create `MariCamiStore/Pages/SalesGoals/Current.cshtml.cs` (OrganizationPageModel guard; handlers `Salespeople`, `Goal`, `UpdateHeader`, `UpdateDay`; error → `{success:false,error}`)
- [ ] T022 [US1] Create `MariCamiStore/Pages/SalesGoals/Current.cshtml` (salesperson combo, header card with highlighted Real/% and traffic-light "% al día de hoy", currency combo, goal input, detail table, empty-state message when no salespeople)
- [ ] T023 [US1] Create `MariCamiStore/wwwroot/js/pages/sales-goals/current.js` (load first salesperson, render DTO, save-on-blur for goal/actual cells with inline error + revert, refresh whole view from returned DTO, semáforo classes, es-CR number format)
- [ ] T024 [US1] Add Ventas → "Metas" nested treeview (Mes Actual, Histórico de metas) in `MariCamiStore/Pages/Shared/_Layout.cshtml` (keep menu-open/active behavior consistent with `layout.js`)

## Phase 7: User Story 3 — Ajustes de meta (P2)

- [ ] T025 [US3] Implement `UpdateHeaderAsync` (goal > 0, currency exists, current month only; recompute proposals; `GoalAmount` follows proposal only where it equaled the old proposal; actuals untouched; recompute %s; return `GoalDto`) in `MariCamiStore/Services/SalesGoalService.cs`
- [ ] T026 [US3] Wire header goal/currency edit controls in `MariCamiStore/wwwroot/js/pages/sales-goals/current.js` to `UpdateHeader`

## Phase 8: User Story 6 — Histórico (P3)

- [ ] T027 [P] [US6] Implement `GetHistoryAsync(salespersonId)` and `GetDetailAsync(goalId)` (read-only, no create, newest first) in `MariCamiStore/Services/SalesGoalService.cs`
- [ ] T028 [P] [US6] Create `MariCamiStore/Pages/SalesGoals/History.cshtml(.cs)` and `MariCamiStore/wwwroot/js/pages/sales-goals/history.js` (salesperson combo incl. inactive, list table, read-only detail view, "sin registros" message)

## Phase 9: Polish

- [ ] T029 `dotnet build` clean (0 errors/warnings introduced); walk through [quickstart.md](quickstart.md) steps 1–10 as far as the environment allows
- [ ] T030 Mark spec Status `Implemented`, update `brainstorm/16-control-metas-ventas.md` status and `brainstorm/00-overview.md` (spec path)

## Dependencies

- T001–T005 → T006–T011 → everything else.
- T012–T015 (US4) and T016–T017 (US5) are independent of the goal service after Phase 2.
- T018–T019 → T020 → T021–T024 (US1); T025 needs T019; T027–T028 need T019 only.
- MVP slice: Phases 1–2 + US4 + US2 + US1.

## Parallel examples

- T001–T005 together; T012–T015 with T016–T017 together; T027–T028 with T025–T026.
