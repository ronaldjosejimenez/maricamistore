# Feature Specification: Defaults por proveedor, calculadora y fórmulas de ítems

**Feature Branch**: `018-defaults-proveedor-calculadora`

**Created**: 2026-10-25

**Status**: Draft

**Input**: User description: "Basado en el brainstorm brainstorm/18-defaults-proveedor-calculadora.md (short name: defaults-proveedor-calculadora). Implementar las fórmulas tal como están en el requerimiento; resolver las Open Questions del brainstorm en el spec."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Valores por defecto en el catálogo de proveedores (Priority: P1)

El administrador define, para cada proveedor, el impuesto, la moneda, el tipo de cambio y el costo de envío por defecto, y marca si el proveedor aparece en la calculadora. Al crear un proveedor nuevo, el impuesto, la moneda y el tipo de cambio vienen precargados desde la Configuración y se pueden modificar. Los proveedores que ya existen reciben esos mismos tres valores de la Configuración.

**Why this priority**: Las historias 2 y 3 dependen de estos datos.

**Independent Test**: Crear un proveedor y verificar que impuesto, moneda y tipo de cambio coinciden con la Configuración; editarlos y guardar; verificar que los proveedores preexistentes tienen esos tres valores cargados.

**Acceptance Scenarios**:

1. **Given** una Configuración con impuesto 7 %, moneda USD y tipo de cambio 530, **When** el usuario agrega un proveedor nuevo, **Then** los campos Impuesto default, Moneda default y Tipo de cambio default muestran 7, USD y 530, y el usuario puede cambiarlos antes de guardar.
2. **Given** un proveedor existente, **When** el usuario edita su costo de envío por defecto y su marca "Activo para la calculadora", **Then** los cambios se guardan y se ven en el catálogo.
3. **Given** proveedores creados antes de esta funcionalidad, **When** se aplica la actualización del sistema, **Then** cada uno tiene Impuesto, Moneda y Tipo de cambio default iguales a los de la Configuración.

---

### User Story 2 - Calculadora basada en proveedor (Priority: P1)

El usuario abre la Calculadora y elige un proveedor de una lista que solo muestra los proveedores activos para la calculadora. Impuesto, Tipo de cambio y Costo envío se llenan con los valores del proveedor. Si marca "Envío rápido o local", el costo de envío se duplica. El usuario calcula el precio final con la nueva fórmula, que incluye el envío.

**Why this priority**: Es la herramienta diaria de cotización; el cambio de fórmula afecta los precios que se le dan al cliente.

**Independent Test**: Seleccionar un proveedor activo, ingresar el precio de lista y el costo de servicio, calcular y comparar contra la fórmula; marcar y desmarcar el checkbox de envío.

**Acceptance Scenarios**:

1. **Given** proveedores A (activo para la calculadora) y B (inactivo), **When** el usuario abre la Calculadora, **Then** el combo "Proveedores" muestra A y no muestra B.
2. **Given** el proveedor A con impuesto 7 %, tipo de cambio 530 y envío 2, **When** el usuario lo selecciona, **Then** Impuesto = 7, Tipo de cambio = 530 y Costo envío = 2.
3. **Given** Costo envío por defecto = 2, **When** el usuario marca "Envío rápido o local", **Then** Costo envío = 4; **When** lo desmarca, **Then** Costo envío = 2.
4. **Given** Precio de lista 10, Impuesto 7 %, Costo envío 2, Tipo de cambio 530 y Costo servicio 3000, **When** el usuario presiona Calcular, **Then** Precio con impuesto = 12.70, Precio en colones = 6731.00 y Total = 9800 (9731 redondeado a la centena superior).
6. **Given** un Total calculado de 5089, 17890 o 5100, **When** se muestra el resultado, **Then** el Total es 5100, 17900 o 5100 respectivamente.
5. **Given** la Calculadora, **When** se abre, **Then** no existen los campos "Nombre del cliente" ni "Código o descripción de la prenda", los botones "Agregar a cuenta" y "Nueva cuenta", ni el grid "Cuenta del cliente"; y los valores que vengan en la URL se ignoran.

---

### User Story 3 - Orden nueva con defaults del proveedor (Priority: P2)

