# Design: Pantalla de Transacciones

**Spec**: 007 | **Date**: 2026-06-08 | **Source**: `requerimientos/Mejoras varias fase 2.txt` ítems 4, 4.1, 4.2

---

## Objetivo

Pantalla genérica para consultar transacciones con filtros y sumatoria, y registrar transacciones manuales desde un modal en la misma pantalla.

---

## Layout

```
┌──────────────────────────────────────────────────────────┐
│  Transacciones                    [Nueva Transacción]    │
│  Filtros: [Desde __] [Hasta __] [Cliente ▼] [Tipo ▼]    │
│  ────────────────────────────────────────────────────    │
│  Orden   │ Cliente │ Tipo  │ Descripción │  Monto │ Fecha │
│  OR-001  │ Ana G.  │ Cargo │ Cargo – ... │  5,000 │ 01/06 │
│  —       │ Ana G.  │ Pago  │ Pago – Ana  │  2,000 │ 03/06 │
│  ────────────────────────────────────────────────────    │
│                                    Total: ₡ 7,000.00    │
└──────────────────────────────────────────────────────────┘

[Modal: Nueva Transacción]
  Cliente:     [▼──────────────────]
  Tipo:        [Pago / Cargo / Anulación ▼]
  Monto:       [____________]
  Descripción: [____________]
  [Cancelar]  [Guardar]
```

---

## Columnas de la tabla

| Columna | Fuente |
|---------|--------|
| Orden | `SourceId != null` → nombre de la orden (join OrderItem → Order). Si null → "—" |
| Cliente | Nombre/Apodo del cliente |
| Tipo | `TransactionType` (Cargo, Pago, Anulación) |
| Descripción | `TransactionDescription` |
| Monto | `TransactionAmount` |
| Fecha | `TransactionDate` (formato dd/MM/yyyy) |

---

## Filtros

- **Desde / Hasta**: rango de `TransactionDate` (ambos opcionales)
- **Cliente**: dropdown con todos los clientes (opcional)
- **Tipo**: dropdown con Payment, Charge, Void (opcional)

Los filtros se aplican en el servidor (`?handler=Load`). El total se calcula sobre los resultados devueltos.

---

## Modal — Nueva Transacción

Campos requeridos: Cliente, Tipo, Monto (> 0).
Descripción: opcional (auto-generada si vacía: `"{Tipo} manual – {NombreCliente}"`).
`Source = Manual`, `SourceId = null`, `Status = Applied`.
Al guardar exitosamente: cierra modal y recarga la tabla con los filtros actuales.

---

## Cambios de Modelo

### `Transaction.cs`
- `SourceId` ya es `Guid?` en el modelo actual. ✓
- En `PaymentService.RegisterPaymentAsync`: cambiar `SourceId = Guid.Empty` → `SourceId = null` (parte de Spec 005).

### `TransactionSource`
- Agregar `Manual` a `List()` (actualmente está definido pero excluido de la lista).

---

## Backend

### `ITransactionService` (nuevo)
```csharp
Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
Task CreateManualTransactionAsync(ManualTransactionRequest request);
```

### `TransactionDto`
Campos: `Id`, `OrderName` (nullable), `CustomerName`, `TransactionType`, `TransactionDescription`, `TransactionAmount`, `TransactionDate`.

### `TransactionFilterDto`
Campos: `DateFrom?`, `DateTo?`, `CustomerId?`, `TransactionType?`.

### `ManualTransactionRequest`
Campos: `CustomerId`, `TransactionType`, `Amount`, `Description?`.

### `Pages/Transactions/Index.cshtml.cs`
- `OnGetAsync`: retorna Page
- `OnGetLoadAsync([FromQuery] TransactionFilterDto filter)`: retorna `JsonResult` con lista de `TransactionDto`
- `OnPostCreateManualAsync([FromBody] ManualTransactionRequest request)`: crea transacción manual

---

## Navegación

Agregar ítem "Transacciones" al menú en `_Layout.cshtml`.

---

## Notas

- Los filtros vacíos retornan todos los registros (sin límite de fecha por defecto)
- El total se muestra **solo cuando hay un Tipo de transacción seleccionado** en el filtro — mezclar Cargos + Pagos + Anulaciones en una suma no tiene significado. Cuando está visible, es la suma de `TransactionAmount` de los registros mostrados.
- La columna "Orden" requiere join: `Transaction.SourceId → OrderItem.Id → OrderItem.OrderId → Order.NameOfOrder`
