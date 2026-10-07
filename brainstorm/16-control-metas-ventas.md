---
name: control-metas-ventas
description: Control de metas de ventas por vendedor y mes, con detalle diario, catálogo de vendedores y creación de registros bajo demanda en zona horaria de Costa Rica.
metadata:
  type: project
---

# Brainstorm: Control de Metas de Ventas

**Date:** 2026-10-06
**Status:** implemented (specs/016-control-metas-ventas)

## Problem Framing

Se necesita llevar el control de las metas mensuales de ventas por vendedor, con seguimiento diario (meta vs. real) y semáforo de cumplimiento. Hoy una persona lo hace en Excel (`PPTO Octubre 2026`): meta mensual de ₡4 000 000 repartida en 31 días, columna de real y de cumplimiento.

Restricción de infraestructura: la app vive en un web container de Azure que se duerme y no se mantendrá activo por costos, así que los registros del mes **no pueden crearse con un job a las 00:00 del día 1**; se crean **bajo demanda** al consultar al vendedor.

### Análisis del Excel de ejemplo (octubre 2026)

- 31 filas (jue 1 → sáb 31), meta 4 000 000 (= valor por defecto en Configuration).
- Todos los montos diarios son múltiplos de ₡4 000 y suman exactamente la meta. Pesos (monto/4 000): 

| Semana | Jue | Vie | Sáb | Dom | Lun | Mar | Mié |
|---|---|---|---|---|---|---|---|
| 1 | 28 | 37 | 52 | 39 | 20 | 21 | 25 |
| 2 | 27 | 34 | 48 | 32 | 18 | 22 | 25 |
| 3 | 31 | 35 | 53 | 38 | 18 | 22 | 26 |
| 4 | 30 | 37 | 50 | 35 | 19 | 24 | 28 |
| 5 | 32 | 38 | 56 | – | – | – | – |

- Patrón: sábado el más alto, luego viernes/domingo/jueves; lunes el más bajo; martes y miércoles intermedios. Tendencia ligeramente creciente hacia el final del mes.
- Con un solo mes no se sabe si los pesos son fijos entre meses o dependen del mes en curso.
- El Excel tiene filas "Libres", "Vacaciones" y "Meta Final" (todas en 0), **fuera de alcance** por ahora.

## Approaches Considered

### A: Perfil fijo por día de la semana + tendencia semanal (elegida)
- Pros: simple, reproduce el patrón del ejemplo, se afina con datos de más meses, aislable en una clase propia.
- Cons: aproximación, no reproduce octubre exacto.

### B: Solo peso por día de la semana, sin tendencia
- Pros: la más simple.
- Cons: no reproduce el ejemplo (todos los sábados valdrían igual).

### C: Plantilla de 31 pesos por número de día del mes
- Pros: reproduce octubre exacto.
- Cons: se rompe en meses donde los días de la semana caen distinto.

## Decision

Enfoque **A**. El algoritmo de distribución queda **en duro en el código pero aislado** (clase/servicio propio, fácil de reemplazar) para afinarlo con más datos.

## Key Requirements

### Modelo de datos
1. **Vendedor (catálogo, nuevo):** campos tomados de Customer que aplican: `Name`, `NickName`, `PhoneNumber`, `Email`, más indicador Activo/Inactivo. Sin Address/LocationLink/IsGeneric. Global (como Customer, sin organización).
2. **Control de meta (maestro):** Id (guid), Vendedor, Mes, Año, Monto de la meta, Monto real, % de cumplimiento, Moneda, Organización.
3. **Control de meta diario (detalle):** Id (guid), FK al control, Día del mes, Monto Meta Propuesto, Monto Meta, Monto real, % de cumplimiento.
4. **Configuration:** nuevo campo "Meta mensual por defecto", default 4 000 000.

