\# Módulo de habitaciones



\## DTOs

\- CreateRoomDto: datos de entrada para crear una habitación.

\- RoomDto: datos de respuesta al consultar una habitación.

\- RoomFilterDto: filtros por hotel, estado y paginación.



\## Validaciones

\- HotelId y RoomTypeId deben ser positivos al crear una habitación.

\- RoomNumber es obligatorio y admite hasta 20 caracteres.

\- Floor es opcional y admite hasta 20 caracteres.

\- Los filtros HotelId y OperationalStatus son opcionales.

\- Si se indica HotelId como filtro, debe ser positivo.

\- Si se indica un estado, debe coincidir con uno de los estados permitidos.

\- Page debe ser como mínimo 1; su valor inicial es 1.

\- PageSize admite entre 1 y 100; su valor inicial es 10.



\## Estados operativos

\- ACTIVE: habitación activa.

\- OUT\_OF\_SERVICE: habitación fuera de servicio.

\- INACTIVE: habitación inactiva.



Una habitación activa no necesariamente está disponible.

La disponibilidad requiere comprobar reservas y fechas.



\## Pendiente

Integrar el acceso a PostgreSQL y verificar que el hotel y el tipo

de habitación existan y correspondan entre sí. También comprobar

que el número de habitación no se repita dentro del mismo hotel.

