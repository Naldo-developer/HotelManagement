# HotelManagement

Solución .NET 10 organizada en Api, Application, Domain e Infrastructure.

## Ejecutar la API

Requiere el SDK de .NET 10. Desde la raíz del repositorio:

```powershell
dotnet build HotelManagement.sln
dotnet run --project src/HotelManagement.Api --launch-profile http
```

El perfil `http` configura Development y escucha en `http://localhost:5075`:

- Salud: http://localhost:5075/api/health
- Swagger UI: http://localhost:5075/swagger
- Documento OpenAPI: http://localhost:5075/swagger/v1/swagger.json

Swagger UI y su documento solo están disponibles en Development.

También puedes usar `--launch-profile https`, que escucha en
`https://localhost:7143` y `http://localhost:5075`, con redirección a HTTPS.
Para HTTPS necesitas un certificado de desarrollo de confianza (`dotnet dev-certs https --trust`).

`GET /api/health` devuelve HTTP 200 con:

```json
{"status":"ok","service":"HotelManagement.Api"}
```

Este endpoint solo comprueba que la API responde; no verifica PostgreSQL.
El SQL inicial y el modelo generado ya están integrados: el script define 16 tablas
y el scaffolding aporta las entidades de Domain y el DbContext de Infrastructure.
Esto no implica que la API esté conectada a PostgreSQL; su configuración de conexión
e inyección del contexto está pendiente.

Application registra MediatR mediante `AddApplication`. Todavía no hay casos de uso
CQRS ni endpoints de negocio implementados. Las convenciones y la versión elegida
se documentan en [docs/arquitectura.md](docs/arquitectura.md).
