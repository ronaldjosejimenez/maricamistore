# Tasks: Pantalla de Transacciones

**Feature**: 007-transactions-screen | **Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

## Phase 1 — Servicio Backend (US1 + US2)

- [X] T001 [US1] Crear `MariCamiStore/Services/ITransactionService.cs` con records `TransactionDto`, `TransactionFilterDto`, `ManualTransactionRequest` e interfaz `ITransactionService`
- [X] T002 [US1] Crear `MariCamiStore/Services/TransactionService.cs` implementando `GetTransactionsAsync` (left join Transactions→Customers→OrderItems→Orders con filtros server-side) y `CreateManualTransactionAsync` (Source="Manual", SourceId=null, Status=Applied, descripción auto-generada)
- [X] T003 [P] [US1] Registrar `services.AddScoped<ITransactionService, TransactionService>()` en `MariCamiStore/Extensions/ApplicationExtensions.cs`

## Phase 2 — Página Razor (US1 + US2)

- [X] T004 [US1] Crear `MariCamiStore/Pages/Transactions/Index.cshtml` con: sección de filtros (Desde, Hasta, Cliente dropdown, Tipo dropdown, botón Filtrar), contenedor `#transactions-table-container`, botón "Nueva Transacción", y modal `#modal-new-tx` con campos Cliente, Tipo, Monto, Descripción (opcional) y botones Cancelar/Guardar
- [X] T005 [US1] Crear `MariCamiStore/Pages/Transactions/Index.cshtml.cs` con `IndexModel` extendiendo `OrganizationPageModel`, handlers `OnGet`, `OnGetLoadAsync([FromQuery] params)` y `OnPostCreateManualAsync([FromBody] ManualTransactionRequest)`

## Phase 3 — JavaScript (US1 + US2)

- [X] T006 [US1] Crear `MariCamiStore/wwwroot/js/pages/transactions/index.js` con: función `loadTransactions()` que lee filtros del DOM y llama `GET ?handler=Load`, función `renderTransactions(data)` que construye tabla HTML con columnas Orden/Cliente/Tipo/Descripción/Monto/Fecha y fila Total (solo si hay tipo seleccionado), listener del botón Filtrar, lógica del modal (abrir, validar, POST ?handler=CreateManual, cerrar y recargar), y llamada inicial `loadTransactions()` al cargar página

## Phase 4 — Navegación (US3)

- [X] T007 [P] [US3] Agregar ítem "Transacciones" en el menú Finanzas de `MariCamiStore/Pages/Shared/_Layout.cshtml` (después del ítem "Registro de Pagos", antes del cierre `</ul>`)

## Dependencies

- T002 depende de T001 (usa los tipos definidos en ITransactionService)
- T003 depende de T002 (debe existir TransactionService para registrar)
- T005 depende de T001 (usa DTOs)
- T006 depende de T004 y T005 (el DOM y los handlers deben existir)
- T007 es independiente del resto

## Implementation Strategy

MVP (P1): T001 → T002 → T003 → T004 → T005 → T006 → T007
P2/P3: incluido en T005 (CreateManual handler) y T006 (modal JS)
