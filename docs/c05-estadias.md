# C05: apertura y consulta de estadías

## Endpoints

Todos requieren la política Operations: ADMIN o RECEPTION.

- POST /api/stays/check-in
- GET /api/stays/{id}
- GET /api/stays/by-reservation/{reservationId}

El check-in recibe reservationId y notes opcional (máximo 1000
caracteres). Devuelve 201 con la estadía y Location hacia su detalle.
El usuario responsable se obtiene de la sesión autenticada.

## Reglas

- Solo admiten check-in las reservas PENDING o CONFIRMED.
- La fecha local del hotel debe estar dentro de
  [check_in_date, check_out_date).
- La habitación debe estar operativa; hotel y tipo deben estar activos.
- Se revalida la capacidad y la ausencia de mantenimiento incompatible.
- Una reserva solo puede tener una estadía.
- Una habitación con una estadía abierta rechaza otro check-in,
  aunque el periodo reservado de la estadía anterior haya terminado.
- El check-in crea la estadía ACTIVE y cambia la reserva a CHECKED_IN.
- La habitación conserva su estado operativo ACTIVE.
- Los conflictos de negocio devuelven 409.

## Transacción y coordinación con T06

Se utiliza READ COMMITTED y SELECT FOR UPDATE.

Orden de bloqueo de C05:
1. Reserva.
2. Habitación.

Las validaciones de estado y ocupación se realizan después de
obtener los bloqueos correspondientes. Ambos cambios se confirman
en una sola transacción.

T06 bloquea la misma reserva:
- Si la cancelación confirma primero, el check-in se rechaza.
- Si el check-in confirma primero, la cancelación se rechaza.

Los nuevos procesos que bloqueen reservas y habitaciones deben
revisar este orden para evitar interbloqueos.

## Protección de edición de habitaciones

RoomRepository.UpdateAsync bloquea la habitación dentro de una
transacción. Si tiene cualquier reserva registrada, rechaza cambiar
su hotel o tipo, incluyendo reservas históricas.

La edición del número y del piso sigue permitida, conservando
las validaciones existentes.

## Preparación

Aplicar los scripts de database según su README:
001_initial_schema.sql, 002_demo_seed.sql y
003_room_period_integrity.sql.

C05 no agrega una migración SQL.

## Verificación realizada — 18/11/2026

Suite ejecutada con PostgreSQL temporal y R02_POSTGRES_ADMIN:
44 pruebas aprobadas, 0 fallidas y 0 omitidas.

Las cinco pruebas nuevas verifican:
- Dos check-in concurrentes de una reserva crean una sola estadía.
- Cancelación primero: el check-in se rechaza.
- Check-in primero: la cancelación se rechaza.
- Una estadía anterior abierta impide otra ocupación.
- Cambiar el tipo de una habitación reservada se rechaza sin cambios
  parciales; editar únicamente el piso sí persiste.

Las pruebas concurrentes usan repositorios reales, conexiones
independientes y detección de bloqueos en PostgreSQL.

Verificación manual en Swagger:
- Creación y consulta de una estadía.
- Check-in repetido: 409.
- Cancelación posterior al check-in: 409.
- Consulta sin sesión: 401.
- CASHIER consultando o intentando check-in: 403.
- RECEPTION consultando la estadía: acceso permitido.

## Continuación

C06 incorporará consumos; C07, pagos; C08, check-out.
La interfaz de estadías corresponde a Y06.

El responsable del check-in queda en stays.check_in_by_user_id.
El trigger existente de reservation_status_history continúa usando
created_by_user_id; no representa necesariamente al autor del cambio.
C05 no modifica ese mecanismo de auditoría.