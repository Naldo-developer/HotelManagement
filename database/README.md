# Base de datos – Sistema de gestión hotelera

Esta carpeta contiene el esquema inicial de PostgreSQL y datos ficticios de demo.

> **Estado:** primera versión para compartir y probar. **No es definitiva.**
> Los cambios posteriores se entregarán como scripts incrementales.

## Contenido

R04 agrega `003_room_period_integrity.sql`, aplicado una vez despues de 001.
Consultar [el contrato y la actualizacion R04](../docs/r04-integridad.md) antes
de actualizar una base con datos. No modifica ni reemplaza el esquema inicial.

| Archivo | Descripción |
| --- | --- |
| `002_demo_seed.sql` | Datos ficticios R01: un hotel, dos tipos, tres habitaciones activas y dos hu?spedes. Repetible. |
| `001_initial_schema.sql` | Esquema inicial: extensión, esquema `hotel`, 16 tablas, índices, triggers y roles iniciales. |

## Requisitos

- PostgreSQL instalado y en ejecución (puerto 5432 por defecto).
- Un usuario con permisos para crear extensiones (por ejemplo, `postgres`), porque el script ejecuta `CREATE EXTENSION IF NOT EXISTS btree_gist;`.
- Un cliente SQL: pgAdmin, DataGrip, Rider o `psql`.

## Orden de ejecución

Ejecutar los scripts en orden en **una base propia de demo vac?a**. El esquema se aplica una vez; el seed puede repetirse.

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

## R01: datos de demo

Despu?s del esquema, ejecutar el seed en la misma base. En una base nueva de
prueba, estos comandos aplican el esquema de forma at?mica y el seed dos veces.
Usar las credenciales locales mediante el prompt o un archivo pgpass; no guardarlas
en el repositorio. Cambiar `postgres` por el usuario local con permisos de creaci?n.

```powershell
createdb -h localhost -U postgres hotel_management_demo
psql -h localhost -U postgres -d hotel_management_demo -v ON_ERROR_STOP=1 -1 -f database/001_initial_schema.sql
psql -h localhost -U postgres -d hotel_management_demo -v ON_ERROR_STOP=1 -f database/002_demo_seed.sql
psql -h localhost -U postgres -d hotel_management_demo -v ON_ERROR_STOP=1 -f database/002_demo_seed.sql
```

El seed usa una transacci?n y las restricciones ?nicas existentes: tax_id del hotel
`DEMO-R01-HOTEL`, nombre del tipo dentro del hotel, n?mero de habitaci?n dentro del
hotel y documento del hu?sped. Resuelve las relaciones mediante esas claves sin
suponer IDs. `ON CONFLICT DO NOTHING` conserva los datos existentes; no actualiza
precios ni sobrescribe ediciones al repetirlo. Las secuencias pueden avanzar aunque
se omitan inserciones, por lo que los IDs no tienen que ser consecutivos.
No incluye usuarios ni contrase?as. Todos los nombres, documentos y direcciones
son ficticios; los correos usan el dominio reservado `example.invalid`.

En una base reci?n creada, comprobar tras cada ejecuci?n:

```sql
SELECT 'hotels' AS entidad, count(*) FROM hotel.hotels
UNION ALL SELECT 'room_types', count(*) FROM hotel.room_types
UNION ALL SELECT 'rooms', count(*) FROM hotel.rooms
UNION ALL SELECT 'guests', count(*) FROM hotel.guests;
-- hotels: 1; room_types: 2; rooms: 3; guests: 2.

SELECT r.room_number, t.name, r.operational_status
FROM hotel.rooms r
JOIN hotel.room_types t
  ON (r.room_type_id, r.hotel_id) = (t.room_type_id, t.hotel_id)
ORDER BY r.room_number;
-- DEMO-101 / Demo Simple; DEMO-102 y DEMO-103 / Demo Doble; todas ACTIVE.
```

Validaci?n R01 realizada con PostgreSQL 18.6 en una instancia temporal aislada:
esquema aplicado sobre base vac?a (16 tablas y 4 roles); seed aplicado dos veces,
comparando todas las columnas e IDs de las cuatro tablas entre ejecuciones.
Resultado: mismo conjunto de datos, sin registros duplicados, con relaciones
hotel/tipo/habitaci?n v?lidas. El esquema inicial permanece sin cambios.

## T07: corrección del trigger de mantenimiento

Ejecutar `004_maintenance_trigger_search_path.sql`
después de aplicar el script 003.

Corrige el error PostgreSQL 42P01 al registrar
mantenimientos desde la API.

La actualización conserva los datos existentes
y las reglas de integridad del esquema.