# Quickstart / Manual Verification: 014-cierre-mes-cxp

## Pre-deploy check (production DB, before applying the migration)
```sql
SELECT OrganizationId, COUNT(*) AS OpenPeriods
FROM dbo.PeriodControls WHERE IsClosed = 0
GROUP BY OrganizationId HAVING COUNT(*) > 1;   -- must return no rows
```

## Local
```bash
cd MariCamiStore && dotnet build
dotnet ef database update   # DEV database only
dotnet run
```

1. **Panel**: column 1 shows Deuda a Pagar (pastel red), En Cuenta (₡) (pastel green, equals the editable field), Pendiente de Recoger (pastel yellow); text readable. Save a new En Cuenta → box updates.
2. **Validations**: save TC 0 or −1 → "El tipo de cambio debe ser mayor a cero."; Pagos or En Cuenta −1 → "El valor no puede ser negativo."; nothing saved.
3. **Preview opens without writing**: click "Cerrar Mes" → two-column dialog; Cancel → month still open, nothing changed.
4. **Carry-over**:
   - Deuda 500.000, En Cuenta 200.000 → new month: Saldo anterior 300.000, En Cuenta propuesto 0.
   - Deuda 500.000, En Cuenta 700.000 → "Sin saldo anterior", En Cuenta propuesto 200.000.
   - Deuda 0, En Cuenta 100.000 → no saldo, En Cuenta 100.000.
5. **Edit in preview**: change new TC / En Cuenta → new-month indicators refresh; TC 0 or En Cuenta −1 → error shown, Confirm disabled.
6. **Confirm** → month closed; new month has edited TC and En Cuenta, Pagos 0, Saldo anterior as computed.
7. **Double close**: open dialog in two tabs; confirm in tab A, then in tab B → "Este mes ya fue cerrado." and reload; exactly one open month.
8. **Config TC 0**: set Configuration TC to 0 → preview proposes the closing month's TC.
