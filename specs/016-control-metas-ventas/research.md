# Research: Control de Metas de Ventas

## R1. Distribution algorithm from the reference Excel (Oct 2026, goal 4 000 000)
- **Observation**: all 31 daily amounts are multiples of 4 000 and sum to 4 000 000. Weights (amount/4 000) by week: Thu 28,27,31,30,32; Fri 37,34,35,37,38; Sat 52,48,53,50,56; Sun 39,32,38,35; Mon 20,18,18,19; Tue 21,22,22,24; Wed 25,25,26,28.
- **Weekday averages**: Mon 18.75, Tue 22.25, Wed 26, Thu 29.6, Fri 36.2, Sat 51.8, Sun 36 → base weights 19/22/26/29/36/51/36.
- **Trend**: the same weekday rises ~8–14 % from first to last occurrence → linear ramp 0.94→1.06.
- **Decision (revised)**: weights indexed by (weekday, occurrence in month) copied from the sheet; exact for Oct 2026, extrapolated for other months. First version of this feature used average weekday weights × a linear ramp, which did not reproduce the sheet; replaced because phase 1 must match the reference.
- **Alternatives**: weekday-only (doesn't reproduce trend); fixed 31-weight template (breaks across months). Rejected.
- **Open**: only one month of data; constants isolated for tuning.

## R2. Time zone
- Existing code already uses `TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central America Standard Time")`. **Decision**: centralize in `Helpers/CostaRicaClock` and use it for all goal logic. IANA id `America/Costa_Rica` rejected for consistency with existing, working code.

## R3. Concurrency on create
- **Decision**: unique index `(SalespersonId, OrganizationId, Year, Month)`; create master + days in one `SaveChanges`; on `DbUpdateException` clear tracker and re-read the existing goal. App-level lock rejected: multiple instances possible.

## R4. Menu placement
- Layout has top-level "Catálogos" (Tipos de Producto, Proveedores) and "Ventas" (Órdenes). **Decision**: "Vendedores" under Catálogos; "Metas" nested treeview under Ventas with "Mes Actual" and "Histórico de metas".

## R5. Configuration seed
- `Configuration` has `HasData` rows; adding a non-null decimal column means seeds must set `DefaultMonthlyGoal = 4000000` and the column has default 4 000 000 so existing rows migrate correctly.

## R6. UI technique
- Catalog: jsGrid, same as Customers. Mes Actual / Histórico: custom Razor + jQuery (like CxP) because of the header card, traffic light and blur-save editable cells. Amounts formatted with the existing es-CR / `AmountFormatter` convention.
