# Deep Review Findings

**Date:** 2026-10-19
**Branch:** 017-ajustes-ordenes-metas-clientes
**Rounds:** 1
**Gate Outcome:** PASS
**Invocation:** quality-gate (speckit-spex-ship)

Stage 1 spec compliance: 100% (22/22 FR, 23/23 acceptance scenarios, 8/8 edge cases), after a pre-review fix that shows the commission rate as 0% / 2.5% / 5% / 6%. It had shown a forced decimal, "5,0%".

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 2 | 2 | 0 |
| Minor | 10 | 2 | 8 |
| **Total** | **12** | **4** | **8** |

**Agents completed:** 5/5 (external tools: CodeRabbit and Copilot skipped, CLI not installed / disabled)
**Agents failed:** none

## Findings

### FINDING-1
- **Severity:** Important
- **Confidence:** 90
- **File:** MariCamiStore/wwwroot/js/pages/orders/items.js (history handler, ~L736-746)
- **Category:** security
- **Source:** security-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:** The status history rows were built by string concatenation and passed to `tbody.append()`. That HTML-parses `notes` and `justification`, which are free text users type in the transition modal. This branch touched the line when it added `statusLabels`.

**Why this matters:** Stored XSS. The script runs for anyone who opens the order history.

**How it was resolved:** Each cell is now built with `$('<td>').text(...)`.

### FINDING-2
- **Severity:** Important
- **Confidence:** 78
- **File:** MariCamiStore/Pages/Orders/Items.cshtml:49, MariCamiStore/Pages/Orders/Index.cshtml.cs:40,70, MariCamiStore/Model/OrderStatus.cs
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:** These pages showed status labels with `OrderStatus.FromKey(...).Name`, which throws for the removed `Delivered` key. If code ships before the migration runs (Program.cs does not migrate on startup), any unmigrated order returns HTTP 500 on its Items page and breaks the Orders list load.

**Why this matters:** A deploy-order mistake would take order pages down.

**How it was resolved:** Added `OrderStatus.LabelFor(key)`, which never throws. It returns the name, "Entregada" for the legacy key, or the raw key, and all three display sites now use it. The deploy-order note in quickstart.md was updated. Unmigrated orders still cannot transition and are hidden from the filter until the migration runs, so the migration must still run before or with the deploy.

### FINDING-3
- **Severity:** Minor
- **File:** MariCamiStore/wwwroot/js/pages/orders/items.js (openViewItem / restoreItemModalEditable)
- **Category:** architecture
- **Source:** architecture-agent
- **Resolution:** fixed (round 1). Removed the redundant `$('#item-customer').prop('disabled', ...)` lines, which the `.modal-body` select already covers.

### FINDING-4
- **Severity:** Minor
- **File:** MariCamiStore/Services/SalesGoalService.cs (GetHistoryAsync / BuildDto)
- **Category:** architecture
- **Resolution:** remaining. The FR-015 base selection is written in two places. GetHistoryAsync builds DTOs with placeholder values and then fills them with `with`. A refactor to one helper is optional.

### FINDING-5
- **Severity:** Minor
- **File:** MariCamiStore/wwwroot/js/pages/orders/items.js (restoreItemModalEditable)
- **Category:** architecture
- **Resolution:** remaining. The "no product types" state is inferred from the DOM, duplicating state the open functions already own. Behavior is correct today.

### FINDING-6
- **Severity:** Minor
- **File:** MariCamiStore/Pages/Orders/Index.cshtml.cs:88-94 + MariCamiStore/Services/OrderService.cs:314-321
- **Category:** architecture
- **Resolution:** remaining (pre-existing). The transition map is duplicated. Both copies match FR-002.

### FINDING-7
- **Severity:** Minor
- **File:** MariCamiStore/wwwroot/js/pages/orders/items.js:13-14
- **Category:** architecture
- **Resolution:** remaining. The client-side `statusLabels` duplicates the server `OrderStatus` names. This is acceptable per the contract (ui-handlers.md says the client maps keys to labels).

### FINDING-8
- **Severity:** Minor
- **File:** sales-goals Current/History .cshtml + .js
- **Category:** architecture
- **Resolution:** remaining. `.goal-highlight`, `fmtRate` and the commission status-to-class mapping are duplicated per page, which follows the existing per-page convention.

### FINDING-9
- **Severity:** Minor
- **File:** MariCamiStore/Services/OrderService.cs:306-310
- **Category:** security
- **Resolution:** remaining (pre-existing, not touched by this branch). `GetOrderStatusHistoryAsync` has no organization filter on `OrderStatusHistory`, so history can be read across organizations if an order id is known.

### FINDING-10
- **Severity:** Minor
- **File:** MariCamiStore/wwwroot/js/pages/customers/index.js
- **Category:** production-readiness
- **Resolution:** remaining. `customerCache` lives for the whole page. Customers created elsewhere (other users or tabs, the Items page) do not appear until reload, and each grid CRUD action refetches the full catalog.

### FINDING-11
- **Severity:** Minor
- **File:** MariCamiStore/Services/SalesGoalCommission.cs and others
- **Category:** test-quality
- **Resolution:** remaining. The repo has no test project. The SC-003 boundaries, rounding, transition table and migration have no automated coverage. `SalesGoalCommission` is pure and cheap to test.

### FINDING-12
- **Severity:** Minor (pre-review, Stage 1)
- **File:** MariCamiStore/wwwroot/js/pages/sales-goals/current.js, history.js
- **Category:** correctness (spec display)
- **Resolution:** fixed before deep review. The commission rate was shown as "5,0%" and now shows as "5%" / "2,5%", matching the US2 scenarios.

## Post-Fix Spec Coverage

The fix loop removed only two redundant lines, both covered by an existing selector. No requirement code was removed. All spec requirements were verified after the fix loop: FR-001 to FR-022 still map to OrderStatus.cs, OrderService.cs, Index.cshtml.cs, items.js, SalesGoalCommission.cs, SalesGoalService.cs, the sales-goals views/JS, and customers/index.js.

## Test Suite Results

No test command detected; the post-fix test step was skipped. `dotnet build` passed (0 errors, 6 pre-existing warnings) after the fixes.

## Remaining Findings

No Critical or Important findings remain. The 8 remaining Minor findings above are advisory.