Al crear una orden y seleccionar el proveedor, la orden toma el tipo de cambio, el impuesto y la moneda por defecto de ese proveedor.

**Why this priority**: Reduce errores de captura, pero el usuario ya puede ingresar estos valores a mano.

**Independent Test**: Crear una orden, seleccionar un proveedor y verificar los tres campos; cambiar de proveedor y verificar que se actualizan.

**Acceptance Scenarios**:

1. **Given** el proveedor A con tipo de cambio 520, impuesto 13 % y moneda EUR, **When** el usuario crea una orden y selecciona A, **Then** Tipo de cambio = 520, Impuesto = 13 y Moneda = EUR.
2. **Given** una orden nueva con el proveedor A seleccionado, **When** el usuario cambia a B, **Then** los tres campos toman los defaults de B.
3. **Given** una orden existente, **When** el usuario la edita y cambia el proveedor, **Then** tipo de cambio, impuesto y moneda no cambian.

---

### User Story 4 - Campos de precio del ítem reordenados y con nuevas fórmulas (Priority: P2)

En el modal de ítem de una orden, los campos de precio aparecen en el nuevo orden. Precio Real incluye el impuesto y el envío estimado; un campo nuevo, Precio Real en Local, lo convierte con el tipo de cambio; Total (calculado) suma la Tarifa Servicio; y Precio Acordado toma por defecto el Total. Los campos de precio no se pueden editar hasta que se elija un Tipo de Producto. Las fórmulas quedan alineadas con la Calculadora.

**Why this priority**: Mejora la captura y la exactitud del precio, pero no bloquea la operación.

**Independent Test**: Abrir "Agregar Ítem", verificar el orden de los campos y que estén bloqueados; elegir un Tipo de Producto, ingresar el precio de lista y comparar contra las fórmulas.

**Acceptance Scenarios**:

1. **Given** el modal de ítem, **When** se abre, **Then** la fila 1 muestra Precio Lista, Precio + Imp, Envío estimado y Precio Real, y la fila 2 muestra Precio Real en Local, Tarifa Servicio, Total (calculado) y Precio Acordado.
2. **Given** un ítem nuevo sin Tipo de Producto, **When** se abre el modal, **Then** los ocho campos de precio están deshabilitados; **When** el usuario elige un Tipo de Producto, **Then** se habilitan (Precio Real en Local y Total (calculado) siempre son de solo lectura).
3. **Given** impuesto de la orden 7 %, tipo de cambio 530, un Tipo de Producto con envío estimado 3 y tarifa 3000, **When** el usuario ingresa Precio Lista 10, **Then** Precio + Imp = 10.70, Precio Real = 13.70, Precio Real en Local = 7261.00, Total (calculado) = 10261.00 y Precio Acordado = 10261.00.
4. **Given** valores calculados, **When** el usuario no ha editado Precio Acordado a mano, **Then** Precio Acordado = Total (calculado); **When** lo editó a mano, **Then** conserva el valor del usuario (regla actual).

---

### Edge Cases

- No hay proveedores activos para la calculadora: el combo aparece vacío con un aviso; los campos se pueden llenar a mano y se puede calcular igual.
- El proveedor seleccionado en la calculadora no tiene costo de envío definido: Costo envío = 0; con el checkbox marcado sigue en 0.
- El usuario edita Costo envío a mano y luego marca o desmarca el checkbox: Costo envío pasa a ser el valor por defecto del proveedor × 2 (o × 1); la edición manual se reemplaza.
- El usuario cambia de proveedor en la calculadora: Impuesto, Tipo de cambio y Costo envío se recargan del nuevo proveedor (aplicando × 2 si el checkbox está marcado), reemplazando lo que se había editado.
- La Configuración no existe al crear un proveedor: los tres campos aparecen vacíos y son obligatorios.
- Un ítem existente se abre para editar: su Tipo de Producto ya está seleccionado, así que los campos están habilitados; los valores guardados no se recalculan hasta que el usuario modifique un campo.
- El usuario editó Precio Real a mano y luego cambia Precio Lista o Envío estimado: Precio Real conserva el valor manual (misma regla que hoy).
- Modo "Ver" del ítem: todos los campos siguen en solo lectura, como hoy.

