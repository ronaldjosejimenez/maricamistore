# Brainstorm: Pantalla de Transacciones

**Fecha**: 2026-06-08  
**Fuente**: `requerimientos/Mejoras varias fase 2.txt` ítems 4, 4.1, 4.2

---

## Problema

No existe una pantalla genérica para consultar el historial de transacciones. El usuario quiere poder:
1. Ver todas las transacciones con filtros (fecha, cliente, tipo)
2. Ver una sumatoria cuando filtra por un tipo específico de transacción
3. Registrar transacciones manuales desde un modal en la misma pantalla

---

## Layout propuesto

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
  Descripción: [____________]  (opcional)
  [Cancelar]  [Guardar]
```

---

## Columnas de la tabla

| Columna | Fuente |
|---------|--------|
| Orden | `SourceId != null` → nombre de la orden (join: Transaction.SourceId → OrderItem.Id → Order.NameOfOrder). Si null → "—" |
| Cliente | Nombre/Apodo del cliente |
| Tipo | `TransactionType` (Cargo, Pago, Anulación) |
| Descripción | `TransactionDescription` |
| Monto | `TransactionAmount` |
| Fecha | `TransactionDate` (dd/MM/yyyy) |

---

## Filtros

- **Desde / Hasta**: rango de `TransactionDate` (opcionales)
- **Cliente**: dropdown con todos los clientes (opcional)
- **Tipo**: dropdown con Payment, Charge, Void (opcional)

Los filtros se aplican en el servidor (`?handler=Load`).

**El total se muestra SOLO cuando hay un Tipo de transacción seleccionado** en el filtro — mezclar Cargos + Pagos + Anulaciones en una suma no tiene sentido semántico.

---

## Modal — Nueva Transacción

- Campos requeridos: Cliente, Tipo, Monto (> 0)
- Descripción: opcional. Si vacía, auto-generar: `"{Tipo} manual – {NombreCliente}"`
- `Source = Manual`, `SourceId = null`, `Status = Applied`
- Al guardar: cierra modal y recarga tabla con filtros actuales

---

## Backend necesario

### `ITransactionService` (nuevo servicio)
```csharp
Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
Task CreateManualTransactionAsync(ManualTransactionRequest request);
```

### DTOs
- `TransactionDto`: Id, OrderName (nullable string), CustomerName, TransactionType, TransactionDescription, TransactionAmount, TransactionDate
- `TransactionFilterDto`: DateFrom?, DateTo?, CustomerId?, TransactionType?
- `ManualTransactionRequest`: CustomerId, TransactionType, Amount, Description?

### `Pages/Transactions/Index.cshtml.cs`
- `OnGetAsync`: retorna Page
- `OnGetLoadAsync([FromQuery] TransactionFilterDto filter)`: retorna `JsonResult` con lista de `TransactionDto`
- `OnPostCreateManualAsync([FromBody] ManualTransactionRequest request)`: crea transacción manual

### Agregar `Manual` a `TransactionSource`
`Manual` ya existe en el enum pero está excluido de `List()`. Incluirlo.

---

## Navegación

Agregar ítem "Transacciones" al menú en `_Layout.cshtml`.

---

## Notas

- El join para la columna Orden: `Transaction.SourceId → OrderItem.Id → OrderItem.OrderId → Order.NameOfOrder`
- Global query filter en Transaction ya aplica `OrganizationId`, no requiere filtro manual
- Los filtros vacíos retornan todos los registros (sin límite de fecha)
