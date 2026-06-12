# Design: Signo de Moneda + Formateo de Montos

**Spec**: 008 | **Date**: 2026-06-08 | **Source**: `requerimientos/Mejoras varias fase 2.txt` ítems 7, 7.1

---

## Objetivo

Agregar campo `Sign` al catálogo de monedas y usarlo para formatear todos los montos en las pantallas con el patrón `{signo} {monto}` (ej: `$ 5,000.00`, `₡ 45,000.00`).

---

## Cambio de Modelo

### `Currency.cs`
```csharp
public string Sign { get; set; } = string.Empty;
```

### `CurrencyEntityTypeConfiguration.cs`
- Configurar `HasMaxLength(10)` para `Sign`
- Actualizar seed data: Colones → `₡`, Dólares → `$`

### Migración EF
`dotnet ef migrations add AddSignToCurrency`

---

## Formato estándar

`{signo} {monto:N2}` → `$ 5,000.00` / `₡ 45,000.00`

Implementar como helper C# estático reutilizable:
```csharp
// MariCamiStore/Helpers/AmountFormatter.cs
public static string Format(decimal amount, string sign) =>
    $"{sign} {amount:N2}";
```

---

## Estrategia por contexto de renderizado

### Server-side (Razor `.cshtml`)

Las páginas que renderizan montos en el servidor reciben el signo de la moneda a través del modelo de página o helpers. Se usa `AmountFormatter.Format(amount, sign)`.

Pantallas afectadas:
- `Orders/Index.cshtml`: totales de orden (moneda local)
- `Reports/Saldos.cshtml` → eliminado por Spec 006; el nuevo handler AJAX devuelve el signo

### Client-side (JavaScript)

Las páginas con tablas AJAX ya cargan `/Currencies?handler=Load`. Al agregar `Sign` al DTO de Currency, el JS recibe automáticamente el signo.

**Patrón estándar JS** (función global en `utilities.js`):
```javascript
function formatAmount(amount, sign) {
    return sign + ' ' + parseFloat(amount).toLocaleString('es-CR', {
        minimumFractionDigits: 2, maximumFractionDigits: 2
    });
}
```

**Variables de signo por página** (inyectadas desde Razor en el bloque `<script>`):
```html
<script>
    var localCurrencySign = '@localCurrencySign';   // signo moneda local (CRC)
    var orderCurrencySign = '@orderCurrencySign';   // signo moneda de la orden
</script>
```

---

## Pantallas afectadas y su moneda

| Pantalla | Campos | Moneda |
|----------|--------|--------|
| Orders/Index | TotalAgreedPriceInLocal, TotalOfTheOrder, EstimatedProfit | Local |
| Orders/Items — header | TotalAgreedPriceInLocal, Subtotal, Impuestos, Envío CR, Total Proveedor, Total Orden, Ganancia | Local |
| Orders/Items — tabla | ListPrice, ListPriceTaxWithTax, RealPrice, EstimateShipping | Moneda de orden |
| Orders/Items — tabla | ServiceFeeInLocal, AgreedPriceInLocal, Total calculado, Subtotales | Local |
| Payments (unificado) | Saldo Global, Saldo Org, tabla de saldos | Local |
| Transactions (nuevo) | TransactionAmount | Moneda de la transacción |
| ProductTypes | EstimateShipping | Moneda del tipo |
| ProductTypes | ServiceFeeInLocal | Local |

---

## Backend — Cambios al DTO de Currency

`ICatalogService.GetCurrenciesAsync()` (o el endpoint Load) debe incluir `Sign` en la respuesta.  
Los endpoints que ya devuelven monedas (`/Currencies?handler=Load`) incluirán el campo automáticamente al ser parte del modelo.

---

## Notas

- No cambia ningún valor almacenado en BD — solo presentación.
- `Sign` puede quedar vacío (`""`) para monedas sin signo definido; en ese caso mostrar solo el número.
- El seed data debe actualizarse para las monedas existentes antes de desplegar en producción.
- La pantalla de catálogo de Monedas (`/Currencies/Index`) debe permitir editar el campo `Sign`.
