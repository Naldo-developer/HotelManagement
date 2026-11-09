\# Módulo de habitaciones



\## DTOs



\- `CreateRoomDto` contiene los datos para registrar una habitación.

\- `RoomDto` contiene los datos que devuelve la API al crear o consultar una habitación.

\- `RoomFilterDto` permite filtrar por hotel, tipo de habitación y estado operativo, y paginar los resultados.



\## Creación



`POST /api/rooms` registra una habitación. Si la creación tiene éxito, devuelve `201 Created` y el encabezado `Location` con la ruta para consultarla por ID.



\- `HotelId` y `RoomTypeId` deben ser positivos. El hotel debe existir y el tipo de habitación debe pertenecer a ese hotel.

\- `RoomNumber` es obligatorio, admite hasta 20 caracteres y debe ser único dentro del hotel.

\- `Floor` es opcional y admite hasta 20 caracteres.

\- `OperationalStatus` toma el valor `ACTIVE` si se omite. También admite `OUT\_OF\_SERVICE` e `INACTIVE`.



El estado operativo describe la condición de la habitación. Para conocer su disponibilidad en fechas concretas también se deben consultar las reservas.



\## Consultas



\- `GET /api/rooms` lista las habitaciones ordenadas por ID. Los filtros `HotelId`, `RoomTypeId` y `OperationalStatus` son opcionales y se pueden combinar.

\- Los IDs usados como filtros deben ser positivos. El estado debe ser uno de los tres valores permitidos.

\- `Page` empieza en 1. `PageSize` admite de 1 a 100 y su valor predeterminado es 10.

\- `GET /api/rooms/{id}` devuelve la habitación solicitada. Un ID menor o igual a cero devuelve `400` y un ID positivo inexistente devuelve `404`.



\## Verificación local



| Caso probado | Resultado observado |

|---|---|

| Listado, filtros combinados y dos páginas consecutivas | `200`. Se obtuvieron las habitaciones esperadas. |

| Consulta por ID | `200` para una habitación existente, `400` para el ID 0 y `404` para un ID inexistente. |

| Crear la habitación 103 del hotel 1 sin enviar estado | `201`. Se asignó el ID 4 y el estado `ACTIVE`. La ruta `/api/rooms/4` devolvió la habitación creada. |

| Repetir la creación de la habitación 103 en el hotel 1 | `409`. En PostgreSQL se verificó una sola fila para ese hotel y número. |

| Enviar el número vacío y el estado `OCUPADA` | `400`. La respuesta identificó ambos campos inválidos. |

| Enviar `HotelId` 99999 | `400`. El hotel indicado no existe. |

| Enviar `RoomTypeId` 99999 para el hotel 1 | `400`. El tipo de habitación debe existir y pertenecer al hotel indicado. |



Los endpoints se probaron manualmente con Swagger y PostgreSQL. Además, las 7 pruebas automatizadas existentes pasaron con `dotnet test HotelManagement.sln`.

