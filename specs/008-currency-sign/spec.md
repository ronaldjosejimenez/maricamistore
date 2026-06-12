# Feature Specification: Signo de Moneda + Formateo de Montos

**Feature Branch**: `008-currency-sign`

**Created**: 2026-06-09

**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Ver montos con signo de moneda en todas las pantallas (Priority: P1)

Como usuario de la aplicación, quiero ver los montos acompañados del signo de la moneda correspondiente para entender de un vistazo en qué moneda está expresado cada valor.

**Why this priority**: Es el objetivo central del feature. Sin esta capacidad, todos los montos son números desnudos sin contexto de moneda, lo que genera errores de interpretación.

**Independent Test**: Navegar a Órdenes/Index y verificar que los totales muestren `$ 5,000.00` o `₡ 45,000.00` en lugar de `5000.00`.

**Acceptance Scenarios**:

1. **Given** una orden con total en colones, **When** el usuario visita Órdenes, **Then** el total aparece como `₡ 45,000.00` (signo + espacio + número con 2 decimales).
2. **Given** una orden con total en dólares, **When** el usuario visita Órdenes, **Then** el total aparece como `$ 5,000.00`.
3. **Given** una moneda sin signo configurado, **When** el sistema muestra un monto, **Then** muestra solo el número con formato numérico (sin signo).
4. **Given** el usuario visita Pagos/Index, **When** ve los saldos globales y la tabla de clientes, **Then** todos los montos muestran el signo de la moneda local.
5. **Given** el usuario visita Transacciones, **When** ve la columna Monto, **Then** los montos muestran el signo de la moneda local.

---

### User Story 2 — Gestionar el signo de moneda en el catálogo de Monedas (Priority: P2)

Como administrador, quiero poder ver y editar el signo asociado a cada moneda en el catálogo, para mantener actualizada la presentación de montos en toda la aplicación.

**Why this priority**: Sin esta pantalla de gestión, los signos de moneda no pueden corregirse ni agregarse para monedas futuras.

**Independent Test**: Navegar a Configuración → Monedas y verificar que cada fila muestra el campo Signo y permite editarlo.

**Acceptance Scenarios**:

1. **Given** el usuario abre el catálogo de Monedas, **When** ve la lista, **Then** cada moneda muestra su código, nombre y signo (ej. `₡`, `$`).
2. **Given** el usuario edita una moneda, **When** cambia el signo y guarda, **Then** el nuevo signo se refleja inmediatamente en todas las pantallas que muestran montos de esa moneda.
3. **Given** el usuario deja el campo Signo vacío, **When** guarda, **Then** el sistema acepta el valor vacío y muestra los montos sin signo (solo número).

---

### User Story 3 — Signo de moneda persistido en base de datos con datos iniciales (Priority: P1)

Como operador de despliegue, quiero que las monedas existentes (Colones, Dólares) tengan sus signos (`₡`, `$`) cargados automáticamente en la base de datos, para que la aplicación muestre los signos correctos desde el primer despliegue sin configuración manual.

**Why this priority**: Sin datos iniciales, el feature no muestra ningún signo hasta que alguien los configura manualmente.

**Independent Test**: Ejecutar la migración y verificar que las filas de Colones y Dólares en la tabla `Currencies` tengan los signos `₡` y `$` respectivamente.

**Acceptance Scenarios**:

1. **Given** una base de datos vacía o con datos de seed, **When** se ejecuta la migración, **Then** las monedas existentes Colones y Dólares tienen los signos `₡` y `$`.
2. **Given** una migración ya aplicada, **When** se aplica nuevamente, **Then** no falla ni duplica datos.

---

### Edge Cases

