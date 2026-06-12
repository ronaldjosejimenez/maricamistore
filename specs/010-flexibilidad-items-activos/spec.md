# Feature Specification: Flexibilidad de Ítems en Órdenes Activas

**Feature Branch**: `010-flexibilidad-items-activos`

**Created**: 2026-06-11

**Status**: Draft

**Input**: Permitir reasignación de cliente y ajuste de precio acordado en ítems de órdenes ya activadas, con cliente genérico "Sin Cliente" para inventario especulativo, y visibilidad de saldos negativos en la pantalla de pagos.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Crear ítem con inventario especulativo (sin cliente) (Priority: P1)

Al agregar ítems a una orden en estado Pendiente (o en cualquier estado editable), el usuario puede asignar el cliente genérico "Sin Cliente" cuando aún no hay comprador real. Al activarse la orden, ese ítem genera un cargo a "Sin Cliente" visible en la pantalla de Saldos como "potencial por cobrar".

**Why this priority**: Es el punto de partida del flujo especulativo. Sin esta historia, los escenarios de reasignación posteriores no tienen origen. Es el requisito más frecuente del negocio (comprar para vender).

**Independent Test**: Puede verificarse creando una orden con ítems asignados a "Sin Cliente", activándola, y confirmando que "Sin Cliente" aparece con saldo pendiente en la pantalla de Saldos.

**Acceptance Scenarios**:

1. **Given** que el usuario abre el modal de agregar/editar ítem en una orden, **When** despliega la lista de clientes, **Then** "Sin Cliente" aparece como opción seleccionable al inicio o con una etiqueta distintiva.
2. **Given** que el usuario asignó "Sin Cliente" a uno o más ítems y la orden se activa, **When** el usuario navega a la pantalla de Saldos (Payments/Index), **Then** "Sin Cliente" aparece en la tabla de saldos con una etiqueta "(Especulativo)" y su saldo refleja la suma de los `AgreedPriceInLocal` de esos ítems.
3. **Given** que "Sin Cliente" tiene saldo especulativo, **When** el usuario intenta registrar un pago para "Sin Cliente" desde el formulario de pagos, **Then** "Sin Cliente" no aparece en el dropdown de clientes del formulario de pago (no se puede abonar a "Sin Cliente").

---

### User Story 2 - Reasignar ítem a otro cliente (Priority: P2)

En una orden activa (o en estados Delivering, Delivered, Completed), el usuario puede reasignar un ítem de un cliente a otro, ya sea porque el cliente original rechazó el producto o porque se encontró un comprador mejor. El precio acordado puede o no cambiar.

**Why this priority**: Es el escenario de negocio más crítico: un cliente rechaza un producto que ya llegó o ya tiene abonos. Sin esta historia no hay forma de corregir la asignación financieramente.

**Independent Test**: Puede verificarse tomando un ítem activo de Cliente A (con cargo existente), reasignándolo a Cliente B a nuevo precio, y confirmando que el saldo de A se corrige (cargo original anulado) y B acumula el nuevo cargo.

**Acceptance Scenarios**:

1. **Given** que la orden está en estado Active, Delivering, Delivered o Completed, **When** el usuario hace clic en el botón "Reasignar" de un ítem, **Then** se abre un modal con el cliente actual y el precio acordado actual precargados.
2. **Given** que el modal de reasignación está abierto, **When** el usuario selecciona un cliente diferente y confirma (con o sin cambio de precio), **Then** el sistema crea una anulación (Void) del cargo original al cliente anterior y un nuevo cargo (Charge) al cliente nuevo por el precio ingresado, y cierra el modal actualizando la vista.
3. **Given** que se realizó una reasignación de Cliente A a Cliente B, **When** el usuario revisa la pantalla de Saldos, **Then** el saldo de Cliente A se redujo por el monto del cargo anulado (puede quedar negativo si había abonos), y Cliente B acumula el nuevo cargo.
4. **Given** que la orden está en estado Pending o Voided, **When** el usuario visualiza los ítems de la orden, **Then** el botón "Reasignar" no aparece (no aplica en esos estados).

---

### User Story 3 - Ajustar precio acordado al mismo cliente (Priority: P3)

