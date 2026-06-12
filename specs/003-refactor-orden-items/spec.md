# Feature Specification: Refactor Orden e Ítems

**Feature Branch**: `003-refactor-orden-items`

**Created**: 2026-06-04

**Status**: Draft

**Input**: User description: Refactoring mayor del sistema de Orden e Ítems de Orden — modal para ítems, edición in-place, nuevos campos, renombres, lógica de cálculo completa. Basado en `requerimientos/Mejoras Fase 1 v1.txt` y brainstorm `brainstorm/03-refactor-orden-items.md`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Gestión de Ítems con Modal Dedicado (Priority: P1)

El operador puede agregar y editar ítems de una orden a través de un modal dedicado (en lugar de la edición inline actual), con todos los campos requeridos accesibles en el formulario.

**Why this priority**: La cantidad de campos de un ítem hace que la edición inline sea inutilizable. El modal es la base para las demás mejoras de ítems (nuevos campos, imagen, filtros).

**Independent Test**: Abrir una orden en estado Pending, hacer clic en "Agregar Ítem" — debe abrirse un modal con todos los campos. Guardar el ítem — debe aparecer en la grilla. Hacer clic en editar sobre un ítem existente — el modal debe abrirse con los valores cargados.

**Acceptance Scenarios**:

1. **Given** una orden en estado Pending, **When** el usuario hace clic en "Agregar Ítem", **Then** se abre un modal con todos los campos del ítem disponibles para ingresar.
2. **Given** el modal de ítem abierto, **When** el usuario completa los campos obligatorios y guarda, **Then** el ítem aparece en la grilla de la orden y el modal se cierra.
3. **Given** un ítem existente en la grilla, **When** el usuario hace clic en editar, **Then** el modal se abre con todos los valores del ítem cargados para edición.
4. **Given** una orden en estado distinto de Pending, **When** el usuario intenta editar un ítem, **Then** el botón de editar no está disponible o el modal se abre en modo solo lectura.

---

### User Story 2 — Edición In-Place de la Orden (Priority: P1)

El operador puede editar directamente los campos editables de la orden en la pantalla de resumen sin necesidad de abrir un diálogo separado. La pantalla permanece en modo edición mientras el estado sea Pending.

**Why this priority**: El diálogo separado de edición de orden genera fricción innecesaria. La edición in-place reduce los pasos necesarios para actualizar ExchangeRate, TaxPercentage, ShippingAmountIntern y DiscountAmount.

**Independent Test**: Abrir la pantalla de detalle de una orden Pending — los campos editables (`ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`) deben mostrarse como inputs activos directamente en la pantalla, sin botón de "editar orden". Modificar un valor y guardar — el cambio debe persistir.

**Acceptance Scenarios**:

1. **Given** una orden en estado Pending, **When** el usuario carga la pantalla de detalle, **Then** los campos `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount` se muestran como campos editables en la misma pantalla.
2. **Given** una orden en estado Pending con campos editables visibles, **When** el usuario modifica un valor y presiona Guardar, **Then** el cambio persiste y se muestra el aviso "Presionar guardar para recalcular si hay algo pendiente de guardar" mientras haya cambios sin guardar.
3. **Given** una orden en estado distinto de Pending, **When** el usuario carga la pantalla, **Then** todos los campos de la orden se muestran como solo lectura (no editables).
4. **Given** una orden en estado Pending, **When** no hay cambios pendientes, **Then** el aviso de "pendiente de guardar" no es visible.

---

### User Story 3 — Nuevos Campos en Ítems: ProductLink, ProductSourceCode, ProductImage (Priority: P1)

El operador puede registrar un enlace al producto, un código de fuente y una imagen en cada ítem. En modo visualización, puede hacer clic en un botón para ver la imagen en un popup.

**Why this priority**: Estos campos son datos de trazabilidad del producto que deben capturarse en el momento de crear el ítem. Sin ellos, la información queda incompleta.

**Independent Test**: Abrir el modal de nuevo ítem — deben aparecer los campos `ProductLink` (texto), `ProductSourceCode` (texto) y `ProductImage` (upload de imagen). Guardar un ítem con imagen — en la grilla de query, debe aparecer un botón para previsualizar la imagen en un popup.

**Acceptance Scenarios**:

