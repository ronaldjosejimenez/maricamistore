# Implementation Plan: Order Items — Mejoras de UI y Modelo

**Branch**: `master` | **Date**: 2026-06-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/004-order-items-mejoras/spec.md`

---

## Summary

Mejoras incrementales sobre la pantalla `Orders/Items.cshtml`: agrupación del grid por cliente con subtotales, ordenamiento por nombre de cliente → fecha, contador de ítems, reactive pricing (`RealPrice` sigue a `ListPrice`), checklist de recepción con nuevo campo `IsReceived` en `OrderItem`, y campo URL más ancho. Un cambio de esquema (migración DB) y modificaciones en C# y JavaScript sin cambios de ruta ni páginas nuevas.

---

## Technical Context

**Language/Version**: C# / .NET 10 + ASP.NET Core Razor Pages

**Primary Dependencies**: Entity Framework Core, tabla HTML custom (reemplaza jsGrid para esta página), Select2, AdminLTE, jQuery

**Storage**: SQL Server (EF Core migrations)

**Testing**: Manual (sin pruebas automatizadas según política del proyecto)

**Target Platform**: Web server (Linux/Windows, deploy existente)

**Project Type**: Web application — Razor Pages + AJAX + JS

**Performance Goals**: Grid renderiza y agrupa en ≤1 segundo para 200 ítems

**Constraints**: Sin cambios de ruta. Sin nuevas páginas. Sin breaking changes en handlers existentes.

**Scale/Scope**: Pantalla de ítems de una orden (normalmente <100 ítems por orden)

---

## Constitution Check

La constitución del proyecto es un template vacío — no hay principios formales definidos. No hay violaciones que evaluar.

---

## Project Structure

### Documentation (this feature)

```text
specs/004-order-items-mejoras/
├── plan.md                    ← este archivo
├── research.md                ← decisiones de diseño (D1–D7)
├── data-model.md              ← cambio de modelo (IsReceived)
├── contracts/
│   └── items-handlers.md      ← Load (updated) + ToggleReceived (new)
├── checklists/
│   └── requirements.md
├── spec.md
└── tasks.md                   ← generado por /speckit-tasks
```

### Source Code (affected files)

```text
MariCamiStore/
├── Model/
│   └── OrderItem.cs
│       └── [M] Add: IsReceived bool (default false)
├── Infrastructure/Persistance/
│   ├── EntityConfigurations/
│   │   └── OrderItemEntityTypeConfiguration.cs
│   │       └── [M] Configure IsReceived property
│   └── Migrations/
│       └── {timestamp}_AddIsReceivedToOrderItem.cs   [NEW]
├── Services/
│   ├── IOrderService.cs
│   │   └── [M] Add: ToggleIsReceivedAsync signature
│   │   └── [M] Add: GetOrderItemsWithCustomerAsync signature
│   └── OrderService.cs
│       └── [M] Implement: ToggleIsReceivedAsync
│       └── [M] Implement: GetOrderItemsWithCustomerAsync (JOIN w/ Customers, sorted)
├── Pages/Orders/
│   ├── Items.cshtml.cs
│   │   └── [M] OnGetLoadAsync → call GetOrderItemsWithCustomerAsync
│   │   └── [M] Add: OnPostToggleReceivedAsync handler
│   │   └── [M] Add: ToggleReceivedRequest record
│   └── Items.cshtml
│       └── [M] Remove: <div id="jsGrid">
│       └── [M] Add: <div id="items-table-container">
│       └── [M] Add: item-count-badge in card header
│       └── [M] Add: var orderStatus JS variable
│       └── [M] ProductLink field → col-md-12 (full row width)
└── wwwroot/js/pages/orders/
    └── items.js
        └── [M] Replace jsGrid init with loadItems() + custom table render
        └── [M] Add: grouping by customerDisplayName + subtotals
        └── [M] Add: IsReceived checkbox column (conditional on orderStatus)
        └── [M] Add: ProductLink column (clickable link)
        └── [M] Add: item counter badge update
        └── [M] Fix: reactive pricing (userEditedRealPrice flag)
        └── [M] Add: ToggleReceived AJAX call + row color update