En una orden no-Pending, el usuario puede ajustar el precio acordado (`AgreedPriceInLocal`) de un ítem sin cambiar el cliente. Un aumento crea un cargo adicional; una reducción (descuento) crea un abono a favor del cliente.

**Why this priority**: Cubre el caso de retención de cliente mediante descuento y el de corrección de precio pactado hacia arriba. Depende del modal de US2 (reutiliza la misma UI).

**Independent Test**: Puede verificarse abriendo el modal "Reasignar" con el mismo cliente, subiendo el precio y confirmando que aparece un nuevo Charge por la diferencia, luego bajando el precio y confirmando un nuevo Payment por la diferencia.

**Acceptance Scenarios**:

1. **Given** que el modal de reasignación está abierto con el mismo cliente, **When** el usuario aumenta el precio acordado y confirma, **Then** se crea un cargo (Charge) por la diferencia con descripción que incluye "Ajuste de precio" y la descripción del producto.
2. **Given** que el modal de reasignación está abierto con el mismo cliente, **When** el usuario reduce el precio acordado y confirma, **Then** se crea un pago (Payment) por la diferencia con descripción que incluye "Descuento por ajuste de precio" y la descripción del producto.
3. **Given** que el usuario no cambia ni el cliente ni el precio en el modal, **When** hace clic en Confirmar, **Then** el sistema no genera ninguna transacción y no realiza ningún cambio (operación nula).

---

### User Story 4 - Ver saldos negativos (créditos a favor) en pantalla de Saldos (Priority: P4)

Después de una reasignación, el cliente original puede quedar con saldo negativo (crédito a favor) si había hecho abonos antes de ser reasignado. El usuario necesita ver esos saldos negativos para gestionarlos manualmente.

**Why this priority**: Consecuencia directa de las reasignaciones. Sin esta historia, los créditos a favor se ocultan y el usuario no puede gestionarlos.

**Independent Test**: Puede verificarse con un cliente que tenga Charge + Payment + Void que resulte en balance negativo, y confirmar que aparece en la tabla de Saldos con etiqueta "Crédito a favor".

**Acceptance Scenarios**:

1. **Given** que un cliente tiene balance negativo (crédito a favor), **When** el usuario navega a Payments/Index, **Then** ese cliente aparece en la tabla de saldos con el balance negativo y una etiqueta visual "Crédito a favor".
2. **Given** que hay clientes con balance positivo y clientes con balance negativo, **When** la tabla de saldos carga, **Then** ambos aparecen en una única tabla ordenada por nombre de cliente ASC — positivos etiquetados "Saldo pendiente", negativos etiquetados "Crédito a favor", diferenciados por color o badge; no se separan en dos secciones distintas.
3. **Given** que un cliente con crédito a favor recibe un nuevo cargo (en otra orden), **When** el saldo neto vuelve a ser positivo o cero, **Then** ya no aparece en la sección de "Crédito a favor".

---

### Edge Cases

- ¿Qué pasa si se intenta reasignar un ítem y el cargo original ya fue anulado (orden previamente en Voided y luego... imposible, Voided es terminal)? — No aplica: la reasignación solo está disponible en estados que garantizan que el cargo fue creado (Active en adelante excepto Voided).
- ¿Qué pasa si el usuario selecciona el mismo cliente Y el mismo precio en el modal de reasignación? → No se crea ninguna transacción ni se realiza ningún cambio.
- ¿Qué pasa si el nuevo precio es 0? → El sistema debe mostrar una advertencia de confirmación antes de proceder (cargo de ₡0 a nuevo cliente).
- ¿Qué pasa si al recalcular `TotalAgreedPriceInLocal` el valor baja a 0? → Se guarda normalmente. `EstimatedProfitInLocal` puede quedar negativo; esto es correcto y esperado.
- ¿Qué pasa si "Sin Cliente" recibe abonos accidentales (transacción manual)? → El formulario de pagos no permite abonar a "Sin Cliente"; los abonos manuales en la pantalla de Transacciones sí podrían hacerlo — se asume que el usuario sabe lo que hace en ese contexto.

---

## Requirements *(mandatory)*

### Functional Requirements