## Requirements *(mandatory)*

### Functional Requirements

**Proveedores**

- **FR-001**: Cada proveedor MUST tener: Impuesto default (%), Moneda default, Tipo de cambio default, Costo de envío default y "Activo para la calculadora" (sí/no). Todos son editables desde el catálogo de proveedores.
- **FR-002**: Al crear un proveedor, el sistema MUST precargar Impuesto default ← Impuesto de la Configuración, Moneda default ← Moneda de orden por defecto de la Configuración y Tipo de cambio default ← Tipo de cambio de la Configuración (se usa el primer registro de Configuración).
- **FR-003**: Un proveedor nuevo MUST arrancar con Costo de envío default = 0 y "Activo para la calculadora" = no, salvo que el usuario cambie esos valores.
- **FR-004**: La actualización del sistema MUST llenar Impuesto, Moneda y Tipo de cambio default en todos los proveedores existentes con los valores del primer registro de Configuración.

**Calculadora**

- **FR-005**: La Calculadora MUST mostrar al inicio un combo "Proveedores" con solo los proveedores que tienen "Activo para la calculadora" = sí.
- **FR-006**: Al seleccionar un proveedor, la Calculadora MUST cargar Impuesto ← Impuesto default, Tipo de cambio ← Tipo de cambio default y Costo envío ← Costo de envío default (× 2 si "Envío rápido o local" está marcado).
- **FR-007**: La Calculadora MUST tener el checkbox "Envío rápido o local"; al marcarlo, Costo envío = Costo de envío default del proveedor × 2, y al desmarcarlo vuelve al valor default.
- **FR-008**: La Calculadora MUST eliminar los campos "Nombre del cliente" y "Código o descripción de la prenda", los botones "Agregar a cuenta" y "Nueva cuenta" con su lógica, y el grid "Cuenta del cliente".
- **FR-009**: Todos los campos de entrada de la Calculadora MUST seguir siendo editables.
- **FR-010**: La Calculadora MUST calcular:
  - Precio con impuesto = Precio de lista + (Precio de lista × Impuesto / 100) + Costo envío
  - Precio en colones = Precio con impuesto × Tipo de cambio
  - Total = Precio en colones + Costo servicio, redondeado siempre hacia arriba a la centena (5089 → 5100, 17890 → 17900; un valor que ya es múltiplo de 100 no cambia)
- **FR-010a**: Las fórmulas de la Calculadora MUST ser equivalentes a las del ítem de orden: Precio con impuesto ≡ Precio Real, Precio en colones ≡ Precio Real en Local y Total ≡ Total (calculado). La única diferencia es el redondeo a la centena, que aplica solo en la Calculadora.
- **FR-011**: La Calculadora MUST ignorar cualquier valor recibido en la URL (query string).
- **FR-012**: "Limpiar" MUST restablecer los campos a los valores del proveedor seleccionado (o vacíos si no hay ninguno), desmarcar el checkbox y poner los resultados en 0.

**Órdenes**

- **FR-013**: Al crear una orden, cada vez que el usuario selecciona un proveedor, el sistema MUST cargar Tipo de cambio ← Tipo de cambio default, Impuesto ← Impuesto default y Moneda ← Moneda default de ese proveedor. Los campos siguen siendo editables.
- **FR-014**: Al editar una orden existente, cambiar el proveedor MUST NOT modificar el tipo de cambio, el impuesto ni la moneda.

**Ítems de la orden**

