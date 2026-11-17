# R04: contrato de reservas y mantenimiento

## Aplicacion

SQL inicial sin cambios. En una base nueva aplicar `001_initial_schema.sql`,
`002_demo_seed.sql` y `003_room_period_integrity.sql`, en ese orden. En una copia
con datos previos aplicar solamente 003. Hacer respaldo y usar una ventana sin
escrituras: 003 toma bloqueos de tabla hasta terminar. Ejecutar con psql y
`-v ON_ERROR_STOP=1`. El script tiene su propia transaccion y se aplica una vez.
No se debe borrar ni recrear la base para actualizarla.

Si existen reservas activas y mantenimientos activos solapados, el backfill falla
con `23P01` y revierte toda la actualizacion. Revisar y resolver esos datos con
el equipo antes de repetir. No se cancelan registros automaticamente. Diagnostico:

```sql
SELECT r.reservation_id, m.maintenance_id, r.room_id
FROM hotel.reservations r JOIN hotel.maintenance m USING (room_id)
WHERE r.status IN ('PENDING', 'CONFIRMED', 'CHECKED_IN')
  AND m.status IN ('SCHEDULED', 'IN_PROGRESS')
  AND r.stay_period && m.maintenance_period;
```

## Garantias PostgreSQL

- Se conserva `ex_reservations_room_period`: una habitacion no admite reservas
  activas solapadas. Se conserva tambien la exclusion entre mantenimientos.
- La tabla interna `hotel.room_period_blocks` une los periodos activos de ambas
  tablas. Su exclusion `ex_room_period_blocks_overlap` evita tambien conflictos
  cruzados concurrentes. No depende de que un SELECT previo vea otra transaccion.
- Triggers AFTER sincronizan insercion, cambio de fechas, habitacion y estado.
  Al desactivar se libera el bloqueo; DELETE lo elimina mediante FK en cascada.
  Un conflicto revierte la escritura completa. Los rangos son `[entrada, salida)`.
- Los triggers originales conservan capacidad, estado operativo y validacion
  de mantenimiento. T04 ahora excluye mantenimiento SCHEDULED e IN_PROGRESS;
  una consulta de disponibilidad sigue siendo orientativa, no una reserva.

La tabla interna no se edita desde repositorios ni se trunca por separado. No se
deshabilitan triggers en operacion. No requiere entidad EF ni otro scaffolding.
Estos permisos deben respetarse al definir el usuario de despliegue.

## Contrato para Thomas: T05

R04 deja disponible la garantia de intervalos que T05 necesita, una vez fusionada
y aplicada 003 en su entorno. C01 y T04 ya estan en main. T05 aun debe implementar:

1. POST /api/reservations, DTO, validacion de huesped, habitacion, fechas y capacidad.
2. Una transaccion con reconsulta de disponibilidad y alta. Para coordinar T03 y
   cambios operativos, bloquear la habitacion antes de reconsultar; operaciones
   que cambien su estado deben usar el mismo protocolo. Si modifica varias,
   adquirir bloqueos en orden de room_id y mantener transacciones cortas.
3. Traducir `23P01` de ambas exclusiones de reservas/bloqueos a HTTP 409, sin
   exponer SQL. Los triggers originales lanzan P0001: no traducir cualquier
   error de base indiscriminadamente; validar reglas conocidas en aplicacion.
4. Revertir toda la transaccion ante conflicto. Si se usan aislamientos superiores
   a READ COMMITTED o varias filas, tratar 40001/40P01 con reintentos acotados de
   la transaccion completa. Probar dos POST simultaneos: un 201 y un 409.

T05 puede desarrollarse sobre este contrato. Para integrarlo/habilitarlo falta
fusionar R04, aplicar 003 y completar su endpoint y pruebas; no falta una decision
de pagos de Charly. Este PR no implementa T05 ni las politicas C04.

## Decisiones y coordinacion pendientes

- Charly: payments.reference es nullable y no unica. Acordar clave de reintento,
  unicidad global/por reserva/por hotel/proveedor, tratamiento de nulos, anulados
  y reembolsos. No se crea una restriccion arbitraria en R04.
- Thomas y Charly: cancelacion y check-in deben bloquear la misma reserva o usar
  transiciones condicionadas. UNIQUE(stays.reservation_id) ya evita dos estadias
  de una reserva, pero no garantiza por si sola ocupacion unica por habitacion.
- Charly: check-in debe coordinar ocupacion real; consumos, pagos y check-out
  deben compartir bloqueo y verificar saldo dentro de una transaccion. Acordar
  calculo de noches, reembolsos y autor real del historial antes de ampliar DDL.
- Thomas: T03 debe proteger cambios operativos frente a reservas futuras y estadias
  abiertas. R04 no cambia estados automaticamente ni resuelve ese caso de uso.

## Verificacion reproducible

Usar PostgreSQL temporal y `R02_POSTGRES_ADMIN` segun el README de pruebas;
ejecutar `dotnet test HotelManagement.sln`. La suite incluye instalacion limpia,
actualizacion con datos previos (comparacion de datos y esquema), rechazo atomico
de datos heredados incompatibles, disponibilidad con mantenimiento y limite
adyacente, liberacion por estado/DELETE y conflicto concurrente en ambos ordenes.
La prueba R02 aplica tambien 003 y verifica persistencia HTTP con un contexto nuevo.
