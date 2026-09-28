---
name: mejoras-saldos-ordenes-talla
description: Total neto en Saldos de Clientes (Payments), filtro de estados con checkboxes en Órdenes y campo Talla en OrderItem.
metadata:
  type: project
---

# Brainstorm: Total de Saldos, Filtro de Estados en Órdenes y Talla en Ítems

**Date:** 2026-09-28
**Status:** spec-created
**Spec:** specs/011-mejoras-saldos-ordenes-talla

## Problem Framing

Tres mejoras pequeñas e independientes de usabilidad:

1. **Payments – Saldos de Clientes:** la tabla lista el saldo de cada cliente pero no muestra cuánto suman. No hay forma rápida de ver el saldo neto total (o el de un subconjunto filtrado).
2. **Órdenes – filtro por estado:** el combo `statusFilter` solo ofrece grupos fijos ("Pendientes y Activas", "En Entrega", "Completadas", "Anuladas", "Todas"). No permite combinaciones libres (p. ej. Pendiente + Entregada). Además, al entrar a los ítems de una orden y regresar con "Volver", se pierde el filtro elegido.
3. **OrderItem – talla:** no existe dónde registrar la talla de la prenda; hoy se mezcla en la descripción.

## Approaches Considered

Las decisiones de comportamiento se resolvieron por preguntas directas. La única decisión con alternativas reales fue cómo recordar los estados marcados al usar "Volver".

### A: Estados en la URL (elegido)
Órdenes abre ítems con `/Orders/Items?orderId=…&statuses=Pending,Delivered`; "Volver" regresa a `/Orders?statuses=Pending,Delivered`. Sin parámetro (menú) se usan los estados por defecto.
- Pros: simple, sin estado oculto, funciona con varias pestañas, es explícito.
- Cons: hay que propagar el parámetro a través de la pantalla de ítems.

### B: sessionStorage + marca de "volver"
Órdenes guarda la selección en `sessionStorage`; "Volver" deja una marca que Órdenes consume para restaurarla.
- Pros: la pantalla de ítems casi no cambia.
- Cons: más frágil; estado oculto compartido entre pestañas.

### C: Botón "atrás" del navegador
- Cons: poco confiable porque la lista se carga por AJAX. Descartado.

## Decision

Se implementan las tres mejoras en un solo feature. Para recordar el filtro al usar "Volver" se usa el **enfoque A (estados en la URL)**.

## Key Requirements

### 1. Total en Saldos de Clientes (pantalla Payments)
- Fila **"Total"** al pie de la tabla "Saldos de Clientes".
- Muestra **solo el saldo neto**: suma de todos los saldos, donde los saldos a favor (negativos) restan.
- El total refleja **solo los clientes visibles** según el filtro de texto "Filtrar por cliente..." y se recalcula al escribir.
- Formato de moneda consistente con el resto de la tabla (signo de moneda local; negativo mostrado igual que en las filas).
- Se actualiza al registrar un pago (cuando la tabla se recarga).

### 2. Filtro de estados en Órdenes
- Se **elimina** el combo `statusFilter` actual.
- Se agrega una **lista de checkboxes** con los 6 estados de `OrderStatus`: Pendiente, Activa, Entregando, Entregada, Completada, Anulada (etiquetas en español).
- Por defecto (entrada desde el menú / sin parámetro) solo quedan marcados **Pendiente** y **Activa**.
- La lista de órdenes se **recarga automáticamente** al marcar o desmarcar cualquier estado.
- Si **no hay ningún estado marcado**, la lista queda **vacía** (no equivale a "todas").
- **Recordar selección solo vía "Volver":** el enlace a los ítems de una orden lleva los estados marcados; el botón "Volver" de la pantalla de ítems regresa a Órdenes con esos mismos estados marcados. Entrar a Órdenes por cualquier otra vía usa los estados por defecto.

### 3. Talla en OrderItem
- Nuevo campo `Size` en `OrderItem`: string **no nullable**, longitud máxima **20** caracteres.
- **Opcional** para el usuario: se permite texto vacío. Los ítems existentes quedan con valor vacío (`""`) tras la migración.
- Campo de texto libre en el formulario de **crear y editar** ítem (`Pages/Orders/Items`).
- **No** se muestra en la tabla de ítems ni en otras pantallas (solo en el formulario).
- Futuro (fuera de alcance): catálogo de tallas relacionado al tipo de prenda (`ProductType`).

### Fuera de alcance
- Totales separados de "por cobrar" y "a favor" en Payments.
- Persistir la selección de estados entre sesiones o al entrar desde el menú.
- Catálogo de tallas o validación de valores de talla.
- Mostrar la talla en la tabla de ítems, reportes u otras pantallas.

## Open Questions
_(todas resueltas en la spec 011)_
- Estados inválidos en la URL: se ignoran; si no queda ninguno válido, se usan los estados por defecto.
- `?statuses=` vacío explícito: se respeta como "ninguno marcado" (lista vacía).
- La talla se recorta (trim) al guardar.
- La reasignación (feature 010) conserva la talla; no hay otros puntos de creación de ítems.

## Cierre (2026-09-28)
- Implementado en `specs/011-mejoras-saldos-ordenes-talla` (merge a develop; release en PR #10 develop → main).
- Cambio posterior a pedido del usuario: la talla por defecto es **"N/A"** (no vacío); vacío o solo espacios se guarda como "N/A".
- Regla adicional fuera de este brainstorm (PR #9): una orden **Completada ya no se puede anular**.
- Scripts manuales para completar la talla de ítems existentes: `backfill-talla.sql` (sufijo " - TALLA") y `backfill-talla-2.sql` (frases "cambio a X" y "Talla X" sin separador).
- Despliegue: requiere aplicar la migración `AddOrderItemSize` en la base de Azure antes del merge a main.
