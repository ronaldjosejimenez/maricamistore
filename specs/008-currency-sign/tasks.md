# Tasks: Signo de Moneda + Formateo de Montos

**Feature**: 008-currency-sign | **Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

---

## Phase 1 — Modelo y Migración

- [X] T001 [P] [US3] Agregar propiedad `Sign` a `Currency.cs`: añadir `public string Sign { get; set; } = string.Empty;` después de `Abbreviation`.
- [X] T002 [P] [US3] Actualizar `CurrencyEntityTypeConfiguration.cs`: mapear `Sign` con `HasMaxLength(10).IsRequired(false)` y actualizar `HasData` para incluir `Sign = "₡"` en Colones y `Sign = "$"` en Dólares (GUIDs de seed existentes).
- [X] T003 [P] [US3] Generar y aplicar migración EF Core: ejecutar `dotnet ef migrations add AddCurrencySign --project MariCamiStore` y verificar que `Up()` contiene `AddColumn` para `Sign` y dos `UpdateData` con los signos. Aplicar con `dotnet ef database update --project MariCamiStore`.

## Phase 2 — Helpers e Infraestructura de Servicio

- [X] T004 [P] [US1] Crear `MariCamiStore/Helpers/AmountFormatter.cs`: helper estático con método `Format(decimal amount, string sign)` usando cultura `es-CR` (`N2`). Retorna `"{sign} {formatted}"` cuando sign es no vacío, o solo `"{formatted}"` cuando está vacío.
- [X] T005 [P] [US1] Agregar `formatMoney(amount, sign)` a `MariCamiStore/wwwroot/js/utilities.js`: función JavaScript que usa `toLocaleString('es-CR', {minimumFractionDigits:2, maximumFractionDigits:2})` y retorna `sign + ' ' + formatted` cuando sign es truthy, o solo `formatted` cuando no. Añadir después de la función `ParseDataSourceToJson` existente.
- [X] T006 [P] [US1] Verificar que `utilities.js` está incluido globalmente en `MariCamiStore/Pages/Shared/_Layout.cshtml`. Si no está, agregar `<script src="/js/utilities.js"></script>` antes de los scripts de página para que `formatMoney` esté disponible globalmente.
- [X] T007 [P] [US1] Agregar `GetCurrencyByIdAsync` a `ICatalogService.cs` (`Task<Currency?> GetCurrencyByIdAsync(Guid id);`) e implementar en `CatalogService.cs` (`public async Task<Currency?> GetCurrencyByIdAsync(Guid id) => await context.Currencies.FindAsync(id);`).

## Phase 3 — Currencies UI (US2)

- [X] T008 [US2] Actualizar `MariCamiStore/wwwroot/js/pages/currencies/index.js`: agregar campo `{ name: 'sign', title: 'Signo', type: 'text', width: 80 }` al array `fields` de jsGrid, después del campo `abbreviation`. El backend ya maneja `Currency.Sign` al inyectar el modelo vía `[FromBody]`.

## Phase 4 — Pantallas Afectadas (US1)

