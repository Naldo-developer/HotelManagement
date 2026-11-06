# Scaffolding del modelo de persistencia

Este documento describe cómo se generó el modelo a partir de la base PostgreSQL
`hotel_management` y qué ajustes manuales se aplicaron después.

El SQL (`database/001_initial_schema.sql`) es la fuente del esquema. No se usan
migraciones ni `EnsureCreated`.

## Requisitos

- SDK de .NET 10 y la herramienta local `dotnet-ef` (ver `docs/persistencia.md`).
- Base local `hotel_management` creada con el script inicial.

Desde la raíz del repositorio:

```
dotnet restore
dotnet tool restore
dotnet ef --version
```

Verificar en la base: `SELECT current_database();` debe devolver `hotel_management`.

## Conexión sin credenciales en el repositorio

La cadena de conexión se entrega mediante una variable de entorno temporal,
solo para la ventana de terminal en uso. La contraseña no se guarda en archivos
ni en Git:

```
set "ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=hotel_management;Username=postgres;Password=<TU_CONTRASEÑA>"
```

## Comando de scaffolding

Ejecutar en una sola línea, desde la raíz, en la misma ventana donde se definió la variable:

```
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL --project src/HotelManagement.Infrastructure --startup-project src/HotelManagement.Api --schema hotel --context HotelManagementDbContext --context-dir Configuration --output-dir Entities --namespace HotelManagement.Domain.Entities --context-namespace HotelManagement.Infrastructure.Configuration --no-onconfiguring
```

Al terminar, limpiar la variable:

```
set ConnectionStrings__DefaultConnection=
```

## Organización de los archivos generados

- Las entidades se generan en `Infrastructure/Entities` y se mueven a
  `src/HotelManagement.Domain/Entities`. El namespace queda como
  `HotelManagement.Domain.Entities`.
- El contexto `HotelManagementDbContext` queda en
  `src/HotelManagement.Infrastructure/Configuration`.
- Se eliminó la carpeta vacía `Infrastructure/Entities`.

## Inventario real

El esquema `hotel` tiene **16 tablas**. El scaffolding generó **14 clases de entidad**:
las tablas `user_roles` y `reservation_guests` son tablas intermedias de relaciones
muchos a muchos y EF Core las representa como colecciones, sin clase propia.

| Tabla | Entidad |
| --- | --- |
| `app_users` | `AppUser` |
| `consumptions` | `Consumption` |
| `employees` | `Employee` |
| `guests` | `Guest` |
| `hotels` | `Hotel` |
| `maintenance` | `Maintenance` |
| `payments` | `Payment` |
| `reservations` | `Reservation` |
| `reservation_status_history` | `ReservationStatusHistory` |
| `roles` | `Role` |
| `rooms` | `Room` |
| `room_types` | `RoomType` |
| `services` | `Service` |
| `stays` | `Stay` |
| `user_roles` | sin clase: `AppUser.Roles` y `Role` |
| `reservation_guests` | sin clase: `Reservation.Guests` y `Guest` |

La guía anterior del equipo indicaba 17 tablas; el script crea 16 y ese es el
número documentado.

## Ajustes manuales posteriores al scaffolding

El scaffolding generó propiedades de tipo `NpgsqlRange<DateOnly>` para dos
columnas calculadas por la base de datos:

- `reservations.stay_period`: `daterange(check_in_date, check_out_date, '[)')`.
- `maintenance.maintenance_period`: `daterange(start_date, end_date, '[)')`.

Domain no debe depender de paquetes de PostgreSQL, y esas columnas son
`GENERATED ALWAYS`, por lo que la aplicación no puede escribirlas. Se eliminó:

- La propiedad `StayPeriod` de `Reservation` y `MaintenancePeriod` de `Maintenance`,
  junto con `using NpgsqlTypes;`.
- La configuración de ambas propiedades en `HotelManagementDbContext`.
- Los índices `ex_reservations_room_period` y `ex_maintenance_room_period` del
  contexto, que EF generó a partir de las restricciones `EXCLUDE USING gist`.

Las columnas, las restricciones y los triggers **siguen en el SQL** y la base
de datos los aplica igualmente. Solo se quitó su representación en el modelo de EF.

**Quien regenere el modelo debe repetir este ajuste.**

## Verificaciones realizadas

- El scaffolding terminó sin errores y `dotnet build HotelManagement.sln` compila los cuatro proyectos.
- No hay credenciales versionadas ni `OnConfiguring` en el contexto generado.
- No se crearon migraciones.

## Limitaciones

- Las restricciones de exclusión y los triggers del SQL no están representados en EF.
- Siguen pendientes los puntos indicados en `database/README.md`: auditoría,
  concurrencia entre reservas y mantenimiento, y validaciones entre hoteles.
