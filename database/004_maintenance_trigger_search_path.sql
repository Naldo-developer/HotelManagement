-- T07: corregir la resolución del esquema
-- en el trigger de validación de mantenimiento.
-- Aplicar después de 003_room_period_integrity.sql.

BEGIN;

ALTER FUNCTION hotel.fn_validate_maintenance()
    SET search_path TO hotel, pg_temp;

COMMIT;