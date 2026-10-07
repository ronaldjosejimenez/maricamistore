# Data Model: Control de Metas de Ventas

## Salesperson (`dbo.Salespeople`) — global catalog
| Field | Type | Rules |
|---|---|---|
| Id | Guid PK | generated |
| Name | string(150) | required |
| NickName | string(50) | optional |
| PhoneNumber | string(20) | optional |
| Email | string(100) | optional |
| IsActive | bool | default true |

No hard delete from UI; deactivate instead (FR-003).

## SalesGoal (`dbo.SalesGoals`) — monthly master, org-filtered
| Field | Type | Rules |
|---|---|---|
| Id | Guid PK | |
| SalespersonId | Guid FK → Salespeople | required |
| OrganizationId | Guid | required, query filter = session org |
| Month | int | 1–12 |
| Year | int | |
| GoalAmount | decimal(18,2) | > 0 |
| ActualAmount | decimal(18,2) | = Σ day ActualAmount, default 0 |
| CompliancePercentage | decimal(9,1) | Actual/Goal×100, 0 if Goal = 0 |
| CurrencyId | Guid FK → Currencies | required |

Unique index: (SalespersonId, OrganizationId, Year, Month).

## SalesGoalDay (`dbo.SalesGoalDays`) — daily detail
| Field | Type | Rules |
|---|---|---|
| Id | Guid PK | |
| SalesGoalId | Guid FK → SalesGoals (cascade) | |
| DayOfMonth | int | 1..days in month |
| ProposedAmount | decimal(18,2) | from distributor; Σ = GoalAmount |
| GoalAmount | decimal(18,2) | ≥ 0, editable; "adjusted" ⇔ ≠ ProposedAmount |
| ActualAmount | decimal(18,2) | ≥ 0, editable |
| CompliancePercentage | decimal(9,1) | Actual/Goal×100, 0 if Goal = 0 |

Unique index: (SalesGoalId, DayOfMonth). Query filter via `SalesGoal.OrganizationId`.

## Configuration (modified)
+ `DefaultMonthlyGoal` decimal(18,2), default 4 000 000, must be > 0.

## Derived (not stored)
- **TodayPercentage** = Σ Actual(day 1..today) / Σ GoalAmount(day 1..today) × 100 (today = CR day, inclusive; 0 if divisor 0; only for the current month).
- **TodayStatus**: `red` < 70, `yellow` 70–90 inclusive, `green` > 90.

## State / Transitions
- Create (on demand) → edit day actual/goal → edit header goal/currency (recompute proposals; `GoalAmount` follows the proposal only where it equaled the old proposal; actuals untouched).
- Month rolls over → previous goal becomes read-only (History).