```

---

## Implementation Phases

### Phase A — Database Migration (P0, prerequisito)

**Goal**: Agregar `IsReceived` a `OrderItems` sin pérdida de datos.

**Steps**:
1. En `OrderItem.cs`: agregar `public bool IsReceived { get; set; }`.
2. En `OrderItemEntityTypeConfiguration.cs`: agregar `builder.Property(oi => oi.IsReceived).IsRequired().HasDefaultValue(false);`.
3. Generar migración: `dotnet ef migrations add AddIsReceivedToOrderItem --project MariCamiStore`.
4. Aplicar migración: `dotnet ef database update --project MariCamiStore`.

**Verify**: Todos los registros existentes tienen `IsReceived = false`. Nuevo ítem guarda con `IsReceived = false` por defecto.

---

### Phase B — Backend: GetOrderItemsWithCustomer + ToggleReceived

**Goal**: Endpoint Load devuelve `customerDisplayName` e `isReceived` pre-ordenados. Nuevo endpoint ToggleReceived funciona.

**Steps**:
1. **Nuevo DTO/record** en `Items.cshtml.cs` o en un archivo separado:
   ```csharp
   public record OrderItemWithCustomerDto(
       Guid Id, Guid OrderId, Guid CustomerId,
       string CustomerDisplayName,
       string ProductDescription, string? ProductLink, string? ProductSourceCode,
       bool HasImage, Guid ProductTypeId,
       decimal ListPrice, decimal ListPriceTaxWithTax,
       decimal RealPrice, decimal EstimateShipping,
       decimal ServiceFeeInLocal, decimal AgreedPriceInLocal,
       bool IsReceived, DateTime CreatedAt, DateTime UpdatedAt);
   ```

2. **`IOrderService`**: agregar:
   ```csharp
   Task<List<OrderItemWithCustomerDto>> GetOrderItemsWithCustomerAsync(Guid orderId);
   Task<(bool Success, string? Error)> ToggleIsReceivedAsync(Guid itemId, bool isReceived);
   ```

3. **`OrderService.GetOrderItemsWithCustomerAsync`**:
   - JOIN `OrderItems` con `Customers` vía EF Include o proyección.
   - Ordenar: `.OrderBy(x => x.CustomerDisplayName).ThenByDescending(x => x.CreatedAt)`.
   - Retornar lista de `OrderItemWithCustomerDto`.

4. **`OrderService.ToggleIsReceivedAsync`**:
   - Cargar ítem y su orden.
   - Validar que orden esté en `Delivering` o `Delivered`.
   - Actualizar `IsReceived` y `UpdatedAt`.
   - Guardar.

5. **`Items.cshtml.cs`**:
   - `OnGetLoadAsync`: usar `GetOrderItemsWithCustomerAsync`, proyectar respuesta incluyendo `CustomerDisplayName` e `IsReceived`.
   - Agregar handler:
     ```csharp
     public async Task<JsonResult> OnPostToggleReceivedAsync([FromBody] ToggleReceivedRequest request)
     {
         var (success, error) = await orderService.ToggleIsReceivedAsync(request.ItemId, request.IsReceived);
         return new JsonResult(new { success, error });
     }
     public record ToggleReceivedRequest(Guid ItemId, bool IsReceived);
     ```

**Verify**: `GET ?handler=Load` retorna `customerDisplayName` e `isReceived` en cada ítem. Items ordenados. POST ToggleReceived actualiza `IsReceived` en DB.

---

### Phase C — Frontend: Tabla Custom + Grouping + Counter (P1)

**Goal**: Reemplazar jsGrid con tabla agrupada por cliente. Contador visible. URL como link.

**`Items.cshtml` changes**:
1. Reemplazar `<div id="jsGrid"></div>` por:
   ```html
   <div id="items-table-container">
       <div class="text-center text-muted py-3">Cargando...</div>
   </div>
   ```
2. Al título "Artículos" agregar: `<span id="item-count-badge" class="badge badge-primary ml-2">0</span>`.
3. En el bloque `<script>` inicial, agregar: `var orderStatus = '@order.Status';`.
4. Mover `item-product-link` a fila propia (col-md-12), dejar `item-product-source-code` en fila siguiente.

**`items.js` changes** — reemplazar el bloque `$('#jsGrid').jsGrid({...})` con:

```javascript
// ── Custom Table Rendering ────────────────────────────────────────────────────
var isDelivering = orderStatus === 'Delivering' || orderStatus === 'Delivered';
var COLS = 11 + (isDelivering ? 1 : 0); // número de columnas totales

