# T06: consulta y cancelación de reservas

## Funcionalidad

Se agregan las siguientes operaciones:

- GET /api/reservations: listado con filtros y paginación.
- GET /api/reservations/{id}: detalle de una reserva.
- PATCH /api/reservations/{id}/cancel: cancelación con motivo.

El POST existente devuelve 201 con Location hacia el detalle.

El listado admite HotelId, RoomId, PrimaryGuestId y Status.
Page inicia en 1 y PageSize admite valores entre 1 y 100.
La respuesta es una lista ordenada por ReservationId descendente.

El detalle y la cancelación incluyen CancellationReason.

## Reglas de cancelación

- Solo se cancelan reservas PENDING o CONFIRMED.
- Una reserva con una estadía asociada rechaza la cancelación.
- El motivo es obligatorio y admite hasta 500 caracteres.
- La reserva se conserva con estado CANCELLED.
- Repetir la cancelación devuelve 409.
- Un ID positivo inexistente devuelve 404.
- Los datos inválidos devuelven 400.

La cancelación utiliza una transacción READ COMMITTED y bloquea
la fila de la reserva mediante SELECT FOR UPDATE.

El estado y la existencia de una estadía se comprueban después
de obtener el bloqueo. El bloqueo se conserva hasta finalizar
la transacción.

Los triggers existentes registran el cambio de estado y liberan
el bloqueo del periodo. El entorno debe tener aplicados los
scripts 001, 002 y 003 según las instrucciones de database/README.md.

## Contrato para C05: check-in

El check-in debe coordinarse con la cancelación de esta manera:

1. Iniciar una transacción.
2. Bloquear la misma fila de hotel.reservations mediante
   SELECT FOR UPDATE, usando reservation_id.
3. Leer y validar el estado después de obtener el bloqueo.
   No utilizar un estado leído antes de la transacción.
4. Rechazar reservas CANCELLED y las demás transiciones no permitidas.
5. Validar las reglas propias del check-in y crear la estadía.
6. Cambiar la reserva a CHECKED_IN dentro de la misma transacción.
7. Confirmar ambos cambios juntos o revertirlos ante un error.

Si la cancelación confirma primero, el check-in debe encontrar
CANCELLED y rechazar la operación.

Si el check-in confirma primero, la cancelación debe encontrar
CHECKED_IN o la estadía asociada y devolver 409.

El bloqueo de la reserva no resuelve por sí solo la ocupación
simultánea de una habitación por reservas diferentes. C05 debe
implementar esa protección y documentar un orden consistente
cuando necesite bloquear también habitaciones.

Antes de habilitar C05 se debe probar la ejecución simultánea
de check-in y cancelación en ambos órdenes.

## Contrato para Y05: interfaz de reservas

- Usar los filtros y la paginación del listado.
- Enviar cancellationReason en el cuerpo del PATCH.
- Actualizar la información visible después de cancelar.
- Mostrar el mensaje de ProblemDetails ante un conflicto.
- Consultar nuevamente disponibilidad después de una cancelación.

## Verificación realizada

Compilación:
- dotnet build: correcto.

Suite existente:
- 22 pruebas aprobadas.
- 3 pruebas omitidas por falta de R02_POSTGRES_ADMIN.
- Este cambio no incorpora pruebas automatizadas nuevas.

Pruebas manuales con Swagger, PowerShell y PostgreSQL:
- Listado y detalle: 200.
- Cancelación válida: 200 y persistencia del motivo.
- Segunda cancelación: 409.
- Habitación disponible nuevamente para el intervalo cancelado.
- Historial con transición a CANCELLED.
- Reserva con CHECKED_IN y estadía simulada: cancelación rechazada
  con 409 y conservación del estado.
- Limpieza de la estadía simulada y cancelación final de la reserva.
- Filtro por hotel y estado: resultado esperado.
- Estado inválido, Page igual a cero e ID igual a cero: 400.
- ID positivo inexistente: 404.

El escenario de check-in se preparó mediante SQL sobre datos de
prueba. No constituye una prueba del endpoint C05 ni una prueba
de concurrencia entre ambos endpoints.

## Pendientes de pruebas

- Automatizar los casos de T06 en la revisión de cobertura acordada.
- Ejecutar la suite PostgreSQL con R02_POSTGRES_ADMIN configurado.
- Verificar concurrencia con el check-in real al integrar C05.