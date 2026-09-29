# Quickstart / Manual Verification: 015-filtro-organizacion-pagos

```bash
cd MariCamiStore && dotnet build && dotnet run
```
Needs at least two organizations and, ideally, an existing customer with transactions in more than one organization (or register test payments as part of the walkthrough).

1. **Combo**: open Payments → Organization combo appears above "Registrar Pago"; "Todas" first, then all organizations; the session's current organization is preselected.
2. **"Todas" blocks registration**: with "Todas" selected, "Registrar Pago" is disabled and the legend "Seleccione una organización específica para registrar el pago." is visible. Pick a customer → balance card still shows normally.
3. **Pick a specific org**: button enables, legend disappears. "Saldos de Clientes" reloads to that organization's transactions only.
4. **Register a payment** with a non-session organization selected → the new transaction/balance reflects that organization, not the session's.
5. **Switch back to "Todas"** → "Saldos de Clientes" returns to the global view; button disables again.
6. **Balance card follows the filter**: with a customer already selected, change the organization filter → "Saldo Esta Org." updates automatically to the new organization (or to Global when switching to "Todas"); "Saldo Global" never changes.
7. **Rapid filter changes**: click through several organizations quickly → the saldos table ends up matching the last one selected (no flicker back to a stale one).
8. **Server-side guard**: with dev tools, POST `RegisterPayment` with `organizationId: null` → rejected with the same message, no transaction created.
9. **Session organization unaffected**: after using the filter, navigate to Orders/CxP → the app's active organization is still the original session one (unchanged by the Payments filter).