function renderItemsTable(items) {
    $('#item-count-badge').text(items.length);

    var $container = $('#items-table-container');
    if (!items || items.length === 0) {
        $container.html('<div class="alert alert-info">No hay artículos en esta orden.</div>');
        return;
    }

    // Group items by customerDisplayName
    var groups = {};
    var groupOrder = [];
    items.forEach(function(item) {
        var key = item.customerDisplayName || item.customerId;
        if (!groups[key]) { groups[key] = []; groupOrder.push(key); }
        groups[key].push(item);
    });

    var $table = $('<table class="table table-sm table-bordered table-hover">');
    // Header row
    var headerCols = ['Cliente','Descripción','Cód.','Link','P.Lista','P+Imp','Servicio','Total','P.Acordado','P.Real','Envío Est.'];
    if (isDelivering) headerCols.push('Recibido');
    headerCols.push('Acciones');
    var $thead = $('<thead class="thead-light">');
    var $htr = $('<tr>');
    headerCols.forEach(function(h) { $htr.append($('<th>').text(h)); });
    $thead.append($htr); $table.append($thead);

    var $tbody = $('<tbody>');
    var grandTotal = 0;

    groupOrder.forEach(function(groupName) {
        var groupItems = groups[groupName];
        var groupSubtotal = 0;

        // Group header row
        $tbody.append(
            $('<tr class="table-dark">').append(
                $('<td>').attr('colspan', headerCols.length).html('<strong>' + groupName + '</strong>')
            )
        );

        groupItems.forEach(function(item) {
            var total = round2((parseFloat(item.listPriceTaxWithTax)||0) * exchangeRate + (parseFloat(item.serviceFeeInLocal)||0));
            groupSubtotal += parseFloat(item.agreedPriceInLocal) || 0;

            var $tr = $('<tr>');
            if (item.isReceived) $tr.addClass('table-success');

            $tr.append($('<td>').text(item.customerDisplayName || ''));
            $tr.append($('<td>').text(item.productDescription));
            $tr.append($('<td>').text(item.productSourceCode || ''));

            // ProductLink as clickable anchor
            var $linkTd = $('<td style="min-width:250px;">');
            if (item.productLink) {
                $linkTd.append($('<a>').attr('href', item.productLink).attr('target','_blank').text('Ver link'));
            }
            $tr.append($linkTd);

            $tr.append($('<td>').text((parseFloat(item.listPrice)||0).toFixed(2)));
            $tr.append($('<td>').text((parseFloat(item.listPriceTaxWithTax)||0).toFixed(2)));
            $tr.append($('<td>').text((parseFloat(item.serviceFeeInLocal)||0).toFixed(2)));
            $tr.append($('<td>').text(total.toFixed(2)));
            $tr.append($('<td>').text((parseFloat(item.agreedPriceInLocal)||0).toFixed(2)));
            $tr.append($('<td>').text((parseFloat(item.realPrice)||0).toFixed(2)));
            $tr.append($('<td>').text((parseFloat(item.estimateShipping)||0).toFixed(2)));

            if (isDelivering) {
                var $chk = $('<input type="checkbox" class="is-received-chk">').attr('data-item-id', item.id);
                if (item.isReceived) $chk.prop('checked', true);
                $tr.append($('<td class="text-center">').append($chk));
            }

            // Action buttons
            var $btnTd = $('<td>');
            if (isPending) {
                $('<button class="btn btn-xs btn-info mr-1">Editar</button>')
                    .on('click', function() { openEditItem(item); }).appendTo($btnTd);
            }
            if (item.hasImage) {
                $('<button class="btn btn-xs btn-secondary mr-1">Ver Img</button>')
                    .on('click', function() {
                        $('#preview-img').attr('src', '?handler=ItemImage&itemId='+item.id+'&orderId='+orderId);
                        $('#imagePreviewModal').modal('show');
                    }).appendTo($btnTd);
            }
            if (isPending) {
                $('<button class="btn btn-xs btn-danger">Eliminar</button>')
                    .on('click', function() {
                        if (!confirm('¿Eliminar este ítem?')) return;
                        ajaxPost('Delete', { id: item.id }, function(r) {
                            if (r.success) { loadItems(); refreshTotals(); }
                            else alert(r.error);
                        });
                    }).appendTo($btnTd);
            }
            $tr.append($btnTd);
            $tbody.append($tr);
        });

        // Subtotal row
        grandTotal += groupSubtotal;
        $tbody.append(
            $('<tr class="table-light font-weight-bold">').append(
                $('<td>').attr('colspan', headerCols.length - 1).html('Subtotal <em>' + groupName + '</em>'),
                $('<td>').text(round2(groupSubtotal).toFixed(2))
            )
        );
    });

    // Grand total row
    $tbody.append(
        $('<tr class="table-info font-weight-bold">').append(
            $('<td>').attr('colspan', headerCols.length - 1).text('Total Precio Acordado'),
            $('<td>').text(round2(grandTotal).toFixed(2))
        )
    );

    $table.append($tbody);
    $container.html('').append($table);
    refreshTotals();
}

