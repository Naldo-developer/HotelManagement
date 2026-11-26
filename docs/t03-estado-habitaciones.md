# Pruebas manuales — T03: Estado operativo de habitaciones

- **Endpoint:** `PATCH /api/rooms/{id}/operational-status`
- **Entorno:** Swagger UI autenticado como `ADMIN`
- **Suite automatizada (`dotnet test`):** 36 aprobadas, 8 omitidas por falta de `R02_POSTGRES_ADMIN` (sin pruebas automáticas nuevas agregadas en esta tarea).

## Casos ejecutados y respuestas reales

| # | Prueba | Solicitud | Código | Respuesta real |
| :--- | :--- | :--- | :---: | :--- |
| 1 | Desactivar habitación libre | `PATCH /api/rooms/4/operational-status` con `{"operationalStatus": "OUT_OF_SERVICE"}` | `200` | `{"roomId": 4, "hotelId": 1, "roomTypeId": 1, "roomNumber": "103", "floor": "1", "operationalStatus": "OUT_OF_SERVICE"}` |
| 2 | Persistencia del estado | `GET /api/rooms/4` | `200` | `{"roomId": 4, "hotelId": 1, "roomTypeId": 1, "roomNumber": "103", "floor": "1", "operationalStatus": "OUT_OF_SERVICE"}` |
| 3 | Exclusión en disponibilidad | `GET /api/rooms/availability?HotelId=1&CheckInDate=2026-12-01&CheckOutDate=2026-12-03` | `200` | Devuelve únicamente `roomId: 1` (`101`) y `roomId: 3` (`201`); `roomId: 4` no aparece. |
| 4 | Reactivar habitación | `PATCH /api/rooms/4/operational-status` con `{"operationalStatus": "ACTIVE"}` | `200` | `{"roomId": 4, "hotelId": 1, "roomTypeId": 1, "roomNumber": "103", "floor": "1", "operationalStatus": "ACTIVE"}` |
| 5 | Bloqueo con reserva activa | `PATCH /api/rooms/5/operational-status` con `{"operationalStatus": "INACTIVE"}` | `409` | `{"title": "Conflicto de negocio", "status": 409, "detail": "No se puede desactivar una habitación con reservas activas vigentes o futuras."}` |
| 6 | Bloqueo con estadía abierta | `PATCH /api/rooms/7/operational-status` con `{"operationalStatus": "OUT_OF_SERVICE"}` (con `reservationId: 6`, `stayId: 2` activa) | `409` | `{"title": "Conflicto de negocio", "status": 409, "detail": "No se puede desactivar una habitación con una estadía abierta."}` |
| 7 | Estado operativo inválido | `PATCH /api/rooms/4/operational-status` con `{"operationalStatus": "OCUPADA"}` | `400` | `{"title": "One or more validation errors occurred.", "status": 400, "errors": {"OperationalStatus": ["El estado debe ser ACTIVE, OUT_OF_SERVICE o INACTIVE."]}}` |
| 8 | Habitación inexistente | `PATCH /api/rooms/999999/operational-status` con `{"operationalStatus": "OUT_OF_SERVICE"}` | `404` | `{"title": "Recurso no encontrado", "status": 404, "detail": "La habitación no existe."}` |
