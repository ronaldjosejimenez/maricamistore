---
description: "Task list for 013-paquetes-orden"
---

# Tasks: Paquetes de Envío por Orden (OrderPackage)

**Input**: `specs/013-paquetes-orden/` (plan.md, spec.md, data-model.md, contracts/page-handlers.md, research.md, quickstart.md)

**Tests**: No automated test project; verification = `dotnet build` + quickstart.md (manual).

**Paths**: relative to repository root; web project in `MariCamiStore/`. **Never run `dotnet ef database update`.**

Stories: US1 add package→CxP, US2 Shipping CR Pendientes, US3 delete→reversal, US4 UI by state, US5 retire 009 rule.

---

## Phase 1: Setup

- [X] T001 Run `dotnet build MariCamiStore/MariCamiStore.csproj` to confirm a clean baseline (note existing warnings).

## Phase 2: Foundational (data model + migration; blocks all stories)

- [X] T002 Create `MariCamiStore/Model/CxPEntryType.cs`: `public static class CxPEntryType` with `const string Manual = "Manual", AutoActiva = "AutoActiva", AutoDelivered = "AutoDelivered", SaldoAnterior = "SaldoAnterior", AutoPaquete = "AutoPaquete", ReversoPaquete = "ReversoPaquete";` (XML doc comments like other model files). Replace the string literals `"SaldoAnterior"`, `"Manual"` in `MariCamiStore/Services/CxPService.cs` and `"AutoActiva"` in `MariCamiStore/Services/OrderService.cs` with the constants.
- [X] T003 Create `MariCamiStore/Model/OrderPackage.cs` per data-model.md (Id, OrderId, DeliveryDate, Amount, CurrencyId, Description nullable, CreatedAt, `Order Order` navigation), with XML doc comments; add a `public const int DescriptionMaxLength = 500;`.
- [X] T004 Add `public Guid? OrderPackageId { get; set; }` to `MariCamiStore/Model/CxPEntry.cs`.
- [X] T005 Remove `ActualShippingAmountToCR` from `MariCamiStore/Model/Order.cs` and its `builder.Property(...)` block from `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderEntityTypeConfiguration.cs`.
- [X] T006 Create `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/OrderPackageEntityTypeConfiguration.cs` (plan D1): table `OrderPackages` in `MariCamiStoreContext.DEFAULT_SCHEMA`, key, `DeliveryDate` `HasColumnType("date")`, `Amount` `decimal(18,2)`, `Description` max `OrderPackage.DescriptionMaxLength` optional, `CreatedAt` required, `HasOne(p => p.Order).WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict)` (NOT Cascade: SQL Server error 1785 multiple cascade paths), `HasOne<Currency>().WithMany().HasForeignKey(p => p.CurrencyId).OnDelete(Restrict)`, index on OrderId.
- [X] T007 In `MariCamiStore/Infrastructure/Persistance/EntityConfigurations/CxPEntryEntityTypeConfiguration.cs` add `builder.HasOne<OrderPackage>().WithMany().HasForeignKey(e => e.OrderPackageId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);` and `builder.HasIndex(e => e.OrderPackageId);`.
- [X] T008 In `MariCamiStore/Infrastructure/Persistance/MariCamiStoreContext.cs`: add `public DbSet<OrderPackage> OrderPackages { get; set; }` (with doc comment like others), apply `OrderPackageEntityTypeConfiguration`, add query filter `builder.Entity<OrderPackage>().HasQueryFilter(p => p.Order.OrganizationId == currentOrganizationService.OrganizationId);`.
## Phase 3: US5 — Retire the 009 rule (P2, done early to keep build green)

