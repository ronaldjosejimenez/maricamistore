---
name: filtro-organizacion-pagos
description: Filtro de Organización ("Todas" + lista) en la pantalla Payments que determina el alcance de los saldos mostrados y la organización a la que se asocia un pago registrado.
metadata:
  type: project
---

# Brainstorm: Filtro de Organización en Payments

**Date:** 2026-09-29
**Status:** spec-created
**Spec:** specs/015-filtro-organizacion-pagos

## Problem Framing

La pantalla Payments (`Pages/Payments/Index`) hoy no distingue por organización de forma explícita para el usuario:

- **"Saldos de Clientes"** (`GetSaldosReportAsync`) siempre se calcula **ignorando** la organización (`IgnoreQueryFilters()`): es un saldo global entre todas las organizaciones.
- La tarjeta **"Saldo del Cliente"** ya muestra dos cifras — "Saldo Global" (todas las organizaciones) y "Saldo Esta Org." (la organización de la sesión, vía el filtro EF de `Transaction`) — pero "Esta Org." siempre es la organización activa de la sesión, sin poder elegir otra.
- **Registrar Pago** siempre crea la transacción con la organización de la sesión (`currentOrg.OrganizationId`).

El usuario necesita poder **elegir explícitamente una organización distinta** (no solo la de la sesión) para ver sus saldos y registrar pagos contra ella, o mantener la vista global de siempre. Hoy eso solo se puede lograr cambiando la organización activa de la sesión desde la pantalla Organizaciones, lo que afecta a toda la aplicación (Órdenes, CxP, etc.), no solo a Payments.

## Approaches Considered

### A: Filtro con parámetro explícito de organización — elegido
El combo de Payments envía un `organizationId` (o `null` para "Todas") a los handlers de la página. `PaymentService` gana overloads/parámetros que aceptan una organización explícita para saldos y balance, y `RegisterPaymentAsync` recibe la organización a usar. La organización de sesión (`ICurrentOrganizationService`) no cambia.
- Pros: cambio acotado a Payments; no afecta otras pantallas; el usuario puede consultar/pagar contra una organización sin "cambiarse" de organización activa.
- Cons: hay que pasar el parámetro de organización de forma explícita por varias capas (handler → servicio).

### B: Reemplazar temporalmente la organización de sesión
Al cambiar el filtro, se sobrescribe `ICurrentOrganizationService` (o la sesión) con la organización elegida mientras se está en Payments.
- Pros: reutiliza tal cual el filtro EF existente (`HasQueryFilter` por `currentOrganizationService.OrganizationId`) sin tocar los servicios.
- Cons: `ICurrentOrganizationService` es compartido por toda la app (Órdenes, CxP, Configuración...); cambiarlo de forma implícita mientras se navega Payments es frágil y con alto riesgo de efectos secundarios en otras pantallas o pestañas. Además "Registrar Pago" con "Todas" seleccionado necesita seguir usando la organización real de la sesión, lo que ya contradice sobrescribirla.

### C: Calcular todo en el cliente
Traer todas las transacciones y filtrar en JavaScript por organización.
- Cons: hoy el cálculo de saldos es agregado en SQL por cliente; traer datos crudos al navegador es más pesado y menos seguro. Descartado.

## Decision

**Enfoque A.** El filtro de organización es un dato que viaja explícito en las peticiones de Payments; la organización de sesión no se toca.

## Key Requirements

### 1. Combo de Organización
- Se agrega **arriba de la tarjeta "Registrar Pago"**.
- Opciones: **"Todas"** primero, luego las organizaciones devueltas por `GetOrganizationsAsync()` (todas las que existen hoy; el modelo `Organization` no tiene concepto de activo/inactivo y no se agrega uno en este requerimiento).
- Al cargar la pantalla, el combo tiene seleccionada por defecto la **organización de la sesión** (`ICurrentOrganizationService.OrganizationId`).
- Cambiar el combo recarga, sin refrescar la página completa: la tabla "Saldos de Clientes" y, si hay un cliente elegido, la tarjeta "Saldo del Cliente".

### 2. Con una organización específica seleccionada
- **Saldos de Clientes**: se calculan únicamente con transacciones de esa organización (no de la sesión necesariamente; el usuario puede elegir cualquier organización de la lista).
- **Saldo del Cliente**: "Saldo Esta Org." pasa a reflejar la organización elegida en el filtro (antes siempre era la de la sesión).
- **Registrar Pago**: la transacción creada queda asociada a la organización elegida en el filtro.

### 3. Con "Todas" seleccionado
- **Saldos de Clientes**: se calculan sin importar la organización (comportamiento global de hoy, sin cambios).
- **Saldo del Cliente**: "Saldo Esta Org." equivale al "Saldo Global" (misma cifra, ya que no hay una única organización elegida).
- **Registrar Pago está bloqueado**: el botón "Registrar Pago" queda deshabilitado mientras el filtro esté en "Todas", para obligar al usuario a elegir a conciencia la organización a la que se le va a registrar el pago. Junto al botón (o en el lugar de `#payment-error`) se muestra una leyenda explicando por qué está bloqueado, por ejemplo: **"Seleccione una organización específica para poder registrar un pago."** El resto del formulario (cliente, monto, tarjeta de saldo) sigue disponible con "Todas" seleccionado; solo se bloquea la acción de registrar.
- Si el usuario intentara forzar el registro sin pasar por el formulario (llamada directa al handler), el servidor también rechaza un `RegisterPayment` sin una organización específica.

### Fuera de alcance
- Cambiar la organización activa de la sesión (afecta otras pantallas) — el filtro es local a Payments.
- Agregar un campo de organizaciones activas/inactivas al modelo `Organization`.
- Cambios en la pantalla CxP, Órdenes u otras pantallas con lógica por organización.
- Persistir la selección del filtro entre visitas (siempre vuelve a la organización de la sesión al recargar la pantalla).
- Autenticación o permisos (pendiente aparte).

## Open Questions
_(todas resueltas en la spec 015)_
- Combo vacío: caso extremo no observado; no debería ocurrir porque la organización de la sesión ya existe.
- La tarjeta de saldo del cliente sí se recalcula automáticamente al cambiar el filtro (FR-009, Story 2 escenario 4).

## Cierre (2026-09-29)
- Implementado en `specs/015-filtro-organizacion-pagos`: PR #27 (→ develop) y release a `main`.
- Sin migraciones; sin cambios de esquema.
- Corrección del plan (revisión): `CxPService.GetSaldosReportAsync()` actualizado a `GetSaldosReportAsync(null)`, único otro llamador del método, para que el proyecto siguiera compilando.
- Corrección posterior al despliegue local (probada por el usuario): la leyenda "Seleccione una organización específica para poder registrar un pago." quedaba siempre visible porque la clase `d-block` de Bootstrap (`display: block !important;`) le ganaba al `display: none` en línea que ponía `jQuery.toggle()`. Se cambió el elemento de `<small class="... d-block ...">` a `<div class="... small ...">`, que no tiene ese conflicto.
