# Deep Review Findings

**Date:** 2026-06-09
**Branch:** 006-saldos-pagos
**Rounds:** 1
**Gate Outcome:** PASS
**Invocation:** quality-gate

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 1 | 1 | 0 |
| Important | 1 | 1 | 0 |
| Minor | 2 | 0 | 2 |
| **Total** | **4** | **2** | **2** |

**Agents completed:** 5/5 (no external tools — disabled by caller)
**Agents failed:** none

## Findings

### FINDING-1
- **Severity:** Critical
- **Confidence:** 85
- **File:** MariCamiStore/wwwroot/js/pages/payments/index.js:26,32
- **Category:** security
- **Source:** security-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`r.customerName` was injected directly into an HTML string via jQuery `.html()`, without escaping. In `renderSaldos`, the row template was built as:
```javascript
'<tr><td>' + r.customerName + '</td>...'
```
If a customer's `NickName` or `Name` value in the database contains HTML characters such as `<`, `>`, or `"`, they would be rendered as HTML markup rather than displayed as text. A value like `<img src=x onerror=alert(1)>` would execute as JavaScript.

**Why this matters:**
Even though `CustomerName` values are entered by application operators (not untrusted end-users), XSS via stored data is a persistent vulnerability. Any operator with database write access (or any future bug that allows unexpected data entry) could trigger script execution for all users viewing the payments screen. OWASP Top 10 A03 (Injection / XSS).

**How it was resolved:**
Added `escapeHtml(str)` helper using the standard jQuery idiom:
```javascript
function escapeHtml(str) {
    return $('<span>').text(str).html();
}
```
All `r.customerName` references in HTML template strings now pass through `escapeHtml()`. Numeric values (`r.balance.toFixed(2)`) do not require escaping.

---

### FINDING-2
- **Severity:** Important
- **Confidence:** 95
- **File:** MariCamiStore/Services/PaymentService.cs:68
- **Category:** architecture
- **Source:** architecture-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`GetSaldosReportAsync` applied `.OrderByDescending(r => r.Balance)` at the SQL query level (line 68), but then immediately overrode it with an in-memory `.OrderBy(r => r.CustomerName)` at line 80. The SQL-level sort was completely discarded and never visible in results.

```csharp
// Before fix — SQL sort was dead work:
.Where(r => r.Balance > 0)
.OrderByDescending(r => r.Balance)  // ← discarded
.ToListAsync();
// Then:
)).OrderBy(r => r.CustomerName).ToList();  // ← actual final order
```

**Why this matters:**
Dead SQL work misleads future maintainers into thinking the balance-descending sort is intentional or meaningful. It also adds unnecessary load to the SQL Server for every saldo page load. If a developer later moves the in-memory sort or removes it, they might assume the SQL sort is the intended behavior, introducing a subtle ordering bug.

**How it was resolved:**
Removed `.OrderByDescending(r => r.Balance)` from the LINQ-to-SQL query. The final in-memory `.OrderBy(r => r.CustomerName)` at line 79 remains and correctly implements FR-008.

---

### FINDING-3
- **Severity:** Minor
- **Confidence:** 80
- **File:** MariCamiStore/wwwroot/js/pages/payments/index.js:13-35
- **Category:** correctness
- **Source:** correctness-agent
- **Resolution:** pending (minor, not auto-fixed)

**What is wrong:**
`renderSaldos` shows the "No hay saldos pendientes." empty-state message only when `data.length === 0` (no customers with balance). When the filter is active and produces zero matching rows, the function renders an empty table body with only a "Total: 0.00" row — no user-facing message that the filter produced no results.

**Why this matters:**
Minor UX confusion — the user types a search term, sees only a "Total: 0.00" row, and must infer that no customers matched. The spec does not explicitly require a "no filter results" message, so this is not a spec violation, but it is a usability gap.

**Recommended fix (not auto-applied):**
```javascript
if (filtered.length === 0) {
    $('#saldos-table-container').html('<p class="p-3 text-muted">No hay clientes que coincidan con el filtro.</p>');
    return;
}
```
Add this check after computing `filtered`, before building the table HTML.

---

### FINDING-4
- **Severity:** Minor
- **Confidence:** 70
- **File:** MariCamiStore/wwwroot/js/pages/payments/index.js:13
- **Category:** architecture
- **Source:** architecture-agent
- **Resolution:** pending (minor, not auto-fixed)

**What is wrong:**
The `renderSaldos(data)` function signature implies `data` is the dataset to render, but the function always reads the filter value from `$('#saldos-filter').val()` and applies it internally. The parameter `data` is always the full unfiltered `allSaldosData`, never pre-filtered.

**Why this matters:**
Naming mismatch creates a minor cognitive burden for future maintainers who may expect to pass pre-filtered data to `renderSaldos`. Low risk in a small file where the full context is visible.

**Recommended fix (not auto-applied):**
Rename parameter to `sourceData` or `allData` to make the intent clear.

---

## Post-Fix Spec Coverage

All spec requirements verified after fix loop.

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| FR-001: /Payments shows saldos table | Index.cshtml: saldos section | ✓ |
| FR-002: AJAX GET ?handler=Saldos on load | index.js: loadSaldos() in $(function) | ✓ |
| FR-003: Columns Cliente + Saldo Pendiente, ordered by CustomerName ASC | PaymentService.cs:79, index.js:29 | ✓ |
| FR-004: Total row at end of table | index.js:31-32 | ✓ |
| FR-005: #saldos-filter real-time filter (no server calls) | index.js:89 + renderSaldos filter logic | ✓ |
| FR-006: loadSaldos() called after successful payment | index.js:80 | ✓ |
| FR-007: OnGetSaldosAsync returns List<SaldoReportRow> as JSON | Index.cshtml.cs:20-23 | ✓ |
| FR-008: GetSaldosReportAsync orders by CustomerName ASC | PaymentService.cs:79 | ✓ |
| FR-009: Reports/Saldos.cshtml and .cshtml.cs deleted | Pages/Reports/ directory empty | ✓ |
| FR-010: Saldos menu item removed from navigation | _Layout.cshtml lines 238-243 | ✓ |

## Test Suite Results

No test command detected; post-fix test step was skipped. (Project has no automated test suite — manual testing only, per plan.md.)

## Remaining Findings

No Critical or Important findings remain. Gate PASSED after round 1.

Minor findings (informational, not blocking):
- FINDING-3: Empty filter produces no "no results" message (Minor UX gap, not spec violation)
- FINDING-4: `renderSaldos` parameter name slightly misleading (Minor naming issue)