### Pantallas / menú
5. Configuración: agregar el campo "Meta mensual por defecto".
6. Ventas/Catálogos: mantenimiento de Vendedores.
7. Ventas: nuevo submenú **"Metas"** con dos opciones:
   - **Mes Actual** (pantalla principal).
   - **Histórico de metas**: consulta de solo lectura de meses anteriores por vendedor (meta, real, % cumplimiento) con acceso al detalle diario.

### Pantalla Mes Actual (3 secciones)
8. **Combo de vendedor:** preselecciona el primer vendedor; al elegir uno se cargan los datos del mes actual para la organización de la sesión; si no existen, se crean (ver abajo).
9. **Encabezado:** Moneda (combo editable), Mes y Año (no editables), Monto de la meta (editable), Monto real (calculado = suma de reales del detalle, resaltado, persistido), % de cumplimiento (calculado real/meta, resaltado, persistido), **% de cumplimiento al día de hoy** (calculado, no persistido, se refresca con cada cambio de real): suma de reales día 1..hoy ÷ suma de Monto Meta diario día 1..hoy; semáforo rojo < 70%, amarillo 70–90%, verde > 90%.
10. **Detalle (grid):** Día (número), día de la semana (nombre), fecha dd/mm/yyyy, Monto Meta Control (propuesto, no editable), Monto Meta (editable), Monto real (editable), % de cumplimiento (real vs. meta del día).
11. Al editar un monto real del detalle: se actualizan encabezado (monto real, % y % al día) en pantalla y base de datos.
12. **Cambio de Monto de la meta del encabezado (o moneda):** se recalcula el Monto Meta Propuesto con el algoritmo; el Monto Meta editable solo se sobrescribe en los días que el usuario no haya modificado manualmente; los montos reales no se tocan.

### Creación bajo demanda
13. Solo días naturales; **zona horaria de Costa Rica fija en el código** (no depende del cliente).
14. Al consultar un vendedor sin registro del mes actual: crear maestro (Id guid, vendedor, mes/año CR, meta = default de Configuration, real 0, % 0, moneda = local por defecto de Configuration, organización de la sesión) y un detalle por **cada día del mes** (sin importar en qué día del mes ocurra la creación), con Monto Meta Propuesto según algoritmo (suma = meta del encabezado), Monto Meta = propuesto, real 0, % 0.
15. Suma de los montos propuestos diarios debe igualar exactamente la meta (residuo de redondeo ajustado en un día).

### Fuera de alcance
- Filas Libres / Vacaciones / Meta Final del Excel.
- Job automático a las 00:00 del día 1.

## Open Questions
- Granularidad de redondeo del algoritmo (₡1 000 vs ₡4 000 vs proporcional a la meta) y valores exactos del perfil semanal/tendencia para la primera versión.
- Cálculo del % al día de hoy cuando la meta diaria acumulada es 0 (evitar división por cero).
- ¿Un vendedor puede tener metas en varias organizaciones el mismo mes (una por organización)? (se asume que sí: clave vendedor+mes+año+organización).
- ¿Qué hace el Histórico si no hay registros? ¿Se permite filtrar por vendedor/año?
- Pantallas con autenticación pendiente (ver memoria): sin autenticación en producción.

## Resolution (2026-10-07)
- Algoritmo: pesos por (día de la semana, n-ésima ocurrencia en el mes) copiados del Excel; octubre 2026 @ 4 000 000 reproduce el Excel exacto. Redondeo a ₡1 000, residuo al día de mayor peso. Pendiente: más meses de ejemplo para afinar (5.ª ocurrencia de dom/lun/mar/mié repite la 4.ª).
- % al día de hoy con meta diaria acumulada 0 → 0%.
- Metas del mismo vendedor en varias organizaciones: una por vendedor + mes + año + organización.
- Histórico: lista por vendedor (todas las organizaciones no; solo la de la sesión) con detalle de solo lectura.
- Añadido: franja de aviso en Mes Actual cuando las metas diarias no suman la meta del mes.
