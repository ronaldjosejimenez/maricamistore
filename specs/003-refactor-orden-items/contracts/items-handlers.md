# Contracts: Items Page Handlers

**Page**: `MariCamiStore/Pages/Orders/Items.cshtml.cs`
**Feature**: specs/003-refactor-orden-items

---

## Handlers existentes — cambios necesarios

### `OnGetLoadAsync(Guid orderId)` → GET `?handler=Load`

**Cambio**: La respuesta incluye nuevos campos. El campo `listPriceTax` se renombra a `listPriceTaxWithTax`. Se agrega `hasImage` para indicar si el ítem tiene imagen.

**Response shape (por ítem)**:
```json
{
  "id": "guid",
  "orderId": "guid",
  "customerId": "guid",
  "productDescription": "string",
  "productLink": "string | null",
  "productSourceCode": "string | null",
  "hasImage": true,
  "productTypeId": "guid",
  "listPrice": 0.00,
  "listPriceTaxWithTax": 0.00,
  "realPrice": 0.00,
  "estimateShipping": 0.00,
  "serviceFeeInLocal": 0.00,
  "agreedPriceInLocal": 0.00
}
```

Note: `productImage` (binario) NO se incluye en load. Solo `hasImage`.

---

### `OnPostInsertAsync([FromBody] OrderItemDto item)` → POST `?handler=Insert`

**Cambio**: `OrderItemDto` agrega `productLink`, `productSourceCode`, `productImageBase64` (nullable). Renombra `listPriceTax` → `listPriceTaxWithTax`.

**Request body**:
```json
{
  "orderId": "guid",
  "customerId": "guid",
  "productDescription": "string",
  "productLink": "string | null",
  "productSourceCode": "string | null",
  "productImageBase64": "base64string | null",
  "productTypeId": "guid",
  "listPrice": 0.00,
  "listPriceTaxWithTax": 0.00,
  "realPrice": 0.00,
  "estimateShipping": 0.00,
  "serviceFeeInLocal": 0.00,
  "agreedPriceInLocal": 0.00
}
```

**Backend validation**: Si `productImageBase64` no es null, decodificar y verificar que `byte[].Length <= 2097152` (2 MB). Retornar error si excede.

**Response**: `{ "id": "guid", ...campos del ítem creado... }`  
**Error**: `{ "error": "La imagen supera el límite de 2 MB." }`

---

### `OnPostUpdateAsync([FromBody] OrderItemDto item)` → POST `?handler=Update`

**Cambio**: Mismo `OrderItemDto` que Insert, más el campo `id`. Si `productImageBase64` es `null` y el ítem ya tenía imagen, NO sobreescribir (preservar imagen existente). Si `productImageBase64` es `""` (string vacío), limpiar la imagen.

**Request body**: igual a Insert + `"id": "guid"`

**Response**: `{ "id": "guid", ...campos actualizados... }`

---

### `OnPostDeleteAsync([FromBody] DeleteRequest)` → POST `?handler=Delete`

**Sin cambio.** Sigue igual.

**Request body**: `{ "id": "guid" }`  
**Response success**: `{ "success": true }`  
**Response error**: `{ "success": false, "error": "Solo se pueden eliminar ítems de órdenes Pendientes." }`

---

### `OnPostUpdateTotalsAsync([FromBody] OrderTotalsDto)` → POST `?handler=UpdateTotals`

**Cambio**: Agrega `totalAgreedPriceInLocal` al DTO.

**Request body**:
```json
{
  "orderId": "guid",
  "totalAgreedPriceInLocal": 0.00,
  "shippingAmountToCR": 0.00,
  "totalWithoutTaxes": 0.00,
  "taxesAmount": 0.00,
  "totalToPayToSupplier": 0.00,
  "totalOfTheOrder": 0.00,
  "estimatedProfitInLocal": 0.00
}
```

---

### `OnGetProductTypeAsync(Guid id)` → GET `?handler=ProductType&id={guid}`

**Sin cambio.** Retorna `{ estimateShipping, serviceFeeInLocal, currencyId }`.

---

## Handlers nuevos

### `OnGetProductTypesByCurrencyAsync(Guid currencyId)` → GET `?handler=ProductTypesByCurrency&currencyId={guid}`

**Propósito**: Retorna ProductTypes cuya moneda coincide con `currencyId`.

**Response**:
```json
[
  { "id": "guid", "name": "string" }
]
```

---

### `OnGetItemImageAsync(Guid itemId)` → GET `?handler=ItemImage&itemId={guid}`

**Propósito**: Retorna la imagen del ítem como archivo binary para display en popup.

**Response**: `FileContentResult` con `Content-Type: image/*`  
**Error 404**: si ítem no existe o no tiene imagen  
**Error 403**: si el ítem no pertenece a la organización activa (cubierto por query filter del DbContext)

---

### `OnPostUpdateOrderAsync([FromBody] OrderHeaderDto)` → POST `?handler=UpdateOrder`

**Propósito**: Actualiza los campos editables de la orden desde la edición in-place.

**Request body**:
```json
{
  "orderId": "guid",
  "exchangeRate": 0.00,
  "taxPercentage": 0.00,
  "shippingAmountIntern": 0.00,
  "discountAmount": 0.00
}
```

**Response success**: `{ "success": true }`  
**Response error**: `{ "success": false, "error": "string" }`

**Business rule**: Solo permitir update si la orden existe, pertenece a la organización activa, y su estado es Pending.
