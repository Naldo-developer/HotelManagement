
# T08 - Administración de hoteles y tipos de habitación

## Objetivo

Implementar el registro y la actualización de hoteles y tipos de habitación,
manteniendo las relaciones existentes y restringiendo las operaciones de
escritura a usuarios administradores.

## Dependencias

- T01: Consulta de hoteles y tipos de habitación.
- C04: Autenticación y autorización por roles.

## Implementación

Se amplió la funcionalidad existente respetando la arquitectura del proyecto,
utilizando CQRS, MediatR, Entity Framework Core y PostgreSQL.

- DTOs de creación, actualización y respuesta.
- Commands y handlers para las operaciones de escritura.
- Mappings para convertir entidades en DTOs.
- Repositorios específicos para hoteles y tipos de habitación.
- Validaciones de negocio y restricciones de PostgreSQL.
- Manejo centralizado de errores mediante ProblemDetails.

## Endpoints

| Método | Ruta | Acceso |
|---|---|---|
| GET | /api/hotels | Usuario autenticado |
| POST | /api/hotels | ADMIN |
| PUT | /api/hotels/{hotelId} | ADMIN |
| GET | /api/room-types?hotelId={id} | Usuario autenticado |
| POST | /api/room-types | ADMIN |
| PUT | /api/room-types/{roomTypeId} | ADMIN |

Las operaciones de escritura utilizan AppPolicies.Configuration.
La autenticación se realiza mediante la cookie hotel_session.

## Contratos de entrada

### Registrar hotel

POST /api/hotels

```json
{
  "name": "Hotel T08 Arequipa",
  "taxId": "20999999998",
  "address": "Arequipa, Perú",
  "phone": "054123456",
  "email": "hotel.t08@example.com",
  "currencyCode": "PEN",
  "timezone": "America/Lima"
}
```

Para actualizar un hotel se utiliza el mismo conjunto de campos
mediante PUT, indicando el identificador en la ruta.

### Registrar tipo de habitación

POST /api/room-types

```json
{
  "hotelId": 3,
  "name": "Suite T08",
  "description": "Suite matrimonial",
  "maxAdults": 2,
  "maxChildren": 1,
  "baseRate": 180.00
}
```

### Actualizar tipo de habitación

PUT /api/room-types/{roomTypeId}

```json
{
  "name": "Suite T08 Premium",
  "description": "Suite mejorada",
  "maxAdults": 3,
  "maxChildren": 1,
  "baseRate": 250.00
}
```

El hotel asociado no puede modificarse mediante esta operación.

## Validaciones

- Nombre obligatorio y límites de longitud según el esquema.
- Documento tributario único cuando se proporciona.
- Nombre de tipo de habitación único dentro del mismo hotel.
- Identificadores positivos.
- Existencia del hotel asociado.
- Hotel activo para registrar nuevos tipos.
- Capacidad mínima de un adulto.
- Cantidad de niños no negativa.
- Tarifa no negativa y máximo dos decimales.
- Formato de moneda y zona horaria válida.
- Conservación de identificadores y referencias existentes.

## Respuestas HTTP

| Código | Descripción |
|---|---|
| 200 | Consulta o actualización correcta |
| 201 | Registro creado correctamente |
| 400 | Datos inválidos |
| 401 | Usuario sin sesión |
| 403 | Usuario sin permisos administrativos |
| 404 | Recurso inexistente |
| 409 | Conflicto de negocio o registro duplicado |

## Integridad de datos

Las actualizaciones conservan los identificadores, fechas de creación,
estados activos y relaciones de los registros existentes.

Los cambios en la tarifa base del tipo de habitación no modifican
las tarifas históricas almacenadas en las reservas.

No se realizaron modificaciones al esquema de PostgreSQL.
La eliminación y desactivación de catálogos están fuera del alcance de T08.

## Pruebas automatizadas

Se agregó CatalogPersistenceTests.cs y se ampliaron las pruebas
de autorización en RolePolicyTests.cs.

La prueba de integración utiliza PostgreSQL real mediante una
base temporal aislada.

Escenarios comprobados:

- Registro y actualización de hoteles y tipos.
- Rechazo de registros duplicados.
- Validación de datos y referencias.
- Restricción de escrituras para recepcionistas.
- Rechazo de escritura sin sesión.
- Persistencia mediante un nuevo DbContext.
- Conservación de habitaciones y reservas asociadas.
- Conservación de tarifas históricas.

### Resultado de la suite completa

Fecha: 26/11/2026.

- PostgreSQL: 18.
- Framework: .NET 10.
- Total: 50 pruebas.
- Aprobadas: 50.
- Fallidas: 0.
- Omitidas: 0.

Comando utilizado:

```powershell
dotnet test HotelManagement.sln --nologo
```

Se configuró temporalmente R02_POSTGRES_ADMIN siguiendo el
procedimiento de pruebas del repositorio. La instancia fue
detenida correctamente después de la ejecución.

## Verificación manual

Se realizaron pruebas mediante Swagger y DataGrip.

| Operación | Resultado |
|---|---|
| Inicio de sesión ADMIN | Correcto |
| Registro de hotel | Correcto |
| Consulta de hoteles | Correcto |
| Registro de tipo de habitación | Correcto |
| Consulta de tipos | Correcto |
| Actualización de hotel | Correcto |
| Actualización de tipo | Correcto |
| Hotel duplicado | 409 |
| Tipo duplicado | 409 |
| Persistencia del tipo en PostgreSQL | Correcto |

Datos ficticios utilizados en el entorno local:

- Hotel ID: 3.
- Nombre: Hotel T08 Arequipa Premium.
- Tipo ID: 5.
- Nombre del tipo: Suite T08 Premium.
- Tarifa inicial: 180.00 PEN.
- Tarifa actualizada: 250.00 PEN.

## Integración con Y10

El frontend puede utilizar los endpoints GET existentes para obtener
los datos completos de los catálogos y las operaciones POST y PUT
para registrar y actualizar hoteles y tipos de habitación.

Después de cada modificación se recomienda actualizar el listado
correspondiente y mostrar los mensajes de ProblemDetails cuando
la API devuelva un error.

Los identificadores utilizados en las pruebas locales son referenciales.
El frontend debe trabajar con los identificadores devueltos por la API.