1. **Given** el modal de ítem abierto, **When** el usuario llena `ProductLink` y `ProductSourceCode`, **Then** los valores se guardan correctamente (ambos campos son opcionales).
2. **Given** el modal de ítem abierto, **When** el usuario sube una imagen de hasta 2 MB, **Then** la imagen se guarda con el ítem.
3. **Given** el modal de ítem abierto, **When** el usuario intenta subir una imagen mayor a 2 MB, **Then** se muestra un mensaje de error y la imagen no se guarda.
4. **Given** un ítem con imagen guardada en la grilla de visualización, **When** el usuario hace clic en el botón de previsualizar, **Then** se abre un popup mostrando la imagen.
5. **Given** un ítem sin imagen, **When** el usuario visualiza la grilla, **Then** no aparece botón de previsualizar imagen para ese ítem.

---

### User Story 4 — Cálculos Automáticos en Ítems (Priority: P1)

Al ingresar o modificar datos en el modal de ítem, los campos calculados (`ListPriceTaxWithTax`, `Total`, `AgreedPriceInLocal`, `RealPrice`) se actualizan automáticamente siguiendo las fórmulas definidas, sin necesidad de guardar primero.

**Why this priority**: Sin los cálculos automáticos, el operador no puede verificar si los valores son correctos antes de guardar, lo que genera errores en los totales de la orden.

**Independent Test**: Abrir el modal de nuevo ítem, ingresar un `ListPrice` — `ListPriceTaxWithTax` debe calcularse al instante (`ListPrice + (ListPrice * TaxPercentage)`). Ingresar todos los campos requeridos — `Total` y `AgreedPriceInLocal` deben actualizarse en tiempo real.

**Acceptance Scenarios**:

1. **Given** el modal de ítem con `ListPrice` ingresado, **When** el usuario escribe en `ListPrice`, **Then** `ListPriceTaxWithTax` se actualiza automáticamente como `ListPrice + (ListPrice * TaxPercentage de la orden)`.
2. **Given** `ListPriceTaxWithTax` y `ExchangeRate` disponibles, **When** cualquiera de estos cambia, **Then** `Total` se recalcula como `(ListPriceTaxWithTax * ExchangeRate) + ServiceFeeInLocal`.
3. **Given** un ítem nuevo, **When** `Total` cambia, **Then** `AgreedPriceInLocal` se actualiza para coincidir con `Total` (a menos que el usuario lo haya editado manualmente).
4. **Given** un ítem nuevo, **When** se selecciona un `ProductType`, **Then** `ServiceFeeInLocal` se carga con el valor por defecto del ProductType seleccionado y `EstimateShipping` también se carga desde el ProductType.
5. **Given** un ítem nuevo, **When** se escribe un `ListPrice`, **Then** `RealPrice` toma el mismo valor por defecto.

---

### User Story 5 — Filtro de ProductType por Moneda de la Orden (Priority: P2)

Al seleccionar el ProductType de un ítem, el dropdown solo muestra los ProductTypes cuya moneda coincide con la moneda configurada en la orden.

**Why this priority**: Mezclar ítems con monedas distintas genera inconsistencias en el cálculo de totales. Este filtro previene errores del operador.

**Independent Test**: Abrir el modal de nuevo ítem en una orden cuya moneda sea USD — el dropdown de ProductType solo debe mostrar ProductTypes con moneda USD. Cambiar a una orden con moneda CRC — el dropdown debe mostrar solo ProductTypes CRC.

**Acceptance Scenarios**:

1. **Given** una orden con moneda X, **When** el usuario abre el modal de ítem, **Then** el dropdown de ProductType solo muestra ítems cuya moneda sea X.
2. **Given** el dropdown de ProductType filtrado, **When** no hay ProductTypes disponibles para la moneda de la orden, **Then** el dropdown muestra un mensaje indicando que no hay opciones disponibles.

---

### User Story 6 — Recálculo de Totales de la Orden (Priority: P1)

Al guardar la orden (cuando cambia TaxPercentage o ExchangeRate) o al agregar/editar/eliminar un ítem, el sistema recalcula automáticamente todos los campos de totales de la orden (`TotalAgreedPriceInLocal`, `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal`).

**Why this priority**: Los totales de la orden son el dato más crítico para la toma de decisiones. Sin recálculo automático, los totales estarían desactualizados.

**Independent Test**: Agregar un ítem a una orden — los totales de la orden deben actualizarse inmediatamente. Cambiar el ExchangeRate de la orden y guardar — los totales deben recalcularse aplicando las fórmulas definidas.

