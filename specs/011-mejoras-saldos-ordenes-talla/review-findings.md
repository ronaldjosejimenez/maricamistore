# Deep Review Findings

**Date:** 2026-09-28
**Branch:** 011-mejoras-saldos-ordenes-talla
**Rounds:** 0 (no Critical/Important findings; fix loop not entered)
**Gate Outcome:** PASS
**Invocation:** quality-gate (ship pipeline)
**Stage 1 spec compliance:** 100% (FR-001..FR-019 incl. FR-005a, all edge cases)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 0 | 0 | 0 |
| Minor | 6 | - | 6 |
| **Total** | **6** | **0** | **6** |

**Agents:** 5/5 perspectives run as 2 combined agents (Correctness + Production Readiness; Architecture + Security + Test Quality). External tools: CodeRabbit/Copilot skipped (disabled / not installed). No test project, so the post-fix test step was skipped.

## Findings

### FINDING-1
- **Severity:** Minor | **Confidence:** 80 | **Category:** correctness/architecture
- **File:** MariCamiStore/wwwroot/js/pages/orders/index.js:53-57
- **Source:** correctness agent (also reported by: architecture agent)
- **Resolution:** remaining

**What is wrong:** The `onError` handler returns early on abort and does nothing for any other error. The comment suggests other errors are handled, but they are not.

**Why this matters:** A failed reload (500 or network error) keeps showing the rows for the previous selection while the checkboxes show the new one, and the user gets no message. This matches the previous default behavior (jsGrid's onError is a no-op).

**How to resolve:** Add an else branch that shows an error (for example `alert('Error al cargar las órdenes.')`) and/or clears the grid.

### FINDING-2
- **Severity:** Minor | **Confidence:** 75 | **Category:** production-readiness
- **File:** MariCamiStore/wwwroot/js/pages/orders/index.js:27-37
- **Resolution:** remaining

**What is wrong:** jQuery runs the callbacks of an aborted jqXHR synchronously. The superseded request's `always(_hideLoading)` therefore clears the loading timer that the new request just armed.

**Why this matters:** This is cosmetic only. The loading indicator does not appear after a quick toggle, but FR-013 (last selection wins) is still satisfied.

**How to resolve:** Use a sequence token instead of `abort()`, or re-show the indicator after the abort.

### FINDING-3
- **Severity:** Minor | **Confidence:** 80 | **Category:** architecture
- **File:** MariCamiStore/Pages/Orders/Items.cshtml.cs:118-120,171-173; OrderItemEntityTypeConfiguration.cs:39-42; Items.cshtml:198
- **Resolution:** remaining

**What is wrong:** The size limit 20 is hard-coded in four places, and the validation block is duplicated in the insert and update handlers.

**How to resolve:** Use `OrderItem.SizeMaxLength` as a constant and add a shared normalize helper.

### FINDING-4
- **Severity:** Minor | **Confidence:** 70 | **Category:** architecture
- **File:** MariCamiStore/Model/OrderStatus.cs:30-59
- **Resolution:** remaining

**What is wrong:** Request/UI filter parsing lives in the domain enumeration. `DefaultFilterKeys` allocates a new array on every access.

**How to resolve:** Move the parsing to a page or service helper, or at least use `static readonly`.

### FINDING-5
- **Severity:** Minor | **Confidence:** 70 | **Category:** architecture
- **File:** MariCamiStore/Pages/Orders/Items.cshtml.cs:44-45; Items.cshtml:18
- **Resolution:** remaining

**What is wrong:** The Volver URL is built from untyped `ViewData` with an inline ternary. Encoding is correct.

**How to resolve:** Expose a typed `BackUrl` property on the page model.

### FINDING-6
- **Severity:** Minor | **Confidence:** 70 | **Category:** test-quality
- **File:** MariCamiStore/Model/OrderStatus.cs:40-59
- **Resolution:** remaining

**What is wrong:** `ParseFilter` holds the FR-011/FR-012 rules but has no automated test. The repo has no test project.

## Post-Fix Spec Coverage

The fix loop did not run, so no code was removed. All spec requirements were verified in Stage 1.

## Test Suite Results

No test command detected; the post-fix test step was skipped. `dotnet build` succeeds with 0 errors.
