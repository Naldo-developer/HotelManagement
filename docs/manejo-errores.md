# Manejo centralizado de errores

Los handlers de Application pueden lanzar únicamente los errores de negocio que
necesiten: `ResourceNotFoundException` si no existe un recurso requerido y
`BusinessConflictException` si una operación entra en conflicto con el estado
del negocio. Ambos están en `HotelManagement.Application.Exceptions`, derivan
directamente de `Exception` y no conocen HTTP ni ASP.NET Core.

Ejemplos para futuros handlers:

```csharp
throw new ResourceNotFoundException("El hotel solicitado no existe.");
throw new BusinessConflictException("El número de habitación ya está registrado.");
```

Los mensajes de estas dos excepciones se muestran al cliente: deben ser públicos,
sin datos personales, SQL, credenciales ni cadenas de conexión. No envolver
cualquier fallo técnico como un conflicto o un error de validación.

El controller continúa usando `ISender.Send`, sin `try/catch`. La excepción del
handler se propaga por MediatR y el controller hasta `UseExceptionHandler`.
`GlobalExceptionHandler` la convierte en `ProblemDetails`:

| Excepción | HTTP | Detalle al cliente |
| --- | --- | --- |
| ResourceNotFoundException | 404 | Mensaje público del handler |
| BusinessConflictException | 409 | Mensaje público del handler |
| Cualquier otra excepción | 500 | Mensaje genérico, sin información interna |

`AddApiErrorHandling` centraliza `AddProblemDetails` y el registro del handler.
El middleware se coloca antes de Swagger, redirección HTTPS y controllers, también
en Development. No cambia el HTTP 400 automático de validación de `[ApiController]`.

La respuesta incluye `traceId` (Activity actual o identificador de la petición).
Para el 500, `ILogger` registra ese mismo identificador, el tipo y la pila de la
excepción. Se omiten mensaje, Data, excepciones internas, cuerpo y parámetros de
la petición para evitar registrar secretos de los proveedores. En .NET 10 se
suprimen los diagnósticos propios del middleware para evitar duplicar el registro
con una serialización completa de la excepción. Otros componentes que agreguen
logs en el futuro también deberán respetar esta política.

## Verificación y límites

`tests/HotelManagement.Api.Tests` usa TestServer y un controller exclusivo del
ensamblado de pruebas. Comprueba 404, 409, 500, ocultación de mensajes internos,
correlación y contenido seguro de logs, respuesta JSON con Accept alternativo y
validación automática 400. No agrega rutas de prueba a la API de producción ni
accede a PostgreSQL. Ejecutar `dotnet test HotelManagement.sln`.

Estas pruebas verifican el pipeline HTTP con excepciones provocadas en pruebas;
no prueban una operación CQRS de negocio real. La conexión PostgreSQL sigue
pendiente. Si una respuesta ya comenzó, el middleware no puede reemplazarla;
las cancelaciones por desconexión las trata el middleware de ASP.NET Core.

Referencia: [manejo de errores en ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0).
