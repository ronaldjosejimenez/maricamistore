---
name: cierre-mes-cxp
description: Nuevas reglas del cierre de mes de CxP (saldo anterior = pendiente de recoger, En Cuenta heredado), diálogo de vista previa antes de cerrar, validaciones de tipo de cambio y montos, protección contra doble cierre y ajustes visuales del panel.
metadata:
  type: project
---

# Brainstorm: Cierre de Mes de CxP — Reglas, Vista Previa y Protecciones

**Date:** 2026-09-28
**Status:** spec-created
**Spec:** specs/014-cierre-mes-cxp

## Problem Framing

Hoy el cierre de mes (requerimiento 009) hace esto:

- Cierra el mes actual y crea el siguiente.
- El mes nuevo arranca con Tipo de Cambio tomado de la Configuración, Pagos Realizados = 0 y **En Cuenta = 0**.
- En el mes nuevo se crea la entrada **"Saldo anterior"**, en colones, con la **Deuda a Pagar** del mes cerrado.

Problemas:

1. **El dinero en cuenta se pierde.** No se asume que la deuda se paga con lo que hay En Cuenta: el Saldo anterior arrastra toda la deuda y En Cuenta vuelve a 0.
2. **El cierre es inmediato.** Al confirmar se escribe en la base sin mostrar cómo quedará el mes nuevo.
3. **Tipo de cambio en 0.** Se puede guardar TC = 0. Con TC 0, la deuda en otras monedas se convierte en 0 y desaparece del Saldo anterior al cerrar.
4. **Doble cierre.** El servidor cierra "el mes abierto que encuentre". Si se confirma dos veces rápido, la segunda petición puede cerrar también el mes recién creado.
5. **Visual.** Falta ver "En Cuenta" en el panel junto a Deuda y Pendiente. Los colores rojo y amarillo actuales son muy fuertes.

## Approaches Considered

La decisión técnica principal fue cómo proteger contra el doble cierre.

### A: Tres capas — elegido
1. El botón Confirmar se deshabilita al primer clic.
2. La petición lleva el **ID del mes que se cierra**. El servidor cierra solo ese mes y, si ya está cerrado, lo rechaza.
3. Una regla en la base de datos: **un solo mes abierto por organización** (índice único filtrado).
- Pros: cubre también peticiones simultáneas, varias pestañas y reintentos.
- Cons: requiere una migración pequeña.

### B: Botón + servidor (capas 1 y 2)
- Pros: no requiere migración.
- Cons: dos peticiones exactamente simultáneas podrían crear dos meses abiertos.

### C: Solo el botón
- Cons: no protege contra otra pestaña ni contra reintentos.

## Decision

**Enfoque A.** El cierre pasa a tener un **diálogo de vista previa** con dos columnas: el mes que cierra y el mes nuevo. En el mes nuevo se pueden editar el TC y En Cuenta. Hay reglas nuevas para el Saldo anterior y En Cuenta, validaciones de TC > 0 y de montos no negativos, y cambios visuales en el panel.

## Key Requirements

### 1. Reglas del cierre (reemplazan las del requerimiento 009)
- Las fórmulas de los indicadores **no cambian**:
  - Deuda a Pagar = Total por pagar colonizado − Pagos Realizados
  - Pendiente de Recoger = Deuda a Pagar − En Cuenta
- Al cerrar se **asume que la deuda del mes se paga con lo que hay En Cuenta**, hasta donde alcance.
- **Saldo anterior** del mes nuevo = **Pendiente de Recoger** del mes cerrado, **solo si es mayor que 0**. Si es 0 o negativo, **no se crea** la entrada Saldo anterior. Sigue siendo en colones y con referencia "Saldo anterior".
- **En Cuenta** del mes nuevo = lo que queda después de pagar la deuda = **máx(0, En Cuenta − Deuda a Pagar)**. Si la Deuda a Pagar es 0 o negativa, no hay nada que pagar y En Cuenta pasa **igual**; un sobrepago no suma dinero.
- **Pagos Realizados** del mes nuevo = 0 (sin cambio).
- **Tipo de Cambio** del mes nuevo: se propone el de la Configuración (sin cambio) y se puede ajustar en el diálogo.

### 2. Diálogo de vista previa (antes de escribir en la base)
- Al presionar "Cerrar Mes" **no se modifica nada**. Se abre un diálogo con los valores calculados.
- **Columna "Mes que cierra" ({mes/año})**: valores finales de los indicadores y de los campos editables, en solo lectura:
  - Total por pagar colonizado
  - Por pagar en {Moneda}
  - Saldos por Cobrar a Clientes
  - Shipping CR Pendientes
  - Deuda a Pagar
  - En Cuenta
  - Pendiente de Recoger
  - Posición
  - Tipo de Cambio y Pagos Realizados
