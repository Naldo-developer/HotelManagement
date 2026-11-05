# Base de datos – Sistema de gestión hotelera

Esta carpeta contiene el esquema inicial de PostgreSQL del proyecto.

> **Estado:** primera versión para compartir y probar. **No es definitiva.**
> Los cambios posteriores se entregarán como scripts incrementales.

## Contenido

| Archivo | Descripción |
| --- | --- |
| `001_initial_schema.sql` | Esquema inicial: extensión, esquema `hotel`, 16 tablas, índices, triggers y roles iniciales. |

## Requisitos

- PostgreSQL instalado y en ejecución (puerto 5432 por defecto).
- Un usuario con permisos para crear extensiones (por ejemplo, `postgres`), porque el script ejecuta `CREATE EXTENSION IF NOT EXISTS btree_gist;`.
- Un cliente SQL: pgAdmin, DataGrip, Rider o `psql`.

## Orden de ejecución

Por ahora hay un solo script. Cada integrante lo ejecuta en **su propia base local**.

1. Crear una base vacía llamada `hotel_management`:
   ```sql
   CREATE DATABASE hotel_management;
   ```
2. Conectarse a `hotel_management` y comprobar:
   ```sql
   SELECT current_database();
   ```
   El resultado debe ser `hotel_management`.
3. Ejecutar `001_initial_schema.sql` **una sola vez** sobre esa base vacía.
   - Desde pgAdmin: abrir el Query Tool de `hotel_management`, cargar el archivo y ejecutarlo.
   - Desde terminal, en la carpeta principal del repositorio:
     ```
     psql -U postgres -h localhost -d hotel_management -f database/001_initial_schema.sql
     ```

El script no borra bases ni tablas. Si ya se ejecutó antes, volver a correrlo producirá errores de objetos existentes; no se debe borrar nada para forzarlo.

## Qué crea el script

- Extensión `btree_gist` y esquema `hotel`.
- 16 tablas: `hotels`, `room_types`, `rooms`, `guests`, `employees`, `app_users`, `roles`, `user_roles`, `reservations`, `reservation_guests`, `stays`, `services`, `consumptions`, `payments`, `maintenance` y `reservation_status_history`.
- Índices y triggers (actualización de `updated_at`, validación de reservas y mantenimiento, historial de estados de reserva).
- Cuatro roles iniciales: `ADMIN`, `RECEPTION`, `CASHIER` y `MAINTENANCE`.

## Verificación realizada

El script se probó una vez en una base local vacía `hotel_management`, comprobando antes `current_database()`.

```sql
SELECT count(*) FROM information_schema.tables
WHERE table_schema = 'hotel' AND table_type = 'BASE TABLE';
-- Resultado: 16

SELECT code, name FROM hotel.roles;
-- Resultado: ADMIN, RECEPTION, CASHIER, MAINTENANCE
```

## Observación sobre el conteo de tablas

La guía de coordinación indica 17 tablas, pero el script crea **16**. Queda por confirmar con el equipo si el dato de la guía es un error de conteo o si falta una tabla en el esquema.

## Limitaciones pendientes

- **Auditoría:** el esquema actual atribuye los cambios al creador; falta definir una auditoría que registre correctamente quién realiza cada modificación.
- **Concurrencia:** falta revisar los casos de concurrencia entre reservas y mantenimiento.
- **Validaciones entre hoteles:** faltan validaciones que garanticen la coherencia de datos entre distintos hoteles.

## Notas

- El SQL es la fuente de verdad del modelo. Las entidades y el `DbContext` se generarán después a partir de este esquema.
- No se versionan contraseñas ni cadenas de conexión.
