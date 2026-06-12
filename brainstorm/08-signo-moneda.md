# Brainstorm: Signo de Moneda + Formateo de Montos

**Fecha**: 2026-06-08  
**Fuente**: `requerimientos/Mejoras varias fase 2.txt` ítems 7, 7.1

---

## Problema

Los montos en la aplicación no muestran el signo de la moneda. El usuario quiere que todos los montos aparezcan con el formato `{signo} {monto}`, por ejemplo:
- `$ 5,000.00` (Dólares)
- `₡ 45,000.00` (Colones)

Actualmente el catálogo de Monedas (`Currency`) no tiene campo `Sign`.

---

## Solución propuesta

### 1. Agregar `Sign` al modelo `Currency`

```csharp
public string Sign { get; set; } = string.Empty;
```

Configuración EF: `HasMaxLength(10)`.

Migración: `AddSignToCurrency`.

Seed data actualizado:
- Colones → `₡`
- Dólares → `$`

La pantalla de catálogo de Monedas (`/Currencies/Index`) debe mostrar y permitir editar el campo `Sign`.

### 2. Formato estándar

`{signo} {monto:N2}` → ejemplos:
- `$ 5,000.00`
- `₡ 45,000.00`

**No** usar paréntesis, **siempre** con espacio entre signo y monto.

### 3. Helper C# — `AmountFormatter`

```csharp
// MariCamiStore/Helpers/AmountFormatter.cs
public static class AmountFormatter
{
    public static string Format(decimal amount, string sign) =>
        $"{sign} {amount:N2}";
}
```

### 4. Función JS global — `formatAmount`

Agregar a `utilities.js`:
```javascript
function formatAmount(amount, sign) {
    return sign + ' ' + parseFloat(amount).toLocaleString('es-CR', {
        minimumFractionDigits: 2, maximumFractionDigits: 2
    });
}
```

### 5. Inyección de signo en páginas Razor

Para páginas con JS que manejan montos, inyectar el signo desde el servidor:
```html
<script>
    var localCurrencySign = '@localCurrencySign';
    var orderCurrencySign = '@orderCurrencySign';
</script>
```

---

## Pantallas afectadas

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

## Backend — DTO de Currency

El endpoint `/Currencies?handler=Load` incluirá `Sign` automáticamente al ser parte del modelo y mapearse al DTO.

---

## Notas

- No cambia ningún valor almacenado en BD — solo presentación
- Si `Sign` es vacío (`""`), mostrar solo el número (fallback gracioso)
- El seed data debe actualizarse para las monedas existentes antes de desplegar en producción