**Acceptance Scenarios**:

1. **Given** una orden con ítems, **When** el usuario agrega un nuevo ítem, **Then** los campos de totales se recalculan según la Order Calculation Suite.
2. **Given** una orden con ítems, **When** el usuario edita o elimina un ítem, **Then** los campos de totales se recalculan según la Order Calculation Suite.
3. **Given** una orden con ítems, **When** el usuario cambia `TaxPercentage` o `ExchangeRate` y guarda la orden, **Then** `ListPriceTaxWithTax` y `ServiceFeeInLocal` se recalculan para todos los ítems, y luego se ejecuta la Order Calculation Suite.
4. **Given** una orden recién guardada, **When** el usuario visualiza los totales, **Then** `EstimatedProfitInLocal` refleja la ganancia estimada según la fórmula: `TotalAgreedPriceInLocal - (TotalOfTheOrder * ExchangeRate)`.

---

### User Story 7 — Migración de Esquema (Priority: P0 — Prerequisito)

El sistema aplica las migraciones de base de datos necesarias: renombrar `ListPriceTax` a `ListPriceTaxWithTax`, agregar los nuevos campos en OrderItem (`ProductLink`, `ProductSourceCode`, `ProductImage`), eliminar `RequestedAt` de Order y agregar `TotalAgreedPriceInLocal` a Order.

**Why this priority**: Sin las migraciones, ninguna de las otras historias puede implementarse. Es el prerequisito bloqueante.

**Independent Test**: Ejecutar las migraciones en un entorno de desarrollo — la base de datos debe tener la nueva estructura sin pérdida de datos existentes.

**Acceptance Scenarios**:

1. **Given** una base de datos con datos existentes, **When** se ejecutan las migraciones, **Then** los datos existentes en `ListPriceTax` se preservan en la columna renombrada `ListPriceTaxWithTax`.
2. **Given** la migración aplicada, **When** el sistema crea un nuevo ítem, **Then** los campos `ProductLink`, `ProductSourceCode`, `ProductImage` están disponibles (nullable).
3. **Given** la migración aplicada, **When** se consulta la tabla de órdenes, **Then** el campo `RequestedAt` ya no existe y `TotalAgreedPriceInLocal` está disponible.

---

### Edge Cases

- ¿Qué pasa si el usuario sube una imagen exactamente igual a 2 MB? Debe aceptarse.
- ¿Qué pasa si el usuario edita `AgreedPriceInLocal` manualmente y luego cambia `ListPrice`? El campo editado manualmente no debe sobreescribirse.
- ¿Qué pasa si una orden no tiene ítems? Todos los totales deben ser 0.
- ¿Qué pasa si el `ExchangeRate` es 0 o vacío al abrir el modal de ítem? Los campos que dependen de él deben mostrar 0 o N/A sin romper la UI.
- ¿Qué pasa si no hay ProductTypes disponibles para la moneda de la orden? El dropdown muestra un mensaje amigable.
- ¿Qué pasa si la migración falla a mitad? La migración debe ser transaccional o reversible.

---

## Requirements *(mandatory)*

### Functional Requirements

**Migraciones de Base de Datos**

- **FR-001**: El sistema DEBE renombrar la columna `ListPriceTax` a `ListPriceTaxWithTax` en la tabla de ítems de orden, preservando todos los datos existentes.
- **FR-002**: El sistema DEBE agregar los campos `ProductLink` (texto, nullable), `ProductSourceCode` (texto, nullable) y `ProductImage` (binario, nullable, máximo 2 MB) a la tabla de ítems de orden.
- **FR-003**: El sistema DEBE eliminar el campo `RequestedAt` de la tabla de órdenes.
- **FR-004**: El sistema DEBE agregar el campo `TotalAgreedPriceInLocal` (decimal) a la tabla de órdenes.

**Modal de Ítems**

- **FR-005**: El sistema DEBE reemplazar la edición inline de ítems por un modal dedicado para agregar y editar ítems.
- **FR-006**: El modal de ítem DEBE incluir todos los campos: `CustomerId` (combobox con búsqueda), `ProductDescription`, `ProductLink`, `ProductSourceCode`, `ProductImage` (upload), `ProductTypeId` (dropdown filtrado por moneda), `ListPrice`, `ListPriceTaxWithTax`, `ServiceFeeInLocal`, `Total`, `AgreedPriceInLocal`, `RealPrice`, `EstimateShipping`.
- **FR-007**: El campo `ProductImage` DEBE validar que el archivo no supere 2 MB tanto en el frontend como en el backend.
- **FR-008**: En la vista de grilla (Query), los ítems con imagen DEBEN mostrar un botón de previsualización que abra la imagen en un popup/modal.
- **FR-009**: El dropdown `ProductTypeId` DEBE mostrar únicamente los ProductTypes cuya moneda coincida con la moneda de la orden activa.

