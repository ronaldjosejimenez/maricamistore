# Review Guide: Defaults por proveedor, calculadora y fórmulas de ítems

**Generated**: 2026-10-25 | **Spec**: [spec.md](spec.md)

## Why This Change

El impuesto, la moneda, el tipo de cambio y el costo de envío dependen del proveedor, pero hoy salen de la Configuración global o están fijos en la calculadora (que además era solo de SHEIN). Por eso hay que digitarlos a mano en cada orden y en cada cotización. Además, en el modal de ítems el Precio Real no reflejaba el costo real (impuesto + envío) y las fórmulas no coincidían con las de la calculadora.

## What Changes

Cada proveedor tiene su impuesto, moneda, tipo de cambio y costo de envío por defecto, más una marca "Activo para la calculadora". La Calculadora cotiza por proveedor: agrega el envío (que se duplica con "Envío rápido o local") y redondea el Total a la centena superior. Se elimina la funcionalidad de "cuenta del cliente" y la lectura de parámetros por URL. Al crear una orden, elegir el proveedor llena TC, Impuesto y Moneda. En el modal de ítems: Precio Real = Precio + Imp + Envío, se agrega el campo nuevo Precio Real en Local (× TC), Total = Precio Real en Local + Tarifa, los campos se reordenan y quedan bloqueados hasta elegir el Tipo de Producto. **Cambio de comportamiento:** los totales del encabezado solo se recalculan y guardan en órdenes Pendientes; las órdenes en otros estados ya no se reescriben al abrirlas.

## How It Works

- **Esquema**: una sola migración (`SupplierDefaultsAndRealPriceInLocal`) agrega 5 columnas a `Suppliers` y `RealPriceInLocal` a `OrderItems`. Al final de `Up`, un SQL llena los 3 defaults de los proveedores desde `TOP 1 Configurations`, activa Shein y hace el backfill de `RealPriceInLocal = RealPrice × Orders.ExchangeRate`.
- **Proveedores**: el grid jsGrid carga monedas y Configuración en paralelo; `insertTemplate` precarga la fila de inserción. `CatalogService.ValidateSupplier` lanza `ArgumentException` y la página la convierte en HTTP 400.
- **Calculadora**: el script inline pasa a `wwwroot/js/pages/calculator/index.js`; el nuevo handler `OnGetSuppliersAsync` devuelve solo los proveedores activos.
- **Órdenes**: `orders/index.js` reutiliza la lista de proveedores que ya carga y aplica los defaults solo en creación.
- **Ítems**: `recalcModal(source)` encadena Lista → Precio + Imp → Precio Real → P. Real en Local → Total → Acordado, respetando los flags de edición manual. El servidor calcula `RealPriceInLocal` al guardar. El encabezado usa Σ ListPrice como Subtotal (cliente y servidor). `refreshTotals()` solo recalcula y persiste si la orden está Pendiente; en otro caso formatea `savedTotals` emitidos por Razor. Trigger A (cambio de Imp o TC) y Reasignar se adaptan.

## When It Applies

**Applies when**:
- Se crea o edita un proveedor en el catálogo.
- Se cotiza en la Calculadora.
- Se crea una orden nueva y se selecciona un proveedor.
- Se agrega o edita un ítem, o se cambia el Imp/TC de una orden **Pendiente**.
- Se reasigna un ítem (Activa, Entregando, Completada): solo se actualizan Precio Acordado Total y Ganancia.

**Does not apply when**:
- Se edita una orden existente y se cambia el proveedor: no se recargan los defaults (FR-014).
- Se abren órdenes no Pendientes: sus totales no se recalculan (FR-022).
- Hay ítems ya guardados: no se recalculan en lote; Precio Real conserva su valor histórico.
- Hay reportes y otras pantallas: muestran los valores guardados.

## Key Decisions

1. **Subtotal = Σ Precio Lista** (no Σ Precio Real). El proveedor (Lista + Imp) y el transportista (Envío) se pagan por separado, y el nuevo Precio Real ya incluye ambos; usarlo como base cobraría dos veces el impuesto y sumaría dos veces el envío.
2. **Recálculo de totales solo en órdenes Pendientes.** Se consideró recalcular todas (comportamiento previo) o Pendientes + Activas. Se eligió solo Pendientes para no alterar órdenes viejas.
3. **Trigger A mantiene el recálculo forzado de Precio Acordado al cambiar el TC**, ahora con la fórmula nueva. Precio Real solo se recalcula si no fue editado a mano.
4. **`CurrencyIdDefault` nullable en BD**, con la obligatoriedad validada en el servicio, para que la migración no falle sin Configuración.
5. **`RealPriceInLocal` persistido y calculado en el servidor**, sin confiar en el valor del cliente.
6. **Solo Shein queda activo** para la calculadora tras la migración, para que la pantalla no quede vacía al publicar.

## Areas Needing Attention

- **Ítems existentes**: su Precio Real tiene la semántica vieja (≈ Lista), así que al abrirlos se detectan como "editado a mano" y no se recalculan solos. No afecta los totales, pero puede confundir.
- **Órdenes Pendientes existentes**: al abrirlas sí se recalcula el Subtotal con Σ Lista. Si alguna tenía Precio Real ajustado a mano, su Subtotal cambia.
- **Proveedores globales vs. Configuración por organización**: los defaults se toman de `TOP 1` Configuración. Es correcto hoy (una organización), pero no escala a varias.
- **Redondeo de la calculadora** (`Math.ceil` a la centena) solo en el Total; los ítems de orden no redondean, así que una cotización y el ítem resultante pueden diferir hasta en ₡99.
- **Sin pruebas automatizadas**: la verificación depende del quickstart manual con valores exactos.

## Open Questions

- El título de la Calculadora todavía dice "SHEIN" aunque ahora es multiproveedor (cambio cosmético pendiente de confirmar).

## Review Checklist

- [ ] Key decisions are justified
- [ ] Breaking changes are documented with migration guidance
- [ ] Scope matches the stated boundaries
- [ ] Success criteria are achievable
- [ ] No unstated assumptions
- [ ] El SQL de la migración corre después de los `UpdateData` de los seeds
- [ ] Una orden Completada no cambia sus totales al abrirla (quickstart §4.8)

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
