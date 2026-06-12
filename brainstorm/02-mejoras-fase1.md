---
name: mejoras-fase1
description: Mejoras incrementales sobre Fase 1 — error handling, org default, CRUD de organizaciones, config por org, abreviación de moneda, UX del formulario de órdenes.
metadata:
  type: project
---

# Brainstorm: Mejoras Fase 1

**Date:** 2026-06-03
**Status:** active

## Problem Framing

La Fase 1 del sistema MariCamiStore está implementada y compilando, pero requiere un conjunto de mejoras incrementales identificadas en revisión:

1. Los servicios no tienen manejo de errores consistente — una excepción en un servicio devuelve un 500 genérico o un stack trace al cliente.
2. El usuario debe seleccionar manualmente la organización cada vez que inicia la app — no hay un default configurable.
3. En las vistas de catálogos (ProductTypes, Configuración), el campo de moneda muestra el GUID de la moneda en lugar de su abreviación legible (USD, CRC).
4. No existe un CRUD para Organizaciones — actualmente solo hay datos semilla y un selector.
5. La pantalla de Configuraciones no impide crear una segunda configuración para la misma organización.
6. El formulario de nueva orden no tiene el proveedor en primera posición y no sugiere automáticamente un nombre.

## Approaches Considered

### A: Un spec único "Mejoras Fase 1" (Elegido)
- Pros: Un solo pipeline (specify → plan → tasks → implement). Las mejoras son pequeñas y relacionadas. Error handling se implementa primero y queda disponible para las demás mejoras.
- Cons: Spec más largo. Una dependencia interna (error handling primero).

### B: Dos specs separados
- Pros: Más enfocados individualmente.
- Cons: Overhead innecesario para mejoras pequeñas. Dos pipelines completos.

### C: Error handling primero, luego el resto
- Pros: Base técnica sólida antes de funcionalidades.
- Cons: Justifica un spec separado solo para logging, que es excesivo.

## Decision

**Enfoque A: Un spec único**, con error handling como primera fase interna de implementación. Todas las mejoras compilan y se entregan juntas.

## Key Requirements

### Fase A — Error Handling & Logging
- Todos los métodos públicos de los servicios (`CatalogService`, `OrderService`, `PaymentService`) deben tener try/catch.
- Capturar excepción → `_logger.LogError(ex, "mensaje descriptivo con contexto")`.
- Retornar o lanzar un mensaje amigable al usuario (nunca stack trace ni mensaje técnico).
- Los handlers de Razor Pages que llaman servicios deben capturar excepciones y retornar `JsonResult({ success: false, error: "mensaje amigable" })`.

### Fase B — Organización por Defecto
- Agregar `DefaultOrganizationId` (Guid?) en `appsettings.json` y en `BaseSettings`.
- En `CurrentOrganizationService.OrganizationId`: si la sesión no tiene org activa y `DefaultOrganizationId` está configurado, cargar el default automáticamente (sin acción del usuario).
- La selección manual en el navbar siempre prevalece (escribe en sesión y sobreescribe el default).
- Si `DefaultOrganizationId` no está configurado, comportamiento actual sin cambios.

### Fase C — Abreviación de Moneda en Vistas
- En la vista `ProductTypes/Index`, el campo `currencyId` (actualmente muestra GUID) debe mostrar la abreviación de la moneda (USD, CRC, etc.).
- En la vista `Configurations/Index`, los campos `localCurrencyId` y `orderCurrencyIdDefault` deben mostrar la abreviación correspondiente.
- El backend debe retornar la abreviación junto con los datos del grid (JOIN con la tabla Currencies).

### Fase D — CRUD de Organizaciones
- Pantalla `/Organizations/Index` con jsGrid: Crear, Editar, Eliminar organizaciones.
- **Regla de eliminación**: Bloqueada si la organización tiene alguno de los siguientes registros relacionados: órdenes, transacciones o configuración. El servicio retorna un error amigable con el motivo del bloqueo.
- Los campos del CRUD son: Nombre.

### Fase E — Una Sola Configuración por Organización
- El servicio debe validar que no exista ya una configuración para la org activa antes de insertar una nueva.
- En la UI de jsGrid de Configuraciones: si ya existe una config para la org activa, ocultar la fila de inserción (no mostrar la fila de "Agregar").
- La carga del grid debe retornar un flag `hasConfig: true/false` para que el JS pueda controlar la visibilidad de inserción.

### Fase F — Formulario de Orden Mejorado
- En el modal de "Nueva Orden", el select de Proveedor va en la primera posición, sin ninguna opción seleccionada por defecto (placeholder "-- Seleccione proveedor --").
- Al seleccionar un proveedor, el campo "Nombre de la Orden" se auto-llena (y puede ser editado) con el formato: `{NombreProveedor}-DD-MM-YYYY` (fecha de hoy).
- El Tipo de Cambio y el Impuesto se cargan de la configuración de la org activa al abrir el modal (ya funciona en backend — confirmar en frontend).

## Open Questions

_(ninguna — todas las dudas resueltas en sesión del 2026-06-03)_
