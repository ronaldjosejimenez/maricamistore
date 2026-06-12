# Research: Order Items Mejoras

**Feature**: `specs/004-order-items-mejoras`
**Date**: 2026-06-06

---

## D1: Grid Grouping — Approach

**Decision**: Replace jsGrid with a custom `<table>` rendered by JavaScript.

**Rationale**: jsGrid does not natively support group header rows or subtotal rows. The two alternatives were:
1. Post-process the jsGrid DOM after render to inject group rows (fragile, breaks when jsGrid re-renders).
2. Use a custom table built from the AJAX data (clean, full control over rows).

Option 2 is chosen. The data loading stays via the same `?handler=Load&orderId=...` AJAX call. The `jsGrid('loadData')` pattern is replaced by a `loadItems()` function that fetches data and renders a custom table. The jsGrid widget is removed from `Items.cshtml`.

**Alternatives considered**:
- jsGrid DOM post-processing: rejected (fragile, hard to maintain).
- DataTables with grouping plugin: rejected (introduces a new dependency not used in the rest of the project).

---

## D2: Client Name in Grid — Sort/Group Strategy

**Decision**: Return `CustomerDisplayName` from the backend in `OnGetLoadAsync`, pre-sorted by display name ASC then `CreatedAt` DESC.

**Rationale**: The frontend already loads all customers for the dropdown. However, the `customerItems` array is loaded asynchronously. To avoid a race condition between items loading and customers loading (which could cause the group headers to render incorrectly), the safest approach is to include `CustomerDisplayName` directly in the items response from the backend.

The backend does a JOIN (or EF Include) on `Customers` to get `NickName ?? Name ?? Id.ToString()`.

**Alternatives considered**:
- Client-side sort using the already-loaded `customerItems` array: rejected (race condition risk — items may load before customers).
- Separate endpoint for customers per order: rejected (overcomplicated).

---

## D3: IsReceived Toggle — API Design

**Decision**: New `POST ?handler=ToggleReceived` handler with payload `{ itemId, isReceived }`.

**Rationale**: The `IsReceived` state change is a distinct operation from full item update (which carries many fields including image). A lightweight dedicated handler avoids re-sending all item data for a simple boolean toggle.

**Alternatives considered**:
- Reuse the existing `?handler=Update` endpoint: rejected (requires sending all fields including image handling logic for a boolean change).

---

## D4: Reactive Pricing (RealPrice ← ListPrice)

**Decision**: Add a `userEditedRealPrice` flag at file scope (mirroring the existing `userEditedAgreed` pattern).

**Rationale**: The existing `items.js` already has a `userEditedAgreed` flag with the same semantic. The reactive-pricing requirement (FR-007/FR-007b/FR-008) follows the identical pattern. This is the minimal, consistent change.

**Current state**: The existing handler only updates `RealPrice` when it equals 0:
```js
if (!$('#item-real-price').val() || parseFloat($('#item-real-price').val()) === 0) {
    $('#item-real-price').val(lp.toFixed(2));
}
```
This is insufficient for the "new item" case (user types 25, then changes to 30 — RealPrice stays at 25).

**New behavior**:
- `openAddItem()`: `userEditedRealPrice = false`
- `openEditItem(item)`: `userEditedRealPrice = (item.realPrice !== item.listPrice)` (FR-007b)
- `#item-real-price.on('input')`: `userEditedRealPrice = true`
- `#item-list-price.on('input')`: if `!userEditedRealPrice` → update `#item-real-price`

---

## D5: Item Counter

**Decision**: A `<span id="item-count-badge">` in the card header of the "Artículos" card, updated via the `loadItems()` function.

**Rationale**: Simple badge pattern, consistent with AdminLTE. No additional dependencies needed.

---

## D6: ProductLink Column in Grid

**Decision**: Add a `ProductLink` column to the custom table (rendered as a clickable anchor when non-empty), with a minimum width of 250px.

**Rationale**: The current jsGrid config has no `productLink` column. Since we're replacing jsGrid with a custom table, adding this column is straightforward. In the modal, move `ProductLink` to `col-md-12` (full row width) by removing the `ProductSourceCode` from the same row.

---

## D7: Order Status for Checkbox Visibility

**Decision**: Pass `orderStatus` from C# to JavaScript as a page-scope variable (similar to `isPending`).

**Rationale**: The existing pattern already passes `isPending` as a JS variable. Adding `orderStatus` (the raw status string) allows the JS to check for `Delivering` or `Delivered` without backend round-trips.

---

## Summary Table

| # | Decision | Chosen Approach |
|---|----------|-----------------|
| D1 | Grid grouping | Custom `<table>` replacing jsGrid |
| D2 | Customer name / sort | Backend returns `CustomerDisplayName`, pre-sorted |
| D3 | IsReceived toggle | New `POST ?handler=ToggleReceived` |
| D4 | Reactive pricing | `userEditedRealPrice` flag, mirrors existing pattern |
| D5 | Item counter | Badge in card header, updated on load |
| D6 | ProductLink in grid | Column in custom table + modal full-width |
| D7 | Status for checkbox | Pass `orderStatus` to JS from Razor |
