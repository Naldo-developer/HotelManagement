# Preparación de la persistencia

Los cuatro proyectos mantienen `TargetFramework=net10.0`. Se requiere el SDK de
.NET 10 y acceso a NuGet para restaurar las dependencias.

## Paquetes y herramienta

| Dependencia | Versión | Ubicación |
| --- | --- | --- |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | Infrastructure |
| Microsoft.EntityFrameworkCore | 10.0.14 | Infrastructure |
| Microsoft.EntityFrameworkCore.Relational | 10.0.14 | Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 10.0.14 | Infrastructure y Api |
| dotnet-ef | 10.0.14 | Herramienta local en `.config/dotnet-tools.json` |

Todas son versiones estables. Los paquetes Microsoft EF Core y la herramienta
comparten la versión 10.0.14. Core y Relational se fijan explícitamente para
evitar que el proveedor resuelva una revisión anterior por sus dependencias mínimas.
[Npgsql 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3)
está dirigido a .NET 10 y admite Core y Relational `>= 10.0.4` y `< 11.0.0`;
10.0.14 cumple ese rango. El número de revisión del proveedor no necesita
coincidir con el de Microsoft.

Design tiene `PrivateAssets=all` en ambos proyectos para que no se propague a
sus consumidores. Api lo referencia directamente porque es el proyecto de inicio;
Infrastructure lo incluye como proyecto destino del scaffolding.
Las referencias entre los proyectos existentes se mantienen.

## Restauración y comprobación

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet tool restore
dotnet ef --version
dotnet build HotelManagement.sln
```

El manifiesto fija la herramienta local; no se necesita instalar `dotnet-ef`
globalmente. El comando de versión debe mostrar `10.0.14`.

## Scaffolding posterior

La base PostgreSQL local se llama `hotel_management`.
Charly realizará el scaffolding posteriormente, cuando el esquema esté listo,
utilizando el proveedor `Npgsql.EntityFrameworkCore.PostgreSQL` y estas opciones
desde la raíz:

```text
--project src/HotelManagement.Infrastructure
--startup-project src/HotelManagement.Api
```

Esta preparación no ejecuta scaffolding, scripts SQL ni cambios en la base de datos,
y no crea entidades, DbContext, migraciones o tablas. Tampoco cambia DTOs,
lógica de negocio ni endpoints.

Las contraseñas y las cadenas de conexión con credenciales no deben guardarse en
el repositorio, tampoco en `appsettings*.json`, scripts ni esta documentación.
Usar variables de entorno o secretos de usuario locales para suministrarlas.
Al realizar el scaffolding, usar `--no-onconfiguring` para evitar que EF genere
la cadena de conexión dentro del DbContext. No copiar credenciales reales a
comandos que queden registrados en el historial de la terminal.

Referencia: [herramientas CLI de EF Core](https://learn.microsoft.com/en-us/ef/core/cli/dotnet),
incluidas las opciones de proyecto destino, proyecto de inicio y scaffolding.