- **FR-015**: El modal de ítem MUST mostrar los campos de precio en este orden — Fila 1: Precio Lista, Precio + Imp, Envío estimado, Precio Real. Fila 2: Precio Real en Local, Tarifa Servicio, Total (calculado), Precio Acordado.
- **FR-016**: La etiqueta "Envío Est." del modal MUST cambiar a "Envío estimado".
- **FR-017**: Precio Real MUST calcularse como Precio Lista + Impuesto (Precio Lista × Imp% de la orden) + Envío estimado, es decir Precio + Imp + Envío estimado. Se recalcula al cambiar cualquiera de sus componentes mientras el usuario no lo haya editado a mano (regla actual de edición de Precio Real).
- **FR-018**: El ítem MUST tener un campo nuevo, "Precio Real en Local" = Precio Real × Tipo de cambio de la orden. Es de solo lectura, se recalcula siempre que cambie Precio Real y se guarda con el ítem.
- **FR-019**: Total (calculado) MUST ser Precio Real en Local + Tarifa Servicio (solo lectura).
- **FR-019a**: Precio Acordado MUST tomar por defecto el valor de Total (calculado) y seguir las reglas actuales: es editable y, una vez editado a mano, ya no se recalcula.
- **FR-020**: Los campos de precio del modal MUST permanecer deshabilitados hasta que el ítem tenga un Tipo de Producto seleccionado.
- **FR-021**: Los totales del encabezado de la orden MUST mantener la estructura y el significado actuales, porque el pago al proveedor y el envío son pagos independientes: al proveedor se le paga el precio de lista + impuesto, y el Envío estimado se le paga al transportista.
  - Subtotal = Σ Precio Lista de los ítems (la base sin impuesto ni envío; antes era Σ Precio Real, cuyo valor por defecto era el Precio Lista)
  - Impuestos = (Subtotal − Descuento) × Imp%
  - Total a Prov. = Envío interno + Subtotal + Impuestos − Descuento
  - Envío a CR = Σ Envío estimado (se paga al transportista)
  - Total Orden = Total a Prov. + Envío a CR
  - Ganancia Est. = Precio Acordado Total − Total Orden × TC
  - El nuevo Precio Real (que ya incluye impuesto y envío) MUST NOT usarse como base del encabezado, para no cobrar dos veces el impuesto ni sumar dos veces el envío.

### Key Entities

- **Proveedor**: nombre; impuesto default (%); moneda default; tipo de cambio default; costo de envío default (en la moneda default del proveedor); activo para la calculadora.
- **Configuración**: fuente de los defaults iniciales (impuesto, moneda de orden por defecto, tipo de cambio). Se toma el primer registro.
- **Orden**: toma tipo de cambio, impuesto y moneda del proveedor al crearse.
- **Ítem de orden**: Precio Lista, Precio + Imp, Envío estimado, Precio Real (incluye impuesto y envío), Precio Real en Local (nuevo), Tarifa Servicio, Total (calculado), Precio Acordado, Tipo de Producto.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100 % de los proveedores existentes tiene impuesto, moneda y tipo de cambio default después de la actualización.
- **SC-002**: Para cualquier combinación de entradas, los resultados de la Calculadora coinciden exactamente (a 2 decimales) con FR-010.
- **SC-003**: Una cotización en la Calculadora se completa en 3 acciones o menos (elegir proveedor, ingresar precio, calcular), sin escribir impuesto, tipo de cambio ni envío a mano.
- **SC-004**: Al crear una orden, el tipo de cambio, el impuesto y la moneda coinciden con los defaults del proveedor elegido en el 100 % de los casos sin edición manual.
- **SC-005**: Ningún ítem nuevo se puede guardar con precios capturados antes de elegir un Tipo de Producto.

## Assumptions

- El "primer registro de Configuración" es la fuente de los defaults aunque la Configuración sea por organización; los proveedores son globales.
- El costo de envío default del proveedor está en la moneda default del proveedor; en la calculadora se suma antes de convertir con el tipo de cambio, como dice la fórmula.
- Los proveedores existentes quedan con Costo de envío default = 0 y "Activo para la calculadora" = no; el administrador los activa desde el catálogo.
- Costo servicio en la Calculadora conserva su valor inicial actual (3000) y no depende del proveedor.
- El impuesto de la calculadora se calcula solo sobre el precio de lista, no sobre el envío.
- Los ítems ya guardados no se recalculan de forma masiva; las nuevas fórmulas aplican al crear o editar un ítem. En los ítems existentes, Precio Real en Local se llena con Precio Real × Tipo de cambio de su orden.
- El redondeo a la centena superior aplica solo al Total de la Calculadora, no a los ítems de orden.
- La validación de la Calculadora permite Costo envío = 0; los demás campos mantienen sus validaciones actuales.
- Las demás pantallas que muestran ítems (tabla de ítems, reportes) siguen mostrando los valores guardados; solo cambia el modal.