function loadItems() {
    $.get('?handler=Load&orderId=' + orderId, function(data) {
        renderItemsTable(data || []);
    });
}

// IsReceived checkbox handler (delegated)
$(document).on('change', '.is-received-chk', function() {
    var $chk = $(this);
    var itemId = $chk.data('item-id');
    var isReceived = $chk.is(':checked');
    ajaxPost('ToggleReceived', { itemId: itemId, isReceived: isReceived },
        function(r) {
            if (r.success) {
                $chk.closest('tr').toggleClass('table-success', isReceived);
            } else {
                $chk.prop('checked', !isReceived); // revert
                alert(r.error || 'Error al actualizar.');
            }
        },
        function(msg) {
            $chk.prop('checked', !isReceived);
            alert(msg);
        }
    );
});
```

Replace all `$('#jsGrid').jsGrid('loadData')` calls with `loadItems()`, and initial `autoload` call with `loadItems()`.

**Verify**: Grid muestra grupos con subtotales y total general. Contador se actualiza. URLs aparecen como links. Checkbox visible solo en Delivering/Delivered.

---

### Phase D — Reactive Pricing (P1)

**Goal**: `RealPrice` sigue a `ListPrice` para nuevos ítems; comportamiento correcto en edición.

**`items.js` changes**:
1. Añadir `var userEditedRealPrice = false;` al inicio del archivo.
2. En `openAddItem()`: añadir `userEditedRealPrice = false;`.
3. En `openEditItem(item)`: añadir:
   ```js
   userEditedRealPrice = Math.abs((parseFloat(item.realPrice)||0) - (parseFloat(item.listPrice)||0)) > 0.01;
   ```
4. En `$('#item-list-price').on('input', ...)`: reemplazar:
   ```js
   // ANTES: if (!$('#item-real-price').val() || parseFloat($('#item-real-price').val()) === 0)
   // DESPUÉS:
   if (!userEditedRealPrice) {
       $('#item-real-price').val(lp.toFixed(2));
   }
   ```
5. Nuevo listener:
   ```js
   $('#item-real-price').on('input', function() { userEditedRealPrice = true; });
   ```

**Verify**: Test según US-4 escenarios 1–5 del spec.

---

### Phase E — URL Field Width (P2)

**Goal**: Campo Link ocupa fila completa en el modal.

**`Items.cshtml` changes** (modal body, sección de ProductLink):
```html
<!-- Fila dedicada para ProductLink -->
<div class="row">
    <div class="col-md-12">
        <div class="form-group">
            <label>Link del Producto</label>
            <input id="item-product-link" type="text" class="form-control" placeholder="https://..." />
        </div>
    </div>
</div>
<!-- ProductSourceCode en su propia fila (o junto a imagen) -->
<div class="row">
    <div class="col-md-6">
        <div class="form-group">
            <label>Código Fuente</label>
            <input id="item-product-source-code" type="text" class="form-control" />
        </div>
    </div>
</div>
```

**Verify**: Campo Link ocupa ancho completo del modal.

---

## Dependency Order

```
Phase A (DB migration)
    ↓
Phase B (Backend)
    ↓
Phase C + Phase D + Phase E (Frontend, paralelos)
```

---

## Risk Assessment

| Riesgo | Impacto | Mitigación |
|--------|---------|-----------|
| Eliminación de jsGrid rompe `refreshTotals()` | Alto | Mantener `refreshTotals()` sin cambios; reemplazar solo el render |
| Race condition en carga de items + customers | Medio | Backend retorna `CustomerDisplayName` directamente |
| `table-success` no persiste en recarga | Bajo | Clase aplicada en render según valor `isReceived` del servidor |
| Migration en producción genera downtime | Bajo | `ADD COLUMN` con DEFAULT no bloquea en SQL Server (tablas pequeñas) |