**Edición In-Place de la Orden**

- **FR-010**: El sistema DEBE eliminar el diálogo separado de edición de la orden y permitir la edición directamente en la pantalla de resumen.
- **FR-011**: En una orden en estado Pending, los campos `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern` y `DiscountAmount` DEBEN mostrarse como campos editables en la pantalla de resumen.
- **FR-012**: En una orden en estado Pending, los campos `SupplierId`, `NameOfOrder`, `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal` DEBEN mostrarse como solo lectura.
- **FR-013**: El sistema DEBE mostrar el aviso "Presionar guardar para recalcular si hay algo pendiente de guardar" únicamente cuando existan cambios no guardados en la pantalla.
- **FR-014**: En una orden en estado distinto de Pending, todos los campos DEBEN ser solo lectura.

**Formulario de Creación de Nueva Orden**

- **FR-015**: El formulario de creación de nueva orden DEBE mostrar únicamente: `SupplierId`, `NameOfOrder`, `ExchangeRate`, `TaxPercentage`.

**Cálculos Automáticos en Ítems**

- **FR-016**: Al escribir `ListPrice`, el sistema DEBE calcular automáticamente `ListPriceTaxWithTax = ListPrice + (ListPrice * TaxPercentage de la orden)`.
- **FR-017**: Al cambiar `ListPriceTaxWithTax`, `ExchangeRate` o `ServiceFeeInLocal`, el sistema DEBE recalcular `Total = (ListPriceTaxWithTax * ExchangeRate) + ServiceFeeInLocal`.
- **FR-018**: En un ítem nuevo (no editado manualmente), `AgreedPriceInLocal` DEBE seguir el valor de `Total` automáticamente.
- **FR-019**: Si el usuario edita `AgreedPriceInLocal` manualmente, el campo NO DEBE ser sobreescrito por recálculos automáticos posteriores (a menos que cambie el `ExchangeRate` de la orden).
- **FR-020**: Al cambiar el `ExchangeRate` de la orden (Trigger A), el sistema DEBE recalcular `AgreedPriceInLocal` para TODOS los ítems.
- **FR-021**: Al seleccionar un `ProductType`, el sistema DEBE cargar automáticamente el `ServiceFeeInLocal` y `EstimateShipping` por defecto desde el ProductType seleccionado.
- **FR-022**: En un ítem nuevo, `RealPrice` DEBE tomar el valor de `ListPrice` por defecto. Es editable manualmente.

**Moneda de la Orden**

- **FR-032**: Cada orden DEBE tener un campo de moneda que por defecto toma el valor del campo "moneda por defecto" de la configuración de la organización activa.
- **FR-033**: El campo de moneda de la orden DEBE ser editable únicamente mientras el estado sea Pending **o** mientras no se hayan agregado ítems a la orden.
- **FR-034**: Una vez que la orden tiene ítems y su estado no es Pending, el campo de moneda DEBE ser solo lectura.

**Order Calculation Suite (Totales de la Orden)**

- **FR-023**: Al agregar, editar o eliminar un ítem, el sistema DEBE ejecutar la Order Calculation Suite actualizando en orden: `TotalAgreedPriceInLocal`, `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal`.
- **FR-024**: Al guardar la orden con cambios en `TaxPercentage` o `ExchangeRate`, el sistema DEBE recalcular `ListPriceTaxWithTax` y `ServiceFeeInLocal` para todos los ítems, y luego ejecutar la Order Calculation Suite.
- **FR-025**: `TotalAgreedPriceInLocal` = Σ `AgreedPriceInLocal` de todos los ítems.
- **FR-026**: `ShippingAmountToCR` = Σ `EstimateShipping` de todos los ítems.
- **FR-027**: `TotalWithoutTaxes` = Σ `RealPrice` de todos los ítems.
- **FR-028**: `TaxesAmount` = `(TotalWithoutTaxes - DiscountAmount) * TaxPercentage`.
- **FR-029**: `TotalToPayToSupplier` = `ShippingAmountIntern + TotalWithoutTaxes + TaxesAmount - DiscountAmount`.
- **FR-030**: `TotalOfTheOrder` = `TotalToPayToSupplier + ShippingAmountToCR`.
- **FR-031**: `EstimatedProfitInLocal` = `TotalAgreedPriceInLocal - (TotalOfTheOrder * ExchangeRate)`.

