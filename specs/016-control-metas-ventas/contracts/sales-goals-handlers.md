# Contracts: Razor Page handlers

All JSON; POST handlers send the `RequestVerificationToken` header (same as existing pages). Errors: `{ "success": false, "error": "…" }`.

## Salespeople (`/Salespeople`)
- `GET ?handler=Load` → `Salesperson[]`
- `POST ?handler=Insert|Update` body `Salesperson` → saved `Salesperson` (Name required)
- No Delete handler; deactivate via Update with `isActive=false`.

## Mes Actual (`/SalesGoals/Current`)
- `GET ?handler=Salespeople` → `[{id, text}]` active only
- `GET ?handler=Goal&salespersonId=<guid>` → get-or-create current CR month → **GoalDto**
- `POST ?handler=UpdateHeader` body `{goalId, goalAmount, currencyId}` → **GoalDto**
- `POST ?handler=UpdateDay` body `{dayId, goalAmount?, actualAmount?}` (omitted = unchanged) → **GoalDto**

**GoalDto**
```json
{
  "success": true,
  "id": "guid", "salespersonId": "guid", "month": 10, "year": 2026,
  "goalAmount": 4000000, "actualAmount": 0, "compliancePercentage": 0.0,
  "currencyId": "guid", "currencySign": "₡",
  "todayPercentage": 0.0, "todayStatus": "red|yellow|green",
  "isCurrentMonth": true,
  "days": [ { "id": "guid", "dayOfMonth": 1, "weekday": "jueves", "date": "01/10/2026",
              "proposedAmount": 112000, "goalAmount": 112000, "actualAmount": 0,
              "compliancePercentage": 0.0 } ]
}
```
Validation errors: negative amounts, goal ≤ 0, edit on a non-current month, unknown id.

## Histórico (`/SalesGoals/History`)
- `GET ?handler=Salespeople` (all, including inactive) → `[{id, text}]`
- `GET ?handler=List&salespersonId=<guid>` → `[{id, month, year, goalAmount, actualAmount, compliancePercentage, currencySign}]` newest first
- `GET ?handler=Detail&goalId=<guid>` → **GoalDto** (read-only; never creates)

## Configuration
Existing `OnPostUpsertAsync`; payload gains `defaultMonthlyGoal` (> 0 enforced in `CatalogService.UpsertConfigurationAsync`).