**Cliente genérico "Sin Cliente":**
- **FR-001**: El sistema DEBE tener un cliente genérico llamado "Sin Cliente" marcado como `IsGeneric = true`, creado automáticamente por seed si no existe.
- **FR-002**: "Sin Cliente" DEBE aparecer como opción seleccionable en el dropdown de cliente del modal de agregar/editar ítem (en cualquier estado de la orden) y en el modal de Reasignar, en orden alfabético normal junto con los demás clientes (posición "S").
- **FR-003**: "Sin Cliente" NO DEBE aparecer en el dropdown de cliente del formulario de registro de pago en Payments/Index.

**Operación de reasignación:**
- **FR-004**: El sistema DEBE exponer una operación `ReasignarItem` que reciba: `itemId`, `newCustomerId`, `newAgreedPriceInLocal`.
- **FR-005**: La operación `ReasignarItem` DEBE estar disponible únicamente cuando `Order.Status` ∈ {Active, Delivering, Delivered, Completed}. DEBE rechazarse en Pending y Voided.
- **FR-006**: Cuando el cliente cambia (nuevo cliente diferente al anterior), el sistema DEBE:
  a. Crear una transacción de tipo `Void` al cliente anterior por el monto original (`AgreedPriceInLocal` antes del cambio), con `SourceId = itemId` y descripción `"Reasignación – {NameOfOrder} – {ProductDescription}"` (ProductDescription truncado a 100 caracteres).
  b. Crear una transacción de tipo `Charge` al nuevo cliente por el nuevo precio, con `SourceId = itemId` y la misma plantilla de descripción `"Reasignación – {NameOfOrder} – {ProductDescription}"` (ProductDescription truncado a 100 caracteres).
  c. Actualizar `OrderItem.CustomerId` y `OrderItem.AgreedPriceInLocal`.
- **FR-007**: Cuando el cliente NO cambia pero el precio SUBE, el sistema DEBE crear una transacción de tipo `Charge` al mismo cliente por la diferencia (nuevo precio − precio anterior), con descripción "Ajuste de precio – {ProductDescription}".
- **FR-008**: Cuando el cliente NO cambia pero el precio BAJA, el sistema DEBE crear una transacción de tipo `Payment` al mismo cliente por la diferencia (precio anterior − nuevo precio), con descripción "Descuento por ajuste de precio – {ProductDescription}".
- **FR-009**: Cuando ni el cliente ni el precio cambian, la operación DEBE ser un no-op (ninguna transacción, ningún cambio).

**Recálculo de totales de orden:**
- **FR-010**: Después de cualquier reasignación que cambie `AgreedPriceInLocal`, el sistema DEBE recalcular y persistir `Order.TotalAgreedPriceInLocal` = SUM(`AgreedPriceInLocal`) de todos los ítems de esa orden.
- **FR-011**: El sistema DEBE recalcular `Order.EstimatedProfitInLocal` = `TotalAgreedPriceInLocal` − `TotalOfTheOrder` × `ExchangeRate` después de cada reasignación.
- **FR-012**: El sistema NO DEBE modificar `TotalToPayToSupplier`, `TotalOfTheOrder`, `TotalWithoutTaxes`, `TaxesAmount`, ni `ShippingAmountToCR` durante una reasignación (el costo al proveedor es invariable).

**UI en Orders/Items:**
- **FR-013**: En la página `Orders/Items`, cada fila de ítem DEBE mostrar un botón "Reasignar" cuando `Order.Status` ∈ {Active, Delivering, Delivered, Completed}.
- **FR-014**: Al hacer clic en "Reasignar", DEBE abrirse un modal con:
  - Dropdown de cliente (todos los clientes incluyendo "Sin Cliente") — precargado con el cliente actual.
  - Campo numérico "Precio Acordado" — precargado con el `AgreedPriceInLocal` actual.
  - Botón "Confirmar" y botón "Cancelar".
- **FR-015**: Si el nuevo precio es 0, el modal DEBE mostrar un mensaje de confirmación adicional antes de proceder.

