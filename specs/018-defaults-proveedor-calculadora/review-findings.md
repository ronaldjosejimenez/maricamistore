# Deep Review Findings

**Date:** 2026-10-25
**Branch:** 018-defaults-proveedor-calculadora
**Rounds:** 3
**Gate Outcome:** FAIL (1 Important remaining after round 3)
**Invocation:** superpowers (quality gate from `/speckit-implement`)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 9 | 8 | 1 |
| Minor | 22 | 16 | 6 |
| **Total** | **31** | **24** | **7** |

**Agents completed:** 5/5 + 3 re-review rounds. External tools: none (CodeRabbit and Copilot CLIs are not installed).
**Agents failed:** none.

## Findings

### FINDING-1 — Legacy items treated as edited by hand (Trigger A loses tax and shipping)
- **Severity:** Important · **Confidence:** 72 · **Category:** correctness · **Source:** correctness-agent · **Round found:** 1
- **File:** MariCamiStore/Services/OrderService.cs (RecalcItemsOnOrderSaveAsync), wwwroot/js/pages/orders/items.js (openEditItem)
- **Resolution:** fixed (round 1 by migration, replaced in round 2, narrowed in round 3)

**What is wrong:** Items saved before 018 have RealPrice = ListPrice (the old default). The new "manual edit" check compared against Precio + Imp + Envío, so those items were treated as edited by hand. A TC change then forced Acordado = ListPrice × TC + fee, dropping tax and shipping.
**Why this matters:** Precio Acordado, Precio Acordado Total and Ganancia would silently drop on pending orders.
**How it was resolved:** Round 1 rewrote the data in the migration. That contradicted the spec ("no se recalculan en lote"), so round 2 removed it. Instead, the legacy defaults now count as automatic: RealPrice ≈ ListPrice, and Agreed ≈ (Precio + Imp) × TC + fee. Round 3 limited this to items created before 018. The spec documents this edge case.

### FINDING-2 — Dead code `calcItem`
- **Severity:** Important · **Confidence:** 85 · **Category:** architecture · **Round found:** 1 · **Resolution:** fixed (round 1)
- **File:** wwwroot/js/pages/orders/items.js

**What is wrong:** `calcItem` had no callers but was still updated, and its rule (always overwrite RealPrice) differed from the real flow.
**Why this matters:** It was a third copy of the formula, and it was already inconsistent.
**How it was resolved:** Deleted.

### FINDING-3 — RealPriceInLocal formula duplicated across layers
- **Severity:** Important · **Confidence:** 80 · **Category:** architecture · **Round found:** 1 · **Resolution:** fixed (round 1)
- **File:** Pages/Orders/Items.cshtml.cs, Services/OrderService.cs

**What is wrong:** The Insert and Update handlers and Trigger A each computed `Round(RealPrice × TC, 2)`.
**Why this matters:** The copies would drift apart.
**How it was resolved:** `OrderService.ApplyRealPriceInLocal` is now the single source, used by Create, Update and Trigger A.

### FINDING-4 — Deploy pipeline does not apply migrations
- **Severity:** Important · **Confidence:** 78 · **Category:** production-readiness · **Round found:** 1 · **Resolution:** fixed (documented, round 1). Needs human action at deploy time.
- **File:** .github/workflows/deploy-azure.yml

**What is wrong:** If the code is deployed before the migration runs, every query that touches Suppliers or OrderItems fails ("Invalid column name").
**Why this matters:** Orders, Items, Suppliers and the Calculator would all go down.
**How it was resolved:** Added a note to quickstart.md and a checklist item to REVIEWERS.md: run the idempotent script before deploying. The old code works against the new schema, so running the migration first is safe.

### FINDING-5 — Quickstart contradicts FR-004 (Shein active)
- **Severity:** Important · **Confidence:** 92 · **Category:** test-quality · **Round found:** 1 · **Resolution:** fixed (round 1)
- **File:** specs/018-defaults-proveedor-calculadora/quickstart.md

**What is wrong:** Step 1.1 expected every supplier to be inactive, and step 1.4 switched Shein on by hand, which hid what the migration did.
**How it was resolved:** Quickstart rewritten. It now also covers the rounding boundaries, the FR-018a branches, the supplier validations and the calculator edge cases.

### FINDING-6 — Migration rewrote RealPrice in bulk (spec forbids it)
- **Severity:** Important · **Confidence:** 85 · **Category:** correctness · **Round found:** 2 · **Resolution:** fixed (round 2)
- **File:** Migrations/20261025231321_SupplierDefaultsAndRealPriceInLocal.cs

**What is wrong:** The round-1 fix for FINDING-1 updated RealPrice in pending orders but left Agreed as it was, so Agreed then looked edited by hand.
**How it was resolved:** Bulk update removed; replaced by the legacy-default detection described in FINDING-1.

### FINDING-7 — Decimals in the Items script block depended on server culture
- **Severity:** Important · **Confidence:** 80 · **Category:** correctness · **Round found:** 2 · **Resolution:** fixed (round 2)
- **File:** Pages/Orders/Items.cshtml

**What is wrong:** On a comma-decimal culture such as es-CR, `var exchangeRate = 520,50;` is a JavaScript syntax error.
**How it was resolved:** All decimals emitted into JS (savedTotals plus exchangeRate, taxPercentage, shippingAmountIntern and discountAmount) now use `ToString(CultureInfo.InvariantCulture)`.

### FINDING-8 — Automatic Acordado went stale on tax-only changes
- **Severity:** Important · **Confidence:** 70 · **Category:** correctness · **Round found:** 3 · **Resolution:** fixed (round 3)
- **File:** Services/OrderService.cs

