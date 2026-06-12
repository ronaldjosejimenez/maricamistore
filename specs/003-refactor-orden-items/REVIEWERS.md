# Review Guide: Refactor Orden e Ítems

**Generated**: 2026-06-04 | **Spec**: [spec.md](spec.md)

## Why This Change

El sistema actual de gestión de ítems de orden usa edición inline en jsGrid, lo que resulta inutilizable dado el número de campos requeridos. Además, editar una orden abre un diálogo separado que interrumpe el flujo de trabajo. Faltan campos importantes de trazabilidad del producto (`ProductLink`, `ProductSourceCode`, `ProductImage`), la columna `ListPriceTax` tiene un nombre incorrecto, y los totales de la orden no incluyen `TotalAgreedPriceInLocal`. La lógica de cálculo tampoco está completamente formalizada: cambios en `TaxPercentage` o `ExchangeRate` no actualizan automáticamente los campos de los ítems ni los totales de la orden.

## What Changes

Se reemplaza la edición inline del jsGrid de ítems por un modal Bootstrap dedicado con todos los campos, incluyendo los tres nuevos (`ProductLink`, `ProductSourceCode`, `ProductImage` con validación de 2 MB). La pantalla de detalle de la orden pasa a modo de edición in-place: los 4 campos editables (`ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`) se muestran como inputs directamente en la pantalla, con un aviso de "cambios pendientes" cuando hay datos sin guardar. Los cálculos JS se actualizan en tiempo real en el modal y los totales se recalculan automáticamente al guardar ítems o al cambiar parámetros de la orden. Se aplica una migración de DB que renombra `ListPriceTax → ListPriceTaxWithTax`, elimina `RequestedAt`, y agrega `TotalAgreedPriceInLocal`. El formulario de creación de nueva orden muestra solo 4 campos (de los 6 actuales). No hay cambios de navegación ni rutas nuevas.

## How It Works

**DB Migration (prerequisito bloqueante)**: Una migración EF Core (`RefactorOrderItems`) rename la columna `ListPriceTax` → `ListPriceTaxWithTax`, drops `RequestedAt`, y agrega `TotalAgreedPriceInLocal` (decimal, default 0) a la tabla Orders.

**Services**: `OrderTotalsDto` agrega `TotalAgreedPriceInLocal`. `ICatalogService` agrega `GetProductTypesByCurrencyAsync(Guid currencyId)` para el filtro de ProductTypes en el modal.

**Items page handlers** (`Items.cshtml.cs`): Se agregan 3 nuevos handlers — `OnGetProductTypesByCurrencyAsync` (filtro), `OnGetItemImageAsync` (sirve imagen como FileContentResult a demanda), `OnPostUpdateOrderAsync` (edición in-place del header). Los handlers `OnPostInsertAsync`/`UpdateAsync` se actualizan para aceptar imagen como Base64 en el JSON payload (se valida que la decodificación no supere 2 MB antes de persistir; si la imagen es null en un Update, se preserva la existente).

**Orders/Index page**: `OnGetLoadAsync` agrega `currencyId` e `itemCount` a la respuesta. `OnGetConfigurationAsync` agrega `currencyId` (default de org). Se agrega `#order-currency` dropdown al modal de orden, deshabilitado cuando `itemCount > 0 && status != Pending`.

**HTML** (`Items.cshtml`): jsGrid pasa a display/delete only (`inserting: false, editing: false`). Se agregan botones "Agregar Ítem" y "Editar" que abren `#itemModal` Bootstrap scrollable. Los 4 campos editables del header de la orden se convierten en `<input>` con Razor condition `@if (isPending)`. Se agrega `#imagePreviewModal` para previsualización a demanda.

**JS** (`items.js`): `calcItem()` usa `listPriceTaxWithTax`; `recalcOrderHeader()` agrega `TotalAgreedPriceInLocal`; `persistTotals()` lo incluye en el POST. El modal usa flag `userEditedAgreed` para respetar overrides manuales de `AgreedPriceInLocal` (excepto cuando cambia ExchangeRate, que fuerza recálculo para todos los ítems). Las imágenes se codifican en Base64 client-side antes de enviarse.

## When It Applies

**Applies when**:
- El usuario está en la pantalla de detalle de una orden (`/Orders/Items?orderId=...`)
- El usuario agrega o edita ítems de una orden Pending
- El usuario guarda una orden Pending con cambios en `ExchangeRate` o `TaxPercentage`
- El usuario crea o edita una orden desde `/Orders`