- ¿Qué pasa si `Sign` es `null` o vacío? → Mostrar solo el número formateado, sin signo ni espacio adicional.
- ¿Qué pasa si una pantalla JS no recibe el signo desde el servidor? → Usar string vacío como fallback; el monto se muestra sin signo.
- ¿Qué pasa con signos multibyte (ej. `₡`)? → El campo almacena hasta 10 caracteres Unicode; se muestra tal cual.
- ¿Qué pasa si el monto es cero? → `$ 0.00` (el signo siempre se muestra cuando está configurado).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El catálogo de Monedas DEBE incluir un campo `Sign` de hasta 10 caracteres que represente el símbolo visual de la moneda (ej. `$`, `₡`).
- **FR-002**: Las monedas existentes Colones y Dólares DEBEN tener los signos `₡` y `$` respectivamente al ejecutar la migración de base de datos.
- **FR-003**: El formato de presentación de montos DEBE ser `{signo} {monto}` (signo, espacio, número con dos decimales y separadores de miles). En servidor (C#) el número se formatea con `N2` en la cultura `es-CR` (`1.000.000,00`). En cliente (JavaScript) se usa `toLocaleString('es-CR', {minimumFractionDigits:2, maximumFractionDigits:2})`. Cuando `Sign` está vacío, mostrar solo el número formateado sin signo ni espacio adicional.
- **FR-004**: La pantalla de catálogo de Monedas (`/Currencies/Index`) DEBE mostrar el campo `Sign` en la lista y permitir editarlo.
- **FR-005**: Las siguientes pantallas DEBEN mostrar montos en moneda **local** con el signo configurado para la moneda local de la organización:
  - Órdenes/Index: totales (TotalAgreedPriceInLocal, TotalOfTheOrder, EstimatedProfit)
  - Órdenes/Items header: TotalAgreedPriceInLocal, Subtotal, Impuestos, Envío CR, Total Proveedor, Total Orden, Ganancia
  - Órdenes/Items tabla columnas locales: ServiceFeeInLocal, AgreedPriceInLocal
  - Pagos/Index: Saldo Global, Saldo Org, tabla de saldos de clientes (incluye la fila de Total al pie de la tabla)
  - Transacciones/Index: columna Monto y fila de Total (cuando aplica filtro de tipo)
  - Tipos de Producto: ServiceFeeInLocal
- **FR-006**: Las siguientes pantallas DEBEN mostrar montos en **moneda de la orden/tipo** con el signo correspondiente a esa moneda:
  - Órdenes/Items tabla columnas de proveedor: ListPrice, ListPriceTaxWithTax, RealPrice, EstimateShipping
  - Tipos de Producto: EstimateShipping
- **FR-007**: El sistema DEBE inyectar el signo de la moneda local desde el servidor en cada página Razor que renderiza montos vía JavaScript. El mecanismo es un bloque `<script>` en el `.cshtml` que expone variables JS globales, siguiendo el patrón existente de `items.cshtml` (e.g., `var localCurrencySign = '@localCurrencySign';`). Para pantallas que muestran también montos en moneda de orden/proveedor, se debe inyectar adicionalmente `var orderCurrencySign = '@orderCurrencySign';`. Las páginas afectadas son: `Orders/Index`, `Orders/Items`, `Payments/Index`, `Transactions/Index`, `ProductTypes/Index`.
- **FR-008**: DEBE existir una función de formateo compartida en el JavaScript global (`/js/utilities.js` o un archivo dedicado incluido en `_Layout.cshtml`) con la firma `function formatMoney(amount, sign)` que reciba un número y un signo y devuelva el string formateado según FR-003.
- **FR-009**: El endpoint `OnGetLoadAsync` de la pantalla de Monedas (`?handler=Load`) DEBE incluir el campo `sign` en el objeto JSON de cada moneda retornada. El `CatalogService.GetCurrenciesAsync()` DEBE proyectar este campo.

### Key Entities

- **Currency**: Moneda del sistema. Atributos clave: Id, Name, Abbreviation (campo existente), **Sign** (nuevo — símbolo visual, hasta 10 chars, puede estar vacío).
  - Nota: el modelo existente usa `Abbreviation`, no `Code`. Los GUIDs de seed existentes son: Colones = `63B4D953-66D5-409E-929D-6036111FB710`, Dólares = `63B4D953-66D5-409E-929D-6036111FB711`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los montos visibles en las pantallas afectadas (listadas en FR-005 y FR-006) muestran el signo de la moneda correspondiente cuando `Sign` está configurado.
- **SC-002**: Cuando `Sign` está vacío, el 100% de los montos afectados se muestran sin signo ni espacio extra.
- **SC-003**: Un administrador puede ver y editar el signo de cualquier moneda en el catálogo sin pasos adicionales fuera de la pantalla de Monedas.
- **SC-004**: La migración de base de datos se aplica sin errores y los registros de Colones y Dólares tienen los signos correctos.

## Assumptions

- La organización tiene configurada una moneda local (`LocalCurrencyId`) en su configuración; si no está configurada, se usa string vacío como signo.
- La moneda de una orden (`CurrencyId`) siempre existe en el catálogo de monedas; si por alguna razón no existe, se usa string vacío como signo.
- El formato de miles usa separadores estándar de español de Costa Rica (`1.000.000,00` → `toLocaleString('es-CR', {minimumFractionDigits:2, maximumFractionDigits:2})`). En servidor (C#) se usará la cultura `es-CR` con `N2`.
- Los campos de monto que ya existen en la base de datos no se modifican; solo cambia la presentación visual.
- El seed data de signos se aplica mediante la migración (no como datos de prueba separados).
- Las pantallas de Calculadora y otras no listadas en FR-005/FR-006 están fuera del alcance de este feature.

## Out of Scope

- Pantalla de Calculadora (`/Calculator/Index`).
- Formularios de entrada de montos (los inputs de captura permanecen como números crudos — solo la presentación/lectura cambia).
- Internacionalización/localización completa de la aplicación (i18n): este feature solo agrega el signo visual de moneda, no implementa un sistema de i18n.
- Cambio en la lógica de cálculo de montos, tipo de cambio, o impuestos.
- Múltiples organizaciones con monedas locales distintas: el feature asume una sola moneda local por organización (la configurada en `LocalCurrencyId`).
- Pantallas de Suppliers, Customers, Organizations, Configurations: ninguna de estas muestra montos monetarios.