**Pantalla de Saldos:**
- **FR-016**: La consulta de saldos DEBE devolver clientes con balance distinto de cero (`Balance ≠ 0`): tanto saldos positivos (deuda pendiente) como negativos (crédito a favor). Los clientes con balance exactamente igual a 0 NO deben aparecer en la tabla.
- **FR-017**: La tabla de saldos DEBE ser una única tabla ordenada por nombre de cliente ASC. Los clientes con balance negativo DEBEN mostrarse con etiqueta "Crédito a favor" y diferenciación cromática (color o badge) respecto a los saldos positivos. No se crean secciones separadas.
- **FR-018**: "Sin Cliente" DEBE mostrarse en la tabla de saldos con etiqueta "(Especulativo)" cuando su balance sea distinto de cero.

### Key Entities

- **Customer**: Entidad existente. Se agrega campo `IsGeneric bool` (default `false`). El registro "Sin Cliente" tiene `IsGeneric = true`.
- **OrderItem**: Entidad existente. El campo `CustomerId` acepta el Id de "Sin Cliente" (ya es Guid no-nullable, sin cambio de esquema).
- **Transaction**: Entidad existente. Reutilizada para crear Void y Charge/Payment durante reasignaciones. La descripción incluye el contexto del ajuste.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El usuario puede crear un ítem con "Sin Cliente" y ese ítem aparece en Saldos sin necesidad de pasos adicionales — 0 pasos intermedios requeridos.
- **SC-002**: Una reasignación de cliente completa (con ajuste de precio) genera las transacciones correctas y actualiza los totales de la orden en menos de 3 segundos desde la confirmación.
- **SC-003**: Después de una reasignación, el saldo del cliente anterior queda matemáticamente correcto: `SUM(Charge) − SUM(Payment) − SUM(Void)` sin discrepancias.
- **SC-004**: Al navegar a Payments/Index después de una reasignación, los datos de saldo reflejan los cambios correctamente sin necesidad de un refresh adicional — los saldos negativos aparecen como "Crédito a favor" en la misma carga de página.
- **SC-005**: El costo al proveedor (`TotalToPayToSupplier`) permanece inalterado después de cualquier reasignación o ajuste de precio — verificable comparando el valor antes y después de la operación.

---

## Clarifications

### Session 2026-06-11

- Q: ¿La tabla de saldos muestra positivos y negativos en una tabla única o en dos secciones separadas? → A: Tabla única ordenada por nombre ASC, diferenciación solo por color/badge (Opción B).
- Q: ¿Qué descripción llevan las transacciones Void y Charge generadas en un cambio de cliente (FR-006)? → A: `"Reasignación – {NameOfOrder} – {ProductDescription}"` (ProductDescription truncado a 100 caracteres); misma plantilla para Void y Charge.
- Q: ¿"Sin Cliente" aparece anclado al inicio del dropdown o en orden alfabético? → A: En orden alfabético normal entre los demás clientes (posición "S").

---

## Out of Scope

- Cambiar `ListPrice`, `RealPrice`, `ListPriceTaxWithTax`, o `EstimateShipping` de un ítem en estado Active o posterior (estos afectan el costo al proveedor).
- Revertir o ajustar las entradas `CxP AutoActiva` o `AutoDelivered` al reasignar ítems (el costo al proveedor no cambia).
- Historial visual de reasignaciones por ítem en la UI (el historial está disponible en la pantalla de Transacciones).
- Paginación o búsqueda server-side en la tabla de saldos ampliada con negativos.

---

## Assumptions

- "Sin Cliente" tiene un ID fijo en producción: `84828E82-81CA-437D-B2F0-B9877EF044C6`. El mecanismo de seed DEBE verificar si ese ID ya existe: si existe, solo actualiza el registro para asegurarse de que `IsGeneric = true`; si no existe, lo crea con ese ID específico. No se crea un segundo registro.
- Los cargos (Charge) originales de los ítems, creados al activar la orden, tienen `SourceId = item.Id`. La operación de reasignación usa `SourceId` para identificar el cargo original a anular.
- Los pagos (Payment) de un cliente no están vinculados a órdenes ni a ítems específicos — afectan el balance global del cliente. Esto es comportamiento existente y no cambia.
- `Order.TotalOfTheOrder` ya está persistido correctamente desde la activación y no se recalcula durante reasignaciones.
- La pantalla `Payments/Index` ya recarga la tabla de saldos via AJAX; la tabla ampliada con negativos utilizará el mismo mecanismo de carga existente.
- El modal de reasignación solo puede operar sobre un ítem a la vez (no hay reasignación masiva en esta versión).