- [X] T010 [US5] In `MariCamiStore/Services/IOrderService.cs` remove the `ActualShippingAmountToCR` parameter from `TransitionOrderDto`; in `MariCamiStore/Services/OrderService.cs` `TransitionOrderAsync` remove the whole `else if (dto.ToStatus == OrderStatus.Delivered.Key) { ... }` block (keep AutoActiva).
- [X] T011 [US5] In `MariCamiStore/Pages/Orders/Index.cshtml` remove the `#actual-shipping-group` form-group; in `MariCamiStore/wwwroot/js/pages/orders/index.js` remove the `actual-shipping` show/hide in `openTransitionModal`, its `shippingAmountToCR` parameter (and the argument at the call site), and `payload.actualShippingAmountToCR`.
- [X] T012 [US5] Search the solution for any remaining `ActualShippingAmountToCR` / `actual-shipping` references outside `Migrations/` and remove them.
- [X] T013 [P] [US5] Append a dated section "## Descartado (2026-09-28)" to `brainstorm/09-cuentas-por-pagar.md` and a note to `specs/009-cuentas-por-pagar/spec.md` (after the Status line or as a final section) stating that the shipping rule (FR-008 a FR-010: "Shipping real a CR" al entregar y entrada AutoDelivered; y el cálculo de "Shipping CR Pendientes" solo con órdenes Activas) fue descartada y reemplazada por `specs/013-paquetes-orden` (paquetes de envío).

- [X] T009 (runs after T010–T012, needs a compiling project) From `MariCamiStore/`: `dotnet ef migrations add AddOrderPackages --output-dir Infrastructure/Persistance/Migrations`. Verify `Up` creates `OrderPackages` (FK to Orders with `ReferentialAction.Restrict`/NoAction — NOT Cascade — FK to Currencies Restrict, index), adds `CxPEntries.OrderPackageId` (FK `SetNull`, index) and drops `Orders.ActualShippingAmountToCR`; `Down` reverses. Snapshot updated. Do NOT update any database.

## Phase 4: US1 + US3 — Package service (P1/P2)

- [ ] T014 [US1] Create `MariCamiStore/Services/IOrderPackageService.cs` with records `OrderPackageDto(Guid Id, DateTime DeliveryDate, decimal Amount, string? Description, DateTime CreatedAt)`, `OrderPackagesDto(decimal EstimatedShipping, decimal TotalPackages, decimal Pending, bool CanManage, List<OrderPackageDto> Packages)` and interface methods `GetByOrderAsync(Guid orderId)`, `AddAsync(Guid orderId, DateTime deliveryDate, decimal amount, string? description)` → `(bool Success, string? Error)`, `DeleteAsync(Guid packageId)` → `(bool Success, string? Error)`.
- [ ] T015 [US1] Create `MariCamiStore/Services/OrderPackageService.cs` (primary-constructor style like `OrderService`, injecting `MariCamiStoreContext` and `ICxPService`) implementing plan D3: state check (Active/Delivering) with the exact messages in contracts/page-handlers.md, amount > 0, description trim/empty→null/max 500, open period check "No hay un período CxP abierto.", then add `OrderPackage` (CurrencyId = order.CurrencyId, Amount rounded 2, CreatedAt UtcNow) + `CxPEntry` (Type `CxPEntryType.AutoPaquete`, OrderPackageId, OrderId, Reference via private `BuildReference`, truncated to 500) and a single `SaveChangesAsync`.
- [ ] T016 [US3] In `OrderPackageService.DeleteAsync`: load package (with order), state check, open period check, add `CxPEntry` Type `ReversoPaquete`, Amount = −package.Amount, same currency/OrderId, OrderPackageId null, Reference "Reverso: " + BuildReference; remove package; single `SaveChangesAsync`.
- [ ] T017 [US1] `GetByOrderAsync`: EstimatedShipping = order.ShippingAmountToCR, TotalPackages = Σ, Pending = Math.Max(0, est − total), CanManage = status ∈ {Active, Delivering}, packages ordered by DeliveryDate then CreatedAt.
- [ ] T018 [US1] Register `services.AddScoped<IOrderPackageService, OrderPackageService>();` in `MariCamiStore/Extensions/ApplicationExtensions.cs`.

