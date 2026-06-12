# Review Guide: Order Items — Mejoras de UI y Modelo

**Generated**: 2026-06-06 | **Spec**: [spec.md](spec.md)

## Why This Change

La pantalla de ítems de orden muestra actualmente una lista plana sin agrupación, lo que obliga al operador a sumar manualmente los montos acordados por cliente. El campo `RealPrice` requiere entrada manual aunque en la mayoría de los casos debería igualar a `ListPrice`. No existe forma de confirmar la recepción física de cada ítem durante la entrega. Adicionalmente, el campo `ProductLink` está visualmente demasiado angosto para URLs de plataformas como Shein o Amazon.

## What Changes

La pantalla de ítems de orden gana agrupación por cliente con subtotales de `AgreedPriceInLocal` y un total general, ordenamiento predeterminado por nombre de cliente → fecha (más reciente primero), un contador de ítems visible como badge, sincronización automática `RealPrice` ← `ListPrice` durante la captura (con desactivación manual), un checkbox de "Recibido" por ítem activo solo en estados `Delivering`/`Delivered` (fila verde al marcar), y un campo `ProductLink` más ancho. El widget jsGrid es reemplazado por una tabla HTML generada con JavaScript. Se agrega el campo `IsReceived bool` (default `false`) a `OrderItem` mediante una migración de base de datos — **cambio de esquema bloqueante** que debe aplicarse antes de cualquier otra fase.

## How It Works

La implementación se divide en cinco fases:

**Fase A (Migración)**: Agregar `IsReceived` a `OrderItems` con `ADD COLUMN BIT NOT NULL DEFAULT 0`. Sin pérdida de datos; registros existentes quedan en `false`.

**Fase B (Backend)**: Nuevo método `GetOrderItemsWithCustomerAsync` en `OrderService` que hace JOIN con `Customers` vía EF, calcula `CustomerDisplayName = NickName ?? Name ?? Id`, ordena `.OrderBy(name).ThenByDescending(createdAt)`, y retorna un nuevo DTO `OrderItemWithCustomerDto`. Nuevo método `ToggleIsReceivedAsync` que valida que la orden esté en `Delivering` o `Delivered` antes de persistir el toggle. `OnGetLoadAsync` en `Items.cshtml.cs` se actualiza para usar el nuevo método; se agrega el handler `OnPostToggleReceivedAsync`.

**Fase C (Frontend — tabla custom)**: El bloque `$('#jsGrid').jsGrid({...})` se reemplaza por `renderItemsTable(items)` — función que agrupa ítems por `customerDisplayName`, genera filas de encabezado de grupo (`table-dark`), filas de ítem, filas de subtotal (`table-light font-weight-bold`) y una fila de total general (`table-info`). El badge `#item-count-badge` se actualiza con `items.length` en cada render. La columna `ProductLink` aparece como anchor clickeable con `min-width:250px`.

**Fase D (Reactive Pricing)**: Flag `userEditedRealPrice` a nivel de archivo, siguiendo el patrón existente `userEditedAgreed`. `openAddItem()` lo resetea a `false`; `openEditItem(item)` lo inicializa en `true` si `RealPrice ≠ ListPrice` al abrir; el listener de `#item-list-price` omite la sincronización si la flag es `true`; un nuevo listener en `#item-real-price` activa la flag al primer input manual.

**Fase E (Modal URL)**: Campo `item-product-link` movido a fila propia `col-md-12` en el modal, sacándolo del layout compartido con `ProductSourceCode`.

## When It Applies

**Applies when**:
- El usuario está en la pantalla `Orders/Items` de una orden existente
- La orden está en estado `Delivering` o `Delivered` (para que los checkboxes de recepción sean visibles)
- El modal de nuevo ítem o edición está abierto (para reactive pricing)

**Does not apply when**:
- Órdenes en estados `Pending`, `Active`, `Completed` o `Voided` — los checkboxes de recepción no se muestran (el campo `IsReceived` existe en DB pero es solo lectura desde JS)
- Cualquier otra pantalla del sistema — el reemplazo de jsGrid afecta únicamente `Items.cshtml`
- La pantalla de Órdenes (`Orders/Index`) — sin cambios

## Key Decisions

1. **Reemplazar jsGrid con tabla HTML custom**: jsGrid no soporta filas de encabezado de grupo ni filas de subtotal. Las alternativas evaluadas (post-procesamiento del DOM de jsGrid, DataTables con plugin de agrupación) fueron rechazadas por fragilidad o por introducir dependencias nuevas no usadas en el resto del proyecto.

