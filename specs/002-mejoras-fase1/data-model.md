# Data Model: Mejoras Fase 1

No se requieren nuevas entidades ni migraciones de base de datos para este feature. Todos los campos necesarios ya existen en el modelo de datos.

---

## Cambios de Configuración (no de esquema)

### `BaseSettings` — Agregar propiedad

```
BaseSettings
├── ConnectionString    (existing)
├── DefaultCulture      (existing)
└── DefaultOrganizationId : Guid?   ← ADD (nullable; null = sin org por defecto)
```

### `appsettings.json` — Agregar sección

```json
{
  "DefaultOrganizationId": null
}
```
(null = deshabilitado; reemplazar con el GUID real de la org deseada para activar auto-carga)

---

## Entidades Existentes Relevantes (sin cambios de esquema)

### `Organization`
```
Organization
├── Id             : Guid  (PK)
└── Name           : string
```
- Relacionada con: `Configuration.OrganizationId`, `Order.OrganizationId`
- Regla de eliminación: bloqueada si existen `Order` u `Configuration` con ese `OrganizationId`

### `Configuration`
```
Configuration
├── Id                     : Guid  (PK)
├── OrganizationId         : Guid  (FK → Organization)
├── ExchangeRate           : decimal
├── ExchangeRateMargin     : decimal
├── TaxPercentage          : decimal
├── LocalCurrencyId        : Guid  (FK → Currency)
├── OrderCurrencyIdDefault : Guid  (FK → Currency)
└── ProductTypeIdDefault   : Guid?  (FK → ProductType)
```
- **Invariante**: máximo una configuración por `OrganizationId`
- El `UpsertConfigurationAsync` en `CatalogService` ya respeta esta invariante en el backend

### `Currency`
```
Currency
├── Id           : Guid  (PK)
├── Name         : string
└── Abbreviation : string   ← ya existe; debe mostrarse en UI en lugar del Id
```

### `Order`
```
Order
├── Id                    : Guid   (PK)
├── OrganizationId        : Guid   (FK → Organization)
├── NameOfOrder           : string
├── SupplierId            : Guid   (FK → Supplier)
├── ExchangeRate          : decimal
├── TaxPercentage         : decimal
├── Status                : string (key de OrderStatus enumeration)
└── ... (campos de totales existentes)
```
- **Regla**: ítems solo modificables cuando `Status == "Pending"`
- Los valores `ExchangeRate` y `TaxPercentage` se pre-cargan desde `Configuration` al crear una orden nueva

### `OrderItem`
```
OrderItem
├── Id                 : Guid   (PK)
├── OrderId            : Guid   (FK → Order)
└── ... (campos existentes)
```
- Acción de agregar/editar/eliminar bloqueada cuando `Order.Status != "Pending"`

---

## Nuevos Servicios (sin cambios de esquema)

### `IOrganizationService` (nuevo)
Encapsula toda la lógica CRUD de organizaciones:
- `GetOrganizationsAsync()` → listado
- `GetOrganizationByIdAsync(Guid id)` → organización o null
- `CreateOrganizationAsync(Organization org)` → organización creada
- `UpdateOrganizationAsync(Organization org)` → organización actualizada
- `DeleteOrganizationAsync(Guid id)` → `(bool Success, string? Error)`; falla si hay registros relacionados

---

## Cambios de Servicio Existente

### `ICurrentOrganizationService` — Extender con fallback

Agregar método o modificar comportamiento para que, si la sesión no tiene organización activa y `BaseSettings.DefaultOrganizationId` está configurado, se cargue automáticamente en sesión durante el primer request.

### `ICatalogService.GetConfigurationAsync` — Sin cambios de firma

El método ya filtra por `OrganizationId` de la sesión. Sin cambios.