## Phase 5: US2 — Shipping CR Pendientes (P1)

- [ ] T019 [US2] In `MariCamiStore/Services/CxPService.cs` `GetPeriodIndicatorsAsync`, replace the Active-only `activeOrders` query/sum with plan D4 (Active + Delivering, per-order `Math.Max(0, ShippingAmountToCR − Σ OrderPackages.Amount)`, convert unless local currency), inside the existing non-warning branch.

## Phase 6: US4 — Items page UI (P2)

- [ ] T020 [US4] In `MariCamiStore/Pages/Orders/Items.cshtml.cs`: inject `IOrderPackageService` into `ItemsModel`; add `OnGetPackagesAsync(Guid orderId)` → `JsonResult(dto)`; `OnPostAddPackageAsync([FromBody] AddPackageRequest req)` and `OnPostDeletePackageAsync([FromBody] DeletePackageRequest req)` returning `{ success, error }`; records `AddPackageRequest(Guid OrderId, DateTime DeliveryDate, decimal Amount, string? Description)`, `DeletePackageRequest(Guid PackageId)`.
- [ ] T021 [US4] In `MariCamiStore/Pages/Orders/Items.cshtml`: between the "Artículos" card and "Historial de Estado", add `@if (!isPending)` card `card card-info` "Paquetes de Envío" with: summary row (`#pkg-estimated`, `#pkg-total`, `#pkg-pending`), an add form rendered only when status is Active/Delivering (`#pkg-date` type=date, `#pkg-amount` number step 0.01 min 0.01, `#pkg-description` maxlength `@MariCamiStore.Model.OrderPackage.DescriptionMaxLength`, `#btn-add-package`, `#pkg-error`), and `#packages-table-container`. Emit `var canManagePackages = true|false;` in the existing inline `<script>` block.
- [ ] T022 [US4] In `MariCamiStore/wwwroot/js/pages/orders/items.js`: `loadPackages()` (GET `?handler=Packages&orderId=`) rendering summary with `formatMoney(v, orderCurrencySign)` and a table (Fecha de entrega | Descripción | Monto | delete button only if `canManagePackages`), empty → "Sin paquetes registrados."; default `#pkg-date` to today; add handler (client validation amount > 0, disable button during request, POST AddPackage with the existing antiforgery pattern, show error or reset form + reload); delete handler (`confirm('¿Eliminar este paquete? Se registrará un reverso en CxP.')`, disable button during request, POST DeletePackage, reload); call `loadPackages()` on page load only if the card exists (`$('#packages-table-container').length`).

## Phase 7: CxP entries display (US1/US3)

- [ ] T023 [US1] In `MariCamiStore/wwwroot/js/pages/cxp/index.js`: add `'AutoPaquete': 'Auto-Paquete'` and `'ReversoPaquete': 'Reverso Paquete'` to `TYPE_LABELS` (keep `AutoDelivered`); render negative amounts (row and subtotal `group.total`, plus any other `formatMoney` use on possibly negative CxP amounts) as `<span class="text-danger">−` + `formatMoney(Math.abs(x), sign)` + `</span>` (FR-010). Delete buttons unchanged (FR-009b).

## Phase 8: Polish

- [ ] T024 `dotnet build MariCamiStore/MariCamiStore.csproj`: 0 errors, no new warnings.
- [ ] T025 Verify by code reading against quickstart.md §1–10; confirm no `ActualShippingAmountToCR` remains outside `Migrations/`, every new element ID used in `items.js` exists once in `Items.cshtml`, and handlers enforce state rules server-side.

---

## Dependencies

T001 → T002–T008 → (T010–T012) → T009 (migration needs a compiling model) → T014–T018 → T019, T020 → T021 → T022; T023 after T002; T013 independent. Polish last.

## Parallel

T013 (docs) can run anytime; T023 (cxp JS) parallel to T020–T022 (different files).
