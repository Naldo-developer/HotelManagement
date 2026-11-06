# Arquitectura y convenciones

La solución usa CQRS + MediatR para organizar los casos de uso. Los Services
tradicionales no deben concentrar la lógica de negocio.

## Dependencia y registro inicial

Se fija **MediatR 12.5.0** en `HotelManagement.Application`. Es una versión estable
con licencia Apache-2.0, elegida para esta configuración inicial sin requerir una
clave de licencia. No es la versión más reciente.

El paquete ofrece destinos .NET 6.0 y .NET Standard 2.0, compatibles con el destino
`net10.0` de la solución, que consume los recursos del destino .NET 6.0.
Las dependencias declaradas para ese destino son:

- `MediatR.Contracts >= 2.0.1 y < 3.0.0`.
- `Microsoft.Extensions.DependencyInjection.Abstractions >= 8.0.0`.

El destino alternativo .NET Standard 2.0 también requiere
`Microsoft.Bcl.AsyncInterfaces >= 8.0.0`; no es el destino consumido aquí.

La integración con `IServiceCollection` viene incluida; no se agrega el paquete
separado `MediatR.Extensions.Microsoft.DependencyInjection`.

Fuentes de la versión y sus requisitos:
[NuGet 12.5.0](https://www.nuget.org/packages/MediatR/12.5.0) y
[documentación de la versión](https://github.com/jbogard/MediatR/blob/v12.5.0/README.md).

`Configuration/ApplicationServicesExtensions.cs` expone `AddApplication`, que
registra MediatR y busca handlers en el ensamblado que contiene esa extensión:
`HotelManagement.Application`. `Program.cs` invoca esta extensión antes de
`builder.Build()`. El registro queda preparado para los futuros handlers.

## Casos de uso

- Ubicar los comandos en `Application/UseCases/<Entidad>/Commands` y las consultas
  en `Application/UseCases/<Entidad>/Queries`.
- Cada Command/Query debe ser `public` y su Handler `internal`, ambos en el mismo
  archivo: solicitud arriba y handler debajo.
- `Handle` conserva su parámetro `CancellationToken` y lo propaga a las operaciones
  asíncronas que lo admiten.
- Usar `Create` uniformemente para las operaciones de creación.
- Los controllers reciben `ISender` y envían solicitudes mediante `ISender.Send`.
  Sus acciones usan llaves y `return`; no contienen DbContext, LINQ ni lógica de
  negocio.
- Usar repositorios específicos por entidad; no usar `GenericRepository`.
- Domain permanece independiente de las otras capas y de los proveedores de
  persistencia. Application no referencia Infrastructure.

## Alcance actual

Esta configuración no agrega solicitudes, handlers ficticios ni endpoints de
negocio. El SQL y el modelo generado están integrados, pero la conexión PostgreSQL,
el registro del DbContext y el manejo común de errores serán tareas posteriores.

Comprobar el arranque, `/api/health` y Swagger valida la infraestructura HTTP.
Estas comprobaciones no verifican una operación CQRS real ni acceso a PostgreSQL:
todavía no hay casos de uso implementados.
