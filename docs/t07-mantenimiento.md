# T07 - Registro y cierre de mantenimientos

## Objetivo

Implementar el registro, consulta y cierre de órdenes de mantenimiento, garantizando que las habitaciones no puedan reservarse durante los periodos bloqueados.

## Implementación

- DTOs y validaciones de mantenimiento.
- Repositorio específico con transacciones.
- Commands y Queries mediante MediatR.
- Controlador REST sin lógica de negocio.
- Autorización para los roles ADMIN y MAINTENANCE.
- Integración con las restricciones de disponibilidad de R04.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | /api/maintenance | Listar mantenimientos |
| POST | /api/maintenance | Registrar mantenimiento |
| PATCH | /api/maintenance/{id}/close | Cerrar mantenimiento |

## Reglas de negocio

- La habitación debe existir y no estar inactiva.
- La fecha final debe ser posterior a la inicial.
- No se permiten mantenimientos con estadías abiertas.
- No se permiten solapamientos con reservas o mantenimientos activos.
- El costo debe ser positivo o cero, con un máximo de dos decimales.
- Solo se pueden cerrar mantenimientos activos.
- El cierre conserva el historial y libera el bloqueo de fechas.
- El estado operativo de la habitación no se modifica automáticamente.

## Corrección de PostgreSQL

Durante las pruebas se identificó el error 42P01 en la función fn_validate_maintenance, debido a la resolución del esquema de la tabla reservations.

Se agregó el script database/004_maintenance_trigger_search_path.sql para corregir el search_path de la función.

## Pruebas realizadas

| Escenario | Resultado |
|---|---|
| Disponibilidad inicial | Correcto |
| Registro de mantenimiento | 201 |
| Listado de mantenimientos | 200 |
| Bloqueo de disponibilidad | Correcto |
| Mantenimiento solapado | 409 |
| Reserva durante mantenimiento | 409 |
| Cierre de mantenimiento | 200 |
| Recuperación de disponibilidad | Correcto |
| Cierre repetido | 409 |
| Fechas inválidas | 400 |

Las diez pruebas funcionales fueron comprobadas manualmente mediante Swagger.

En PostgreSQL se verificó que los mantenimientos 3 y 4 conservaron su historial con estado COMPLETED y que sus bloqueos fueron eliminados de room_period_blocks.

## Compilación y pruebas automatizadas

- Compilación correcta.
- 36 pruebas automatizadas aprobadas.
- 0 pruebas fallidas.
- 8 pruebas omitidas por falta de configuración de R02_POSTGRES_ADMIN.

## Dependencias

T03, T06, C05 y R04 integrados.

## Integración posterior

El módulo permite continuar con Y09, correspondiente a la interfaz web de mantenimiento.