2. **Backend retorna `CustomerDisplayName` pre-ordenado**: El frontend ya carga los clientes para el dropdown, pero de forma asíncrona. Incluir `CustomerDisplayName` directamente en la respuesta de `?handler=Load` elimina el riesgo de race condition entre la carga de ítems y la carga de clientes.

3. **Endpoint `POST ?handler=ToggleReceived` dedicado**: Reutilizar `?handler=Update` requeriría enviar todos los campos del ítem (incluyendo lógica de imagen) para cambiar un boolean. El endpoint dedicado es más ligero y semánticamente correcto.

4. **Flag `userEditedRealPrice` (mirrors `userEditedAgreed`)**: El archivo `items.js` ya usa este patrón para `AgreedPriceInLocal`. Seguir el mismo patrón minimiza la cantidad de código nuevo y mantiene coherencia interna.

5. **Badge en card header para el contador**: Patrón estándar de AdminLTE, sin dependencias adicionales.

## Areas Needing Attention

- **Tamaño de `renderItemsTable`**: La función tiene ~80 líneas de JS inline en `items.js`. Es funcionalmente correcta pero monolítica. Si en el futuro se agregan columnas, el mantenimiento puede volverse complejo. Para este PR es aceptable dado el alcance.

- **`isDelivering` calculado al cargar el módulo**: La variable se evalúa una vez con el valor de `orderStatus` (inyectado por Razor al renderizar). Si el estado de la orden cambia sin reload de página, la visibilidad del checkbox no se actualiza. Es aceptable para este caso de uso (los cambios de estado requieren navegación).

- **Subtotal solo sobre `AgreedPriceInLocal`**: FR-002 especifica subtotales de `AgreedPriceInLocal`. Si el revisor espera subtotales de otros campos (como `RealPrice` o `ServiceFeeInLocal`), no los verá. El spec es explícito al respecto.

- **`refreshTotals()` llamado dentro de `renderItemsTable`**: Es un side effect al final de la función. El revisor debe verificar que esto no cause doble cálculo si `loadItems()` también lo llama externamente.

- **jsGrid scripts/links en Items.cshtml**: Las tareas T013/T016 reemplazan el `<div>` y el bloque JS, pero no mencionan explícitamente remover `<link>` o `<script>` de jsGrid si están cargados per-página (no en el layout global). Verificar durante implementación si hay referencias a jsGrid en `Items.cshtml` que deban eliminarse.

## Open Questions

~~**jsGrid en el layout global vs per-página**~~: **Resuelto** — jsGrid está en `_Layout.cshtml` (global) y lo usan 9 páginas más. En `Items.cshtml` solo existe el `<div id="jsGrid"></div>` (sin script/link propios). T013 cubre exactamente eso; no se necesita ninguna acción adicional.

~~**CustomerDisplayName — NickName null vs string vacío**~~: **Resuelto** — `Customer.NickName` es `string` con default `string.Empty` (nunca null). El frontend de Customers ya tiene `validate: 'required'` en el campo. Se agrega validación backend en T029. La implementación en T007 puede usar `NickName` directamente sin fallback a null, aunque se mantiene la guarda `!string.IsNullOrEmpty` como defensa en caso de datos históricos.

## Review Checklist

- [ ] La migración `AddIsReceivedToOrderItem` aplica limpiamente sin pérdida de datos
- [ ] `GetOrderItemsWithCustomerAsync` maneja el caso `NickName == null` correctamente con `!string.IsNullOrEmpty`
- [ ] `ToggleIsReceivedAsync` valida el estado de la orden antes de persistir (no permite toggle en `Completed`/`Voided`)
- [ ] `renderItemsTable` produce subtotales correctos con sumas de `AgreedPriceInLocal` por grupo
- [ ] Checkbox solo visible cuando `orderStatus === 'Delivering' || === 'Delivered'`
- [ ] `userEditedRealPrice` se resetea en `openAddItem()` y se inicializa correctamente en `openEditItem()`
- [ ] `refreshTotals()` no se llama dos veces por render
- [ ] ProductLink col-md-12 en modal no rompe el layout de otros campos
- [ ] Key decisions are justified
- [ ] Scope matches the stated boundaries (solo `Items.cshtml` — sin páginas nuevas)
- [ ] No unstated assumptions

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