**Does not apply when**:
- La orden está en estado distinto de Pending (all fields read-only, modal abre en modo visualización)
- El módulo de Customers, Suppliers, o ProductTypes — sin cambios
- Módulos de Configurations, Payments, Reports — sin cambios
- Tests automatizados — explícitamente fuera de scope

## Key Decisions

1. **Base64 para imagen en JSON** (en lugar de multipart form): Los handlers existentes usan `[FromBody]` JSON. Base64 mantiene el patrón consistente sin introducir un parser multipart. 2 MB de imagen → ~2.67 MB de payload, dentro del límite default de 30 MB de ASP.NET Core. Un endpoint separado habría requerido UX de dos pasos.

2. **jsGrid para display + modal para inserción/edición**: Mantiene jsGrid para paginación y visualización (ya invertido en el codebase). El modal es consistente con el modal de "Nueva Orden" ya existente. Eliminar jsGrid completamente habría requerido reescribir también la vista de Query.

3. **`hasImage` en lugar de binario en `OnGetLoadAsync`**: Cargar `byte[]` completo en la respuesta del grid load (N ítems × hasta 2 MB) sería inaceptable en red. La imagen se sirve a demanda vía `OnGetItemImageAsync`.

4. **Flag client-side `userEditedAgreed`** para AgreedPriceInLocal: El spec requiere que el override manual no sea sobreescrito por recálculos. Un flag JS (reseteado al abrir el modal para nuevo ítem) es la solución más simple sin cambios en el modelo de datos.

5. **`CurrencyId` editable en modal de Orders/Index**: La edición de moneda se centraliza en el modal de orden (no en Items.cshtml). Esto simplifica el flujo ya que la moneda define qué ProductTypes son válidos; cambiarla con ítems ya agregados requiere condiciones de protección (`itemCount > 0 && status != Pending` → read-only).

## Areas Needing Attention

- **Imagen y tipo MIME**: `OnGetItemImageAsync` detecta el tipo de imagen desde los primeros bytes (magic numbers). Si la imagen fue guardada en un formato distinto o sin cabecera estándar, podría servirse con tipo MIME incorrecto. Revisar si se necesita guardar el content-type junto con el binario.

- **Trigger A — ExchangeRate change**: Al guardar la orden con cambio de `ExchangeRate`, el JS recalcula `AgreedPriceInLocal` para TODOS los ítems (ignora el flag `userEditedAgreed`). Esto es el comportamiento esperado según el spec, pero es un override silencioso de datos que el usuario editó manualmente. Confirmar que el equipo está de acuerdo.

- **`ItemCount` en `OnGetLoadAsync`**: Agregar el conteo de ítems requiere un JOIN o `.Count()` subquery para cada orden. Con volúmenes pequeños es irrelevante, pero es una modificación al query de la grilla de órdenes que podría afectar rendimiento. Revisar si se puede hacer eficientemente con EF Core.

- **`select2` para búsqueda de Customer**: El task T029 usa el patrón de búsqueda en el select de Customer asumiendo que `select2` esté disponible en `_Layout.cshtml`. Si no está, se cae a un filtro JS simple sobre `<select>`. Confirmar durante implementación.

- **`OrderItemDto` vs `OrderItem` model**: Los handlers Insert/Update pasarán de recibir el modelo EF directamente a recibir un DTO. Esto es una mejora arquitectural pero cambia la firma de los handlers — verificar si hay algún consumer externo que llame estos endpoints directamente.

## Open Questions

No hay preguntas abiertas sin respuesta. Todos los items del brainstorm fueron resueltos en el revisit del 2026-06-04.

## Review Checklist

- [ ] La migración usa `RenameColumn` (no Drop+Add) para preservar datos existentes en `ListPriceTax`
- [ ] La validación de imagen (2 MB) ocurre tanto en frontend (antes de Base64) como en backend (después de decodificar)
- [ ] `AgreedPriceInLocal` manual override se respeta excepto en Trigger A (ExchangeRate change)
- [ ] `OnGetLoadAsync` de Items NO incluye el binario `ProductImage` en la respuesta (solo `hasImage`)
- [ ] `OnGetItemImageAsync` retorna 404 si el ítem no existe o no tiene imagen
- [ ] El modal de ítem muestra "No hay tipos disponibles para esta moneda" cuando la lista de ProductTypes está vacía
- [ ] La moneda de la orden es read-only cuando `itemCount > 0 && status != Pending`
- [ ] Key decisions are justified
- [ ] Scope matches the stated boundaries
- [ ] Success criteria are achievable
- [ ] No unstated assumptions

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
