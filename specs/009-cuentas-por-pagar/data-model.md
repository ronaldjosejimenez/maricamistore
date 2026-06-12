# Data Model: Módulo Cuentas por Pagar

**Feature**: 009-cuentas-por-pagar
**Date**: 2026-06-10

## New Entities

### PeriodControl

Represents a transaction month. One record has `IsClosed = false` at any time (the open period).

```
PeriodControl
├── Id                  Guid        PK
├── OrganizationId      Guid        FK → Organization; global query filter
├── TransactionMonth    int         1–12; required
├── TransactionYear     int         4-digit year; required
├── ExchangeRate        decimal(18,4) editable; copied from Configuration on creation
├── PagosRealizados     decimal(18,2) manual, default 0
├── EnCuenta            decimal(18,2) manual, default 0
├── IsClosed            bool        default false; true after CloseMonth
└── CreatedAt           DateTime    UTC
```

**Constraints**:
- `UNIQUE (OrganizationId, TransactionMonth, TransactionYear)` — one period per month/year per org
- At most one row with `IsClosed = false` per `OrganizationId` (enforced in service logic, not DB constraint)

**EF Configuration**: `HasQueryFilter(p => p.OrganizationId == currentOrg.OrganizationId)`

---

### CxPEntry

An individual accounts-payable entry belonging to a period.

```
CxPEntry
├── Id              Guid            PK
├── PeriodControlId Guid            FK → PeriodControl; required
├── CurrencyId      Guid            FK → Currency; required
├── Amount          decimal(18,2)   required; can be 0 (e.g., zero-shipping case)
├── Reference       string(500)     required; free text or auto-generated name
├── Type            string(30)      "Manual" | "AutoActiva" | "AutoDelivered" | "SaldoAnterior"
├── OrderId         Guid?           nullable; FK → Order (for auto entries only)
└── CreatedAt       DateTime        UTC
```

**No EF global query filter**: entries are always accessed via their `PeriodControl`, which is already filtered.

---

## Modified Entities

### Order (existing)

Add one field to the existing `Order` entity:

```
Order
└── ActualShippingAmountToCR    decimal(18,2)   new; default 0
                                                Captured when order → Delivered
                                                Does NOT replace ShippingAmountToCR (estimated)
                                                Does NOT affect existing order calculations
```

**Migration column**: `[ActualShippingAmountToCR] DECIMAL(18,2) NOT NULL DEFAULT 0`

---

## Modified DTOs

### TransitionOrderDto (existing record in IOrderService.cs)

```csharp
// Before:
public record TransitionOrderDto(
    Guid OrderId,
    string ToStatus,
    DateTime TransitionDate,
    string? Notes,
    string? Justification);

// After — add optional parameter at end:
public record TransitionOrderDto(
    Guid OrderId,
    string ToStatus,
    DateTime TransitionDate,
    string? Notes,
    string? Justification,
    decimal? ActualShippingAmountToCR = null);
```

All existing call sites remain compatible (null default).

---

## New DTOs (in ICxPService.cs)

```csharp
// Indicators calculated for the open period
public record CxPPeriodIndicatorsDto(
    Guid PeriodId,
    int TransactionMonth,
    int TransactionYear,
    decimal ExchangeRate,
    Dictionary<string, CxPCurrencyBalance> PorPagarPorMoneda,   // key = currencyId.ToString()
    decimal PorPagarEnColones,
    decimal SaldosPorCobrar,
    decimal PagosRealizados,
    decimal DeudaAPagar,
    decimal EnCuenta,
    decimal PendienteDeRecoger,
    decimal ShippingCRPendientesDeAplicar,
    decimal Posicion,
    bool IsClosed);

public record CxPCurrencyBalance(
    string CurrencyName,
    string Sign,
    decimal Amount);

// Entry row for the UI table
public record CxPEntryDto(
    Guid Id,
    Guid CurrencyId,
    string CurrencyName,
    string Sign,
    decimal Amount,
    string Reference,
    string Type,
    Guid? OrderId,
    DateTime CreatedAt);

// Request for manual entry creation
public record CreateManualCxPEntryRequest(
    Guid CurrencyId,
    decimal Amount,
    string Reference);

// Request for period fields update
public record UpdatePeriodFieldsRequest(
    decimal ExchangeRate,
    decimal PagosRealizados,
    decimal EnCuenta);

// Request for first period initialization
public record InitPeriodRequest(
    int Month,
    int Year,
    decimal ExchangeRate);
```

---

## EF Relationships

```
Organization   1──* PeriodControl
PeriodControl  1──* CxPEntry
Currency       1──* CxPEntry
Order          1──0..1 CxPEntry (via CxPEntry.OrderId, nullable)
```

---

## Database Migration

**Name**: `AddCxPModule`

**Operations**:
1. `AddColumn Orders.ActualShippingAmountToCR DECIMAL(18,2) NOT NULL DEFAULT 0`
2. `CreateTable PeriodControls` (all fields listed above)
3. `CreateTable CxPEntries` (all fields listed above)
4. `AddForeignKey CxPEntries.PeriodControlId → PeriodControls.Id CASCADE`
5. `AddForeignKey CxPEntries.CurrencyId → Currencies.Id RESTRICT`
6. `AddForeignKey CxPEntries.OrderId → Orders.Id SET NULL`
7. `CreateIndex PeriodControls(OrganizationId, TransactionMonth, TransactionYear) UNIQUE`