- **Columna "Mes nuevo" ({mes/año siguiente})**:
  - **Campos editables**: Tipo de Cambio (propuesto desde la Configuración, editable, > 0), Pagos Realizados = 0 (solo lectura) y En Cuenta (propuesto con la regla, editable, ≥ 0).
  - **Entradas** con las que arranca: Saldo anterior con su monto, o el texto "Sin saldo anterior".
  - **Indicadores** con los que arranca, recalculados en el diálogo al cambiar el TC o En Cuenta.
- **Confirmar cierre** escribe todo en una sola operación. **Cancelar** no cambia nada.
- El servidor recalcula el cierre al confirmar; no confía en los números del navegador. Del diálogo solo toma el TC y En Cuenta editados, que valida.
- Si los datos cambiaron entre abrir el diálogo y confirmar (por ejemplo, otra persona agregó una entrada), se cierra con los valores **actuales** del servidor.

### 3. Validaciones
- **Tipo de Cambio > 0** siempre: al guardar los campos del mes, al inicializar el primer mes (ya lo hace) y en el diálogo de cierre. Se valida en el navegador y en el servidor.
- **Sin valores negativos** en Tipo de Cambio, Pagos Realizados (₡) y En Cuenta (₡). El servidor ya los rechaza al guardar; se agrega la validación en el navegador (`min`) y en el diálogo de cierre.
- Mensajes claros, por ejemplo "El tipo de cambio debe ser mayor a cero." y "El valor no puede ser negativo."

### 4. Protección contra doble cierre (enfoque A)
- El botón Confirmar del diálogo se deshabilita mientras se procesa.
- La petición de cierre lleva el ID del mes mostrado. Si ya no es el mes abierto, se rechaza con "Este mes ya fue cerrado." y se recarga la pantalla.
- En la base de datos: **un solo mes abierto por organización** (índice único filtrado sobre `IsClosed = 0`). La migración se aplica a mano antes de desplegar.

### 5. Ajustes visuales del panel CxP
- **Columna 1**, en este orden:
  - "Deuda a Pagar": **rojo pastel**.
  - **Nuevo** recuadro "En Cuenta (₡)", con el mismo valor del campo editable: **verde pastel**.
  - "Pendiente de Recoger": **amarillo pastel**.
- Los tonos pastel conservan buen contraste: texto oscuro sobre fondo claro, ícono y número en negrita.
- El nuevo recuadro En Cuenta se actualiza junto con los demás indicadores, por ejemplo al guardar los campos.

### Fuera de alcance
- Cambiar las fórmulas de los indicadores.
- Reabrir meses cerrados o consultar meses anteriores.
- Validar la fecha de cierre (se puede cerrar en cualquier momento).
- Cambios en paquetes de envío (requerimiento 013).
- Autenticación (pendiente aparte).

## Open Questions
_(todas resueltas en la spec 014)_
- Verificación de meses abiertos: consulta documentada en `specs/014-cierre-mes-cxp/quickstart.md`; se ejecuta antes de aplicar la migración.
- Vista previa: calculada en el servidor (endpoint `ClosePreview`) con las mismas fórmulas del panel; el servidor recalcula al confirmar.
- TC propuesto si la Configuración tiene 0: el del mes que cierra; para confirmar se exige TC > 0.

## Cierre (2026-09-28)
- Implementado en `specs/014-cierre-mes-cxp`: PR #25 (→ develop) y release a `main`.
- Aclaraciones del usuario durante la spec:
  - Nunca se paga de más: al cerrar, la deuda se paga con lo que hay En Cuenta; lo que sobra queda En Cuenta y lo que no alcanza es el Saldo anterior. Una deuda negativa se trata como 0.
  - El Saldo anterior es de solo lectura en el diálogo; solo el TC y En Cuenta del mes nuevo son editables.
- Correcciones de la revisión de código:
  - El cierre por ID respeta el filtro de organización.
  - Se cancela la vista previa vieja al editar.
  - Se escapa el signo de moneda en el HTML.
- La migración `OneOpenPeriodPerOrganization` (índice único filtrado) se aplica manualmente. El código no depende de ella para funcionar; la migración agrega la protección en la base contra cierres simultáneos.
- Pendiente anotado: `GetPeriodIndicatorsAsync` y `UpdatePeriodFieldsAsync` (código del requerimiento 009) todavía usan `FindAsync`, que se salta el filtro de organización. Revisarlo junto con la autenticación.
