# Research: Mejoras Fase 1

## Decision 1: Carga de Organización por Defecto desde Appsettings

**Decision**: Agregar `DefaultOrganizationId` a `BaseSettings` y modificar `CurrentOrganizationService` para que, cuando la sesión no tenga una organización activa, consulte la DB con ese ID y lo escriba a sesión automáticamente en el primer request.

**Rationale**: Es la opción más limpia: un solo punto de configuración (`appsettings.json`), sin middleware adicional. El `CurrentOrganizationService` ya existe y ya es inyectado en todas las páginas que necesitan organización activa. Hacer el fallback dentro del servicio evita duplicar lógica.

**Alternatives considered**:
- Startup hook (IHostedService): carga la org una sola vez al arrancar, pero no tiene acceso a `ISession` en ese contexto (requiere IHttpContextAccessor con sesión activa).
- Middleware: funciona pero es sobre-ingeniería para un caso simple de single-user/small-business.

**Impact**: `BaseSettings.cs` (agregar propiedad), `CurrentOrganizationService.cs` (agregar fallback async), `ICurrentOrganizationService` (agregar método async si se requiere). Dado que la sesión no está disponible fuera de un request, el load debe ocurrir en el primer GET de cualquier página.

---

## Decision 2: Validación de Organización en Pantallas (Guard)

**Decision**: Crear una clase base abstracta `OrganizationPageModel : PageModel` con un método `OnGet()` que valide la organización activa y redirija si no existe. Las páginas que requieren organización heredarán de esta clase en lugar de `PageModel` directamente.

**Rationale**: Es el patrón idiomático en Razor Pages para comportamiento compartido entre páginas. Evita duplicar el guard en cada página. Más explícito que un middleware global (que afectaría a todas las rutas, incluidas las de catálogos globales que no requieren org).

**Alternatives considered**:
- Filter/Attribute: funciona, pero requiere más infraestructura (registro de filtros, atributos personalizados).
- Middleware global: demasiado amplio; algunas páginas (Currencies, Organizations) son globales.
- Repetir el check en cada página: ya se hace en Orders; es inconsistente y propenso a omisiones.

**Páginas que requieren org guard**: Configurations, Orders/Index, Orders/Items, Payments, Reports/Saldos.
**Páginas que NO requieren org guard**: Currencies, ProductTypes, Suppliers, Customers, Organizations (son catálogos globales o la selección de org misma).

---

## Decision 3: Corrección de Visualización de Moneda

**Decision**: Para `configurations/index.js` y `product-types/index.js`, reemplazar las columnas que muestran el GUID de moneda con un campo de tipo `select` alimentado desde `/Currencies?handler=Load`. La carga de monedas se hace una sola vez al inicializar la página.

**Rationale**: jsGrid soporta campos tipo `select` con un array de `{ id, text }`. La lista de monedas es pequeña (< 10 registros) y estable, por lo que cargarla en la inicialización es eficiente. No requiere cambios en el backend.

**Alternatives considered**:
- Agregar campo `currencyAbbreviation` al DTO del backend: requiere cambios en múltiples capas para lo que es solo una mejora de UI.
- Campo de solo lectura en jsGrid que muestre el nombre: menos usable para inserción/edición.

---

## Decision 4: CRUD de Organizaciones con Servicio Dedicado

**Decision**: Crear `IOrganizationService` / `OrganizationService` en la capa de Services para encapsular toda la lógica CRUD de organizaciones (incluyendo la validación de integridad referencial). `Organizations/Index.cshtml.cs` se refactoriza para inyectar el servicio en lugar de acceder al `DbContext` directamente.

**Rationale**: El `Organizations/Index.cshtml.cs` actual accede directamente al `DbContext` (viola FR-001). Necesita un servicio. El check de integridad referencial (órdenes, configuraciones) pertenece a la capa de servicio.

**Tables to check before delete**: `Orders` (where `OrganizationId` references org), `Configurations` (where `OrganizationId` references org).

---

## Decision 5: Configuración Única por Organización en UI

**Decision**: En la vista `configurations/index.js`, deshabilitar el modo `inserting` de jsGrid cuando ya existe una configuración cargada. El servicio backend ya tiene el patrón upsert; el cambio es principalmente en el frontend para comunicar claramente al usuario cuándo puede insertar vs. editar.

**Rationale**: `CatalogService.UpsertConfigurationAsync` ya hace upsert (insert si no existe, update si existe). El problema es que la UI siempre muestra la fila de inserción, lo que confunde al usuario. Deshabilitar `inserting` cuando ya hay un registro y mostrar un aviso es suficiente.

**Alternatives considered**:
- Agregar validación extra en el servicio para rechazar inserts duplicados: el upsert actual ya evita duplicados de facto, pero el UX no comunica esto.

---

## Decision 6: Mejoras en Formulario de Orden

**Decision**:
1. **Campo Proveedor primero**: Mover el `<select>` de Proveedor al inicio del formulario del modal de orden (solo cambio de orden en HTML).
2. **Auto-sugerencia de nombre**: Evento `change` en el select de Proveedor → JS construye `{SupplierName}-{DD}-{MM}-{YYYY}` y lo pone en el campo nombre si está vacío (o siempre en modo creación).
3. **Auto-carga de tipo de cambio e impuesto**: Agregar handler `OnGetConfigurationAsync` en `Orders/Index.cshtml.cs` que devuelva `{ exchangeRate, taxPercentage }`. El JS llama este endpoint en `openNewOrder()` y pre-llena los campos.
4. **Los ítems ya tienen restricción de estado**: `CanEdit` ya se retorna desde el backend; el JS ya la respeta para Editar/Eliminar. Solo revisar que el botón "Agregar ítem" también esté controlado en la vista `items.cshtml`.

**Rationale**: Los cambios son mínimos y localizados. No requieren nuevas entidades ni cambios de esquema.

---

## Finding: Estado Actual vs. Requerido

| Área | Estado Actual | Qué Falta |
|------|---------------|-----------|
| Org por defecto | Solo session | Fallback a `appsettings.json` |
| Guard de org | Solo en Orders | Falta en Configurations, Payments, Saldos |
| Moneda en Config | Muestra GUID | Cambiar a select con abreviación |
| Moneda en ProductTypes | Muestra GUID | Cambiar a select con abreviación |
| CRUD Organizations | Solo Load + SetActive | Agregar Insert/Update/Delete + service |
| Una config por org | UX permite insertar | Deshabilitar insert si ya existe config |
| Formulario orden - Proveedor primero | No — está mezclado | Mover al tope del form HTML |
| Formulario orden - Nombre sugerido | No | Agregar evento JS en cambio de proveedor |
| Formulario orden - Auto-carga config | No | Agregar handler + JS |
| Botón "Agregar" en Items (estado) | Parcialmente | Verificar que se bloquea cuando no Pending |