- [X] T009 [US1] **Orders/Index — PageModel**: cambiar `OnGet` a `async Task<IActionResult> OnGetAsync()` en `Pages/Orders/Index.cshtml.cs`, agregar llamada a `GetLocalCurrencySignAsync()` (usando `ICatalogService` ya inyectado) y asignar resultado a `ViewData["LocalCurrencySign"]`.
- [X] T010 [US1] **Orders/Index — Vista**: agregar `<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>` antes del tag `<script src="...">` en `Pages/Orders/Index.cshtml`.
- [X] T011 [US1] **Orders/Index — JS**: actualizar `MariCamiStore/wwwroot/js/pages/orders/index.js` para usar `itemTemplate: function(val) { return formatMoney(val, localCurrencySign); }` en los campos `totalOfTheOrder` y `estimatedProfitInLocal` del jsGrid (reemplazar renderizado `.toFixed(2)` actual). **Nota**: FR-005 menciona `TotalAgreedPriceInLocal` para esta pantalla, pero actualmente ese campo no aparece como columna en el jsGrid (aunque sí se retorna en el JSON del backend). El formateo con signo aplica únicamente a las columnas que se renderizan en la grilla; si se agrega la columna `totalAgreedPriceInLocal` en el futuro, debe usarse `formatMoney(val, localCurrencySign)`.
- [X] T012 [US1] **Orders/Items — PageModel**: en `Pages/Orders/Items.cshtml.cs`, dentro de `OnGetAsync`, resolver `ViewData["LocalCurrencySign"]` con `GetLocalCurrencySignAsync()` y `ViewData["OrderCurrencySign"]` con `catalogService.GetCurrencyByIdAsync(Order.CurrencyId)`. `ICatalogService` ya está inyectado.
- [X] T013 [US1] **Orders/Items — Vista**: extender el bloque `<script>` existente en `Pages/Orders/Items.cshtml` con las dos variables nuevas: `var localCurrencySign = '@ViewData["LocalCurrencySign"]';` y `var orderCurrencySign = '@ViewData["OrderCurrencySign"]';`.
- [X] T014 [US1] **Orders/Items — JS**: actualizar `MariCamiStore/wwwroot/js/pages/orders/items.js`: en `recalcOrderHeader`, reemplazar todos los renders de monto con `formatMoney(..., localCurrencySign)`. En `renderItemsTable`, usar `localCurrencySign` para columnas locales (`serviceFeeInLocal`, `agreedPriceInLocal`, subtotales, grand total) y `orderCurrencySign` para columnas de proveedor (`listPrice`, `listPriceTaxWithTax`, `realPrice`, `estimateShipping`, total de ítem).
- [X] T015 [US1] **Payments/Index — PageModel**: agregar `ICatalogService catalogService` al constructor de `Pages/Payments/Index.cshtml.cs`, cambiar `OnGet` a `async Task<IActionResult> OnGetAsync()`, resolver y asignar `ViewData["LocalCurrencySign"]`.
- [X] T016 [US1] **Payments/Index — Vista**: agregar `<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>` antes del tag `<script src="...">` en `Pages/Payments/Index.cshtml`.
- [X] T017 [US1] **Payments/Index — JS**: actualizar `MariCamiStore/wwwroot/js/pages/payments/index.js` para usar `formatMoney(..., localCurrencySign)` en: handler de cambio de cliente (`#balance-global`, `#balance-org`), handler de éxito de registro de pago (`#balance-global`, `#balance-org`), y función `renderSaldos` (balance por cliente y fila de Total).
- [X] T018 [US1] **Transactions/Index — PageModel**: agregar `ICatalogService catalogService` al constructor de `Pages/Transactions/Index.cshtml.cs`, cambiar `OnGet` a `async Task<IActionResult> OnGetAsync()`, resolver y asignar `ViewData["LocalCurrencySign"]`.
- [X] T019 [US1] **Transactions/Index — Vista**: agregar `<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>` antes del tag `<script src="...">` en `Pages/Transactions/Index.cshtml`.
- [X] T020 [US1] **Transactions/Index — JS**: actualizar `MariCamiStore/wwwroot/js/pages/transactions/index.js` para usar `formatMoney(..., localCurrencySign)` en la función `renderTransactions` para la celda de `transactionAmount` y la fila de Total.
- [X] T021 [US1] **ProductTypes/Index — PageModel**: en `Pages/ProductTypes/Index.cshtml.cs`, cambiar `OnGet` a `async Task<IActionResult> OnGetAsync()`. Resolver la moneda local inline (sin `GetLocalCurrencySignAsync()` heredado ya que no hereda `OrganizationPageModel`): obtener config, luego `GetCurrencyByIdAsync(config.LocalCurrencyId)`, asignar `ViewData["LocalCurrencySign"]`. `ICatalogService` ya está inyectado.
- [X] T022 [US1] **ProductTypes/Index — Vista**: agregar `<script>var localCurrencySign = '@ViewData["LocalCurrencySign"]';</script>` antes del tag `<script src="...">` en `Pages/ProductTypes/Index.cshtml`.
- [X] T023 [US1] **ProductTypes/Index — JS**: actualizar `MariCamiStore/wwwroot/js/pages/product-types/index.js`: en el callback de carga de monedas (`$.get('/Currencies?handler=Load')`), mapear cada ítem para incluir `sign: c.sign || ''`. En `initGrid`, cambiar `serviceFeeInLocal` a `itemTemplate: function(val) { return formatMoney(val, localCurrencySign); }` y `estimateShipping` a `itemTemplate: function(val, item) { var curr = currencyItems.find(c => c.id === item.currencyId); return formatMoney(val, curr ? curr.sign : ''); }`.

---

## Dependencies

- T002 depende de T001
- T003 depende de T002
- T004 depende de T001
- T007 depende de T001
- T008 depende de T001 y T003
- T009 depende de T007
- T010 depende de T009
- T011 depende de T005, T006, T010
- T012 depende de T007
- T013 depende de T012
- T014 depende de T005, T006, T013
- T015 depende de T007
- T016 depende de T015
- T017 depende de T005, T006, T016
- T018 depende de T007
- T019 depende de T018
- T020 depende de T005, T006, T019
- T021 depende de T007
- T022 depende de T021
- T023 depende de T005, T006, T022

---

## Implementation Strategy

**MVP (P1 — US1 + US3)**: Completar en orden T001 → T002 → T003 → T005 → T006 → T007, luego las fases de cada pantalla (T009–T023). Esto cubre los requisitos prioritarios: signos en base de datos y visualización en todas las pantallas afectadas.

**P2 (US2)**: T008 (columna Sign en jsGrid de Currencies) puede implementarse en paralelo a Phase 4 una vez que T001, T003 estén listos.

**Paralelo posible**: T004 (AmountFormatter C#) puede hacerse en cualquier momento después de T001 sin bloquear otras tareas, ya que es un helper server-side para uso futuro no requerido en el scope actual.

**Orden recomendado de Phase 4**: Orders/Index → Orders/Items → Payments/Index → Transactions/Index → ProductTypes/Index. Cada grupo de 3 tareas (PageModel + Vista + JS) debe completarse en conjunto antes de verificar esa pantalla.