**What is wrong:** With an Imp-only change, RealPrice and RealPriceInLocal were recalculated but Acordado (never edited by hand) was not. The item was then flagged as edited by hand for good.
**How it was resolved:** `agreedIsAuto` is computed before the recalculation, and Acordado is recalculated when `exchangeRateChanged || agreedIsAuto`. FR-018a and the quickstart were updated (Imp 13 gives Acordado 10722.00). Verified end to end.

### FINDING-9 — Legacy-item cutoff fixed at the migration date, not the deploy date ⚠️ REMAINING
- **Severity:** Important · **Confidence:** 75 · **Category:** correctness · **Round found:** 4 (final re-review) · **Resolution:** remaining
- **File:** Services/OrderService.cs (`LegacyItemsCutoffUtc`), wwwroot/js/pages/orders/items.js (`legacyItemsCutoff`)

**What is wrong:** The cutoff is 2026-10-25 23:13:21 UTC, when the migration was generated. Production keeps running the old code until the merge and deploy. Items created in that window have the old defaults but will not be treated as legacy.
**Why this matters:** For those items, Precio Real and Acordado stay frozen as if edited by hand: in Trigger A and in the modal.
**What needs to happen:** Mark legacy items when the migration runs, not with a constant. For example, add an `OrderItems.HasLegacyPriceDefaults bit` column in the 018 migration with `UPDATE ... SET 1` for every existing row, use it in `RecalcItemsOnOrderSaveAsync`, and expose it in `OrderItemWithCustomerDto` for the JS. A lighter alternative is to update the constant to the actual deploy date.

### Minor findings

| # | Source | Description | Resolution |
|---|--------|-------------|------------|
| M1 | security | Currency sign rendered as HTML in the suppliers grid (stored XSS) | fixed: `$('<span>').text()` |
| M2 | correctness | The calculator did not round per step like the item chain (FR-010a) | fixed |
| M3 | correctness | An empty Impuesto saved as 0 | fixed: `validate: 'required'` plus `undefined` on empty |
| M4 | production | savedTotals depended on culture | fixed (together with FINDING-7) |
| M5 | production | GET loads without a `.fail` (calculator, suppliers) | fixed |
| M6–M11 | test-quality | Quickstart gaps (rounding boundaries, FR-018a, supplier validations, calculator edge cases, Ver mode, inexact inputs) | fixed |
| M12 | correctness (r2) | Midpoint: server ToEven vs client half-up | fixed: AwayFromZero |
| M13 | correctness (r2) | Removed the "Orden no encontrada" check on insert | fixed: restored |
| M14 | correctness (r3) | Legacy false positive on new items (RealPrice = Lista typed by hand) | fixed: limited by CreatedAt |
| M15 | correctness (r3) | round2 vs AwayFromZero | fixed partly (see M21) |
| M16 | correctness (r3) | Header totals rounded ToEven on the server | fixed |
| M17 | architecture | Calculator namespace and title still say "Shein" | **remaining** (open question for the user) |
| M18 | architecture + production | Reasignar runs 2 extra queries, one of them loading ProductImage | **remaining** |
| M19 | architecture | `DecimalField` registered globally but defined in one page | **remaining** |
| M20 | architecture | Shein activated by both seed and SQL (Id vs Name) | **remaining** (intentional: covers a Shein created by hand) |
| M21 | correctness (final) | `round2` with EPSILON still differs by 1 cent from the server on some midpoints | **remaining** (cosmetic, ±0.01) |
| M22 | security | `CurrencyIdDefault` is not checked against Currencies, and there are no upper bounds | **remaining** |

## Post-Fix Spec Coverage

The fix rounds removed code (`calcItem`, the RealPriceInLocal assignments in the handlers, and the bulk SQL). Coverage was checked again:

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| FR-001–FR-004 | Supplier model and config, CatalogService.ValidateSupplier, suppliers/index.js, migration SQL | ✓ |
| FR-005–FR-012 | Calculator/Index.cshtml(.cs), calculator/index.js | ✓ |
| FR-013–FR-014 | orders/index.js applySupplierDefaults | ✓ |
| FR-015–FR-016 | Items.cshtml | ✓ |
| FR-017–FR-019a | items.js recalcModal, OrderService.ApplyRealPriceInLocal (Create/Update) | ✓ |
| FR-018a | OrderService.RecalcItemsOnOrderSaveAsync | ✓ (verified end to end) |
| FR-020–FR-020a | items.js setPriceFieldsEnabled, table Total, openEditItem | ✓ |
| FR-021–FR-023 | ApplyOrderCalculationSuite, recalcOrderHeader, refreshTotals/savedTotals, UpdateOrderTotalsAsync guard, Reasignar response | ✓ |

All spec requirements were verified after the fix loop.

## Test Suite Results

There is no automated test project; no test command was detected, so the post-fix test step was skipped. As a substitute, each round ran a local end-to-end script over HTTP and SQL:
- create order, insert item: 13.70 / 7261.00 / 10261.00
- TC 540: 7398.00 / 10398.00; header Subtotal 10.00, Imp 0.70, Envío 3.00
- Imp 13: 11.30 / 14.30 / 7722.00 / 10722.00
- legacy item with TC 550: 14.30 / 7865.00 / 10865.00
- invalid OrderId: JSON error
- UpdateTotals on a Completed order: database unchanged

It passed in all 3 rounds.

## Remaining Findings

- **FINDING-9 (Important):** see above. This needs a decision: add a column marked by the migration (recommended), or set the cutoff to the actual deploy date.
- **M17–M22 (Minor):** non-blocking. M17 depends on a user decision about the calculator title.
