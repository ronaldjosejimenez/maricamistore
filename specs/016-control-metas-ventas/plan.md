# Implementation Plan: Control de Metas de Ventas

**Branch**: `016-control-metas-ventas` | **Date**: 2026-10-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/016-control-metas-ventas/spec.md`

## Summary

Add a salesperson catalog, a monthly sales-goal master (`SalesGoal`) with a per-day detail (`SalesGoalDay`), a `DefaultMonthlyGoal` field in `Configuration`, and two new screens under Ventas → Metas ("Mes Actual" editable, "Histórico" read-only). Goals are created **on demand** the first time a salesperson's current month (Costa Rica time) is requested, with the daily proposed amounts produced by an isolated, replaceable distribution algorithm (fixed weekday profile × weekly ramp, rounded to 1 000 units, residue fixed so the sum is exact). All percentages, the "% al día de hoy" and its traffic-light status are computed server-side in one service so the rules live in one place; the page JS only renders what the server returns.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core Razor Pages; JavaScript (jQuery, jsGrid, AdminLTE 3 / Bootstrap 4)

**Primary Dependencies**: EF Core 10 (SQL Server) with `IEntityTypeConfiguration` classes and migrations in `Infrastructure/Persistance/Migrations`

**Storage**: 3 new tables (`Salespeople`, `SalesGoals`, `SalesGoalDays`) + 1 new column on `Configurations` → one EF migration

**Testing**: No automated test project exists in the repo; `dotnet build` + manual [quickstart.md](quickstart.md). The distribution algorithm is a pure static function so it can be verified with a throwaway snippet (see quickstart).

**Target Platform**: Azure App Service container (sleeps when idle → no background job; on-demand creation)

**Project Type**: Single web application (`MariCamiStore/`)

**Constraints**: Costa Rica time zone hard-coded (`"Central America Standard Time"`, same id already used in `PaymentService`/`OrderService`/`TransactionService`); creation idempotent under concurrent requests; day amounts sum exactly to the goal; percentages guard divide-by-zero.

**Scale/Scope**: A handful of salespeople × 12 months × ≤31 days per org.

## Constitution Check

`.specify/memory/constitution.md` is the unfilled template — no ratified principles. Gate: **PASS**. Post-design: **PASS** (no new projects or dependencies; follows existing catalog / service / page patterns).

## Project Structure

### Documentation (this feature)

```text
specs/016-control-metas-ventas/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/sales-goals-handlers.md
└── tasks.md
```

### Source Code

```text
MariCamiStore/
├── Model/
│   ├── Salesperson.cs                  [NEW]
│   ├── SalesGoal.cs                    [NEW]
│   ├── SalesGoalDay.cs                 [NEW]
│   └── Configuration.cs                [MODIFY] + DefaultMonthlyGoal
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/
│   │   ├── SalespersonEntityTypeConfiguration.cs    [NEW]
│   │   ├── SalesGoalEntityTypeConfiguration.cs      [NEW] unique (Salesperson, Org, Year, Month)
│   │   ├── SalesGoalDayEntityTypeConfiguration.cs   [NEW] unique (SalesGoal, DayOfMonth)
│   │   └── ConfigurationEntityTypeConfiguration.cs  [MODIFY] column + seed value 4 000 000
│   ├── MariCamiStoreContext.cs         [MODIFY] DbSets, configs, org query filters
│   └── Migrations/<ts>_AddSalesGoals.cs [NEW] generated with dotnet ef
├── Helpers/
│   ├── CostaRicaClock.cs               [NEW] Now()/Today() in CR time (single place for FR-010)
│   └── DailyGoalDistributor.cs         [NEW] isolated algorithm (weights, rounding, residue)
├── Services/
│   ├── ISalesGoalService.cs / SalesGoalService.cs [NEW] get-or-create, update header/day, history, recompute
│   ├── ICatalogService.cs / CatalogService.cs     [MODIFY] salespeople CRUD (global) + upsert copies DefaultMonthlyGoal
│   └── (Extensions/ApplicationExtensions.cs)      [MODIFY] register ISalesGoalService
├── Pages/
│   ├── Salespeople/Index.cshtml(.cs)   [NEW] jsGrid catalog (like Customers), IsActive checkbox, no hard delete
│   ├── SalesGoals/Current.cshtml(.cs)  [NEW] Mes Actual
│   ├── SalesGoals/History.cshtml(.cs)  [NEW] Histórico (read-only)
│   └── Shared/_Layout.cshtml           [MODIFY] Ventas → Metas (2 items); Catálogos → Vendedores
└── wwwroot/js/pages/
    ├── salespeople/index.js            [NEW]
    ├── sales-goals/current.js          [NEW]
    ├── sales-goals/history.js          [NEW]
    └── configurations/index.js         [MODIFY] + defaultMonthlyGoal column
```

**Structure Decision**: Single existing web project; new vertical slice (`Salespeople`, `SalesGoals`) plus small edits to Configuration, catalog service, context and layout. The "Vendedores" catalog is placed in the existing top-level **Catálogos** menu (the app has no "Ventas/Catálogos" submenu; Catálogos already holds Tipos de Producto and Proveedores).

## Key Design Decisions

1. **Pure distributor** (`DailyGoalDistributor.Distribute(year, month, goal) → decimal[]`): weekday base weights Lun 19, Mar 22, Mié 26, Jue 29, Vie 36, Sáb 51, Dom 36 (averages observed in the Oct-2026 Excel) × linear ramp 0.94 → 1.06 over the month; floor to 1 000-unit blocks by largest-remainder; leftover (< 1 000, or cents) added to the heaviest day. Constants live at the top of the class for later tuning.
2. **"Ajustado" is derived**: a day is manually adjusted when `GoalAmount != ProposedAmount`. Header goal/currency change → recompute proposals; `GoalAmount` follows only days where it equaled the *old* proposal.
3. **Server owns the math**: every mutating handler returns the full refreshed model (header, days, `todayPercentage`, `todayStatus`), so UI state can never drift from the DB.
4. **Idempotent create**: unique index + try/insert/catch `DbUpdateException` → re-read.
5. **Edit window**: handlers reject edits when the goal's (year, month) ≠ CR current month (FR-026) and validate non-negative amounts / goal > 0 (FR-023, FR-027).
6. **Org scoping**: `SalesGoal` and `SalesGoalDay` get the standard `ICurrentOrganizationService` query filter; `Salesperson` is global like `Customer`.
7. **Percentages**: stored/returned rounded to 1 decimal; 0 when divisor is 0.