### Key Entities

- **Order**: Representa una orden de compra. Campos editables en Pending: `ExchangeRate`, `TaxPercentage`, `ShippingAmountIntern`, `DiscountAmount`. Campos calculados: `TotalAgreedPriceInLocal`, `ShippingAmountToCR`, `TotalWithoutTaxes`, `TaxesAmount`, `TotalToPayToSupplier`, `TotalOfTheOrder`, `EstimatedProfitInLocal`. Campo eliminado: `RequestedAt`.
- **OrderItem**: Representa un ítem de una orden. Nuevos campos: `ProductLink`, `ProductSourceCode`, `ProductImage`. Columna renombrada: `ListPriceTax` → `ListPriceTaxWithTax`. Campo computado (no persiste): `Total`.
- **ProductType**: Entidad existente. Provee los valores por defecto de `ServiceFeeInLocal` y `EstimateShipping` al seleccionar en el modal de ítem. Tiene un campo de moneda que determina el filtro del dropdown.

---

## Error Handling

- Al fallar el guardado de una orden o un ítem por error del servidor, el sistema DEBE mostrar un mensaje de error amigable al usuario sin exponer detalles técnicos ni stack traces.
- Al fallar una migración de base de datos, los datos existentes NO DEBEN modificarse (la migración debe ser reversible o transaccional).
- Si el dropdown de `CustomerId` no puede cargar resultados de búsqueda, DEBE mostrar un mensaje indicando que no hay resultados disponibles sin romper el resto del formulario.
- Si el dropdown de `ProductType` no tiene opciones válidas para la moneda de la orden, DEBE mostrar un mensaje explicativo ("No hay tipos de producto disponibles para esta moneda") en lugar de quedar vacío sin contexto.
- Al intentar guardar un ítem con una imagen mayor a 2 MB, el sistema DEBE mostrar el error ANTES de enviar el formulario (validación en frontend) y también rechazarlo en el backend con un mensaje claro.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El operador puede agregar un ítem completo (con todos los campos) en menos de 2 minutos usando el modal dedicado.
- **SC-002**: Los campos calculados del ítem (`ListPriceTaxWithTax`, `Total`, `AgreedPriceInLocal`) se actualizan en menos de 500 ms tras cada cambio de entrada.
- **SC-003**: Los totales de la orden se recalculan en menos de 2 segundos tras agregar, editar o eliminar un ítem.
- **SC-004**: Las migraciones de base de datos se ejecutan sin pérdida de datos existentes (100% de los registros previos preservados).
- **SC-005**: El operador puede editar los campos de la orden (ExchangeRate, TaxPercentage) directamente en la pantalla sin abrir ningún diálogo adicional.
- **SC-006**: Las imágenes de producto se guardan y previsualización funciona correctamente para imágenes de hasta 2 MB.
- **SC-007**: El dropdown de ProductType solo muestra opciones válidas para la moneda de la orden (0 opciones inválidas visibles).

---

## Assumptions

- La pantalla de detalle de la orden es `Orders/Items.cshtml` (página existente) — no se crea una nueva ruta ni página.
- No se requieren cambios en el esquema de autenticación o autorización.
- El campo `Total` en OrderItem es computado (no persiste en la BD) — se calcula en el modelo C# y en el frontend JS en tiempo real.
- La moneda de la orden se determina por el campo de moneda por defecto en la configuración de la organización activa. El usuario puede cambiarla mientras el estado de la orden sea Pending **o** mientras no se hayan agregado ítems; una vez que existan ítems y la orden ya no sea Pending, la moneda es solo lectura.
- Las imágenes se almacenan directamente en la base de datos como datos binarios; no se usa almacenamiento externo en esta fase.
- El combobox de `CustomerId` con búsqueda tipo-as-you-type se implementa usando el componente de búsqueda interactiva ya presente en el proyecto.
- No se requieren pruebas automatizadas (fuera de alcance según plan de Mejoras Fase 1).
- La UI está en español; el código en inglés (consistente con el resto del proyecto).
