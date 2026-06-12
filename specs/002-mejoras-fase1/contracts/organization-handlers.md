# Contracts: Organization Handlers

## `Pages/Organizations/Index`

### Load Organizations
`GET /Organizations?handler=Load`

Response:
```json
[
  { "id": "guid", "name": "MariCami CR" }
]
```

### Set Active Organization (existing)
`POST /Organizations?handler=SetActive`

Request: `{ "organizationId": "guid" }`
Response: `{ "success": true }`

### Insert Organization (NEW)
`POST /Organizations?handler=Insert`

Request:
```json
{ "name": "Nueva Organización" }
```
Response: created organization object
```json
{ "id": "guid", "name": "Nueva Organización" }
```

### Update Organization (NEW)
`POST /Organizations?handler=Update`

Request:
```json
{ "id": "guid", "name": "Nombre Actualizado" }
```
Response: updated organization object

### Delete Organization (NEW)
`POST /Organizations?handler=Delete`

Request: `{ "id": "guid" }`

Response (success):
```json
{ "success": true }
```

Response (blocked — has related records):
```json
{ "success": false, "error": "No se puede eliminar la organización porque tiene órdenes o configuraciones asociadas." }
```
