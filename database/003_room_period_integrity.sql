-- R04. Aplicar una vez, despues de 001 (002 puede aplicarse antes o despues).
-- La exclusion compartida protege tambien escrituras concurrentes entre tablas.
BEGIN;
SET LOCAL search_path TO hotel, public;
LOCK TABLE reservations, maintenance IN SHARE ROW EXCLUSIVE MODE;

CREATE TABLE room_period_blocks (
    reservation_id BIGINT UNIQUE REFERENCES reservations(reservation_id) ON DELETE CASCADE,
    maintenance_id BIGINT UNIQUE REFERENCES maintenance(maintenance_id) ON DELETE CASCADE,
    room_id BIGINT NOT NULL REFERENCES rooms(room_id) ON DELETE RESTRICT,
    period DATERANGE NOT NULL,
    CONSTRAINT ck_room_period_blocks_source CHECK (
        (reservation_id IS NOT NULL)::int + (maintenance_id IS NOT NULL)::int = 1
    ),
    CONSTRAINT ck_room_period_blocks_period CHECK (NOT isempty(period)),
    CONSTRAINT ex_room_period_blocks_overlap EXCLUDE USING gist (
        room_id WITH =, period WITH &&
    )
);

-- Falla y revierte todo si los datos previos contienen conflictos cruzados.
INSERT INTO room_period_blocks (reservation_id, room_id, period)
SELECT reservation_id, room_id, stay_period FROM reservations
WHERE status IN ('PENDING', 'CONFIRMED', 'CHECKED_IN');
INSERT INTO room_period_blocks (maintenance_id, room_id, period)
SELECT maintenance_id, room_id, maintenance_period FROM maintenance
WHERE status IN ('SCHEDULED', 'IN_PROGRESS');

CREATE FUNCTION hotel.fn_sync_reservation_period_block()
RETURNS TRIGGER LANGUAGE plpgsql SET search_path = hotel, pg_temp AS $$
BEGIN
    IF NEW.status IN ('PENDING', 'CONFIRMED', 'CHECKED_IN') THEN
        INSERT INTO room_period_blocks (reservation_id, room_id, period)
        VALUES (NEW.reservation_id, NEW.room_id, NEW.stay_period)
        ON CONFLICT (reservation_id) DO UPDATE
        SET room_id = EXCLUDED.room_id, period = EXCLUDED.period;
    ELSE
        DELETE FROM room_period_blocks WHERE reservation_id = NEW.reservation_id;
    END IF;
    RETURN NEW;
END;
$$;

CREATE FUNCTION hotel.fn_sync_maintenance_period_block()
RETURNS TRIGGER LANGUAGE plpgsql SET search_path = hotel, pg_temp AS $$
BEGIN
    IF NEW.status IN ('SCHEDULED', 'IN_PROGRESS') THEN
        INSERT INTO room_period_blocks (maintenance_id, room_id, period)
        VALUES (NEW.maintenance_id, NEW.room_id, NEW.maintenance_period)
        ON CONFLICT (maintenance_id) DO UPDATE
        SET room_id = EXCLUDED.room_id, period = EXCLUDED.period;
    ELSE
        DELETE FROM room_period_blocks WHERE maintenance_id = NEW.maintenance_id;
    END IF;
    RETURN NEW;
END;
$$;

-- AFTER permite leer las columnas de rango generadas. DELETE se propaga por FK.
CREATE TRIGGER trg_sync_reservation_period_block
AFTER INSERT OR UPDATE ON reservations
FOR EACH ROW EXECUTE FUNCTION hotel.fn_sync_reservation_period_block();
CREATE TRIGGER trg_sync_maintenance_period_block
AFTER INSERT OR UPDATE ON maintenance
FOR EACH ROW EXECUTE FUNCTION hotel.fn_sync_maintenance_period_block();
COMMIT;
