-- ============================================================
-- SISTEMA DE GESTIÓN HOTELERA
-- PostgreSQL
-- ============================================================

-- Ejecutar conectado a la base de datos del proyecto.
-- Ejemplo de nombre de BD:
-- hotel_management

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE SCHEMA IF NOT EXISTS hotel;

SET search_path TO hotel, public;

-- ============================================================
-- HOTEL
-- ============================================================

CREATE TABLE hotels (
    hotel_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    name VARCHAR(120) NOT NULL,
    tax_id VARCHAR(30),
    address VARCHAR(200),
    phone VARCHAR(30),
    email VARCHAR(120),

    currency_code CHAR(3) NOT NULL DEFAULT 'PEN',
    timezone VARCHAR(50) NOT NULL DEFAULT 'America/Lima',

    active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_hotels_tax_id UNIQUE (tax_id)
);


-- ============================================================
-- TIPOS DE HABITACIÓN
-- ============================================================

CREATE TABLE room_types (
    room_type_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    hotel_id BIGINT NOT NULL,

    name VARCHAR(80) NOT NULL,
    description TEXT,

    max_adults SMALLINT NOT NULL DEFAULT 1,
    max_children SMALLINT NOT NULL DEFAULT 0,

    base_rate NUMERIC(12,2) NOT NULL,

    active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_room_types_hotel
        FOREIGN KEY (hotel_id)
        REFERENCES hotels(hotel_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_room_types_max_adults
        CHECK (max_adults > 0),

    CONSTRAINT ck_room_types_max_children
        CHECK (max_children >= 0),

    CONSTRAINT ck_room_types_base_rate
        CHECK (base_rate >= 0),

    CONSTRAINT uq_room_types_hotel_name
        UNIQUE (hotel_id, name),

    CONSTRAINT uq_room_types_room_type_hotel
        UNIQUE (room_type_id, hotel_id)
);


-- ============================================================
-- HABITACIONES
-- ============================================================

CREATE TABLE rooms (
    room_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    hotel_id BIGINT NOT NULL,
    room_type_id BIGINT NOT NULL,

    room_number VARCHAR(20) NOT NULL,
    floor VARCHAR(20),

    operational_status VARCHAR(20)
        NOT NULL
        DEFAULT 'ACTIVE',

    notes TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_rooms_room_type_hotel
        FOREIGN KEY (room_type_id, hotel_id)
        REFERENCES room_types(room_type_id, hotel_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_rooms_operational_status
        CHECK (
            operational_status IN (
                'ACTIVE',
                'OUT_OF_SERVICE',
                'INACTIVE'
            )
        ),

    CONSTRAINT uq_rooms_hotel_number
        UNIQUE (hotel_id, room_number)
);


-- ============================================================
-- HUÉSPEDES
-- ============================================================

CREATE TABLE guests (
    guest_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    document_type VARCHAR(20),
    document_number VARCHAR(30),

    first_name VARCHAR(80) NOT NULL,
    last_name VARCHAR(100) NOT NULL,

    email VARCHAR(120),
    phone VARCHAR(30),

    birth_date DATE,
    nationality VARCHAR(80),
    address VARCHAR(200),

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_guests_document
        UNIQUE (document_type, document_number)
);


-- ============================================================
-- EMPLEADOS
-- ============================================================

CREATE TABLE employees (
    employee_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    hotel_id BIGINT NOT NULL,

    document_type VARCHAR(20),
    document_number VARCHAR(30),

    first_name VARCHAR(80) NOT NULL,
    last_name VARCHAR(100) NOT NULL,

    position VARCHAR(80),

    email VARCHAR(120),
    phone VARCHAR(30),

    active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_employees_hotel
        FOREIGN KEY (hotel_id)
        REFERENCES hotels(hotel_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_employees_document
        UNIQUE (document_type, document_number)
);


-- ============================================================
-- USUARIOS DEL SISTEMA
-- ============================================================

CREATE TABLE app_users (
    user_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    employee_id BIGINT NOT NULL,

    username VARCHAR(60) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,

    active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMPTZ,

    CONSTRAINT fk_app_users_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_app_users_employee
        UNIQUE (employee_id),

    CONSTRAINT uq_app_users_username
        UNIQUE (username)
);


-- ============================================================
-- ROLES
-- ============================================================

CREATE TABLE roles (
    role_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    code VARCHAR(40) NOT NULL,
    name VARCHAR(80) NOT NULL,
    description TEXT,

    CONSTRAINT uq_roles_code UNIQUE (code)
);


CREATE TABLE user_roles (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,

    PRIMARY KEY (user_id, role_id),

    CONSTRAINT fk_user_roles_user
        FOREIGN KEY (user_id)
        REFERENCES app_users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_roles_role
        FOREIGN KEY (role_id)
        REFERENCES roles(role_id)
        ON DELETE CASCADE
);


-- ============================================================
-- RESERVAS
-- ============================================================

CREATE TABLE reservations (
    reservation_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    confirmation_code VARCHAR(30) NOT NULL,

    room_id BIGINT NOT NULL,
    primary_guest_id BIGINT NOT NULL,

    check_in_date DATE NOT NULL,
    check_out_date DATE NOT NULL,

    adults SMALLINT NOT NULL DEFAULT 1,
    children SMALLINT NOT NULL DEFAULT 0,

    -- Precio acordado al momento de realizar la reserva.
    nightly_rate NUMERIC(12,2) NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',

    cancellation_fee NUMERIC(12,2) NOT NULL DEFAULT 0,
    cancellation_reason TEXT,

    special_requests TEXT,

    created_by_user_id BIGINT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    stay_period DATERANGE
        GENERATED ALWAYS AS (
            daterange(check_in_date, check_out_date, '[)')
        ) STORED,

    CONSTRAINT fk_reservations_room
        FOREIGN KEY (room_id)
        REFERENCES rooms(room_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_reservations_primary_guest
        FOREIGN KEY (primary_guest_id)
        REFERENCES guests(guest_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_reservations_created_by
        FOREIGN KEY (created_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_reservations_confirmation_code
        UNIQUE (confirmation_code),

    CONSTRAINT ck_reservations_dates
        CHECK (check_out_date > check_in_date),

    CONSTRAINT ck_reservations_adults
        CHECK (adults > 0),

    CONSTRAINT ck_reservations_children
        CHECK (children >= 0),

    CONSTRAINT ck_reservations_rate
        CHECK (nightly_rate >= 0),

    CONSTRAINT ck_reservations_cancellation_fee
        CHECK (cancellation_fee >= 0),

    CONSTRAINT ck_reservations_status
        CHECK (
            status IN (
                'PENDING',
                'CONFIRMED',
                'CHECKED_IN',
                'COMPLETED',
                'CANCELLED',
                'NO_SHOW'
            )
        ),

    -- Evita que una misma habitación tenga reservas
    -- activas cuyos periodos se crucen.
    CONSTRAINT ex_reservations_room_period
        EXCLUDE USING gist (
            room_id WITH =,
            stay_period WITH &&
        )
        WHERE (
            status IN (
                'PENDING',
                'CONFIRMED',
                'CHECKED_IN'
            )
        )
);


-- ============================================================
-- HUÉSPEDES ASOCIADOS A UNA RESERVA
-- ============================================================

CREATE TABLE reservation_guests (
    reservation_id BIGINT NOT NULL,
    guest_id BIGINT NOT NULL,

    PRIMARY KEY (reservation_id, guest_id),

    CONSTRAINT fk_reservation_guests_reservation
        FOREIGN KEY (reservation_id)
        REFERENCES reservations(reservation_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_reservation_guests_guest
        FOREIGN KEY (guest_id)
        REFERENCES guests(guest_id)
        ON DELETE RESTRICT
);


-- ============================================================
-- ESTADÍAS
-- ============================================================

CREATE TABLE stays (
    stay_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    reservation_id BIGINT NOT NULL,

    check_in_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    check_out_at TIMESTAMPTZ,

    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',

    check_in_by_user_id BIGINT,
    check_out_by_user_id BIGINT,

    notes TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_stays_reservation
        FOREIGN KEY (reservation_id)
        REFERENCES reservations(reservation_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_stays_check_in_user
        FOREIGN KEY (check_in_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT fk_stays_check_out_user
        FOREIGN KEY (check_out_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_stays_reservation
        UNIQUE (reservation_id),

    CONSTRAINT ck_stays_status
        CHECK (
            status IN (
                'ACTIVE',
                'COMPLETED'
            )
        ),

    CONSTRAINT ck_stays_checkout_after_checkin
        CHECK (
            check_out_at IS NULL
            OR check_out_at >= check_in_at
        )
);


-- ============================================================
-- SERVICIOS
-- ============================================================

CREATE TABLE services (
    service_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    hotel_id BIGINT NOT NULL,

    name VARCHAR(100) NOT NULL,
    description TEXT,

    default_price NUMERIC(12,2) NOT NULL DEFAULT 0,

    unit VARCHAR(30) NOT NULL DEFAULT 'UNIT',

    active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_services_hotel
        FOREIGN KEY (hotel_id)
        REFERENCES hotels(hotel_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_services_price
        CHECK (default_price >= 0),

    CONSTRAINT uq_services_hotel_name
        UNIQUE (hotel_id, name)
);


-- ============================================================
-- CONSUMOS
-- ============================================================

CREATE TABLE consumptions (
    consumption_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    stay_id BIGINT NOT NULL,
    service_id BIGINT NOT NULL,

    quantity NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_price NUMERIC(12,2) NOT NULL,

    subtotal NUMERIC(14,2)
        GENERATED ALWAYS AS (
            quantity * unit_price
        ) STORED,

    consumed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    recorded_by_user_id BIGINT,

    notes TEXT,

    CONSTRAINT fk_consumptions_stay
        FOREIGN KEY (stay_id)
        REFERENCES stays(stay_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_consumptions_service
        FOREIGN KEY (service_id)
        REFERENCES services(service_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_consumptions_user
        FOREIGN KEY (recorded_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT ck_consumptions_quantity
        CHECK (quantity > 0),

    CONSTRAINT ck_consumptions_unit_price
        CHECK (unit_price >= 0)
);


-- ============================================================
-- PAGOS
-- ============================================================

CREATE TABLE payments (
    payment_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    reservation_id BIGINT NOT NULL,

    transaction_type VARCHAR(20)
        NOT NULL
        DEFAULT 'PAYMENT',

    amount NUMERIC(12,2) NOT NULL,

    payment_method VARCHAR(30) NOT NULL,

    status VARCHAR(20)
        NOT NULL
        DEFAULT 'COMPLETED',

    reference VARCHAR(100),

    paid_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    recorded_by_user_id BIGINT,

    notes TEXT,

    CONSTRAINT fk_payments_reservation
        FOREIGN KEY (reservation_id)
        REFERENCES reservations(reservation_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_payments_user
        FOREIGN KEY (recorded_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT ck_payments_transaction_type
        CHECK (
            transaction_type IN (
                'PAYMENT',
                'REFUND'
            )
        ),

    CONSTRAINT ck_payments_amount
        CHECK (amount > 0),

    CONSTRAINT ck_payments_method
        CHECK (
            payment_method IN (
                'CASH',
                'CARD',
                'TRANSFER',
                'DIGITAL_WALLET',
                'OTHER'
            )
        ),

    CONSTRAINT ck_payments_status
        CHECK (
            status IN (
                'PENDING',
                'COMPLETED',
                'VOIDED'
            )
        )
);


-- ============================================================
-- MANTENIMIENTOS
-- ============================================================

CREATE TABLE maintenance (
    maintenance_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    room_id BIGINT NOT NULL,

    start_date DATE NOT NULL,
    end_date DATE NOT NULL,

    category VARCHAR(50),

    description TEXT NOT NULL,

    status VARCHAR(20)
        NOT NULL
        DEFAULT 'SCHEDULED',

    reported_by_user_id BIGINT,
    assigned_employee_id BIGINT,

    cost NUMERIC(12,2) NOT NULL DEFAULT 0,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMPTZ,

    maintenance_period DATERANGE
        GENERATED ALWAYS AS (
            daterange(start_date, end_date, '[)')
        ) STORED,

    CONSTRAINT fk_maintenance_room
        FOREIGN KEY (room_id)
        REFERENCES rooms(room_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_maintenance_reported_user
        FOREIGN KEY (reported_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT fk_maintenance_employee
        FOREIGN KEY (assigned_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL,

    CONSTRAINT ck_maintenance_dates
        CHECK (end_date > start_date),

    CONSTRAINT ck_maintenance_cost
        CHECK (cost >= 0),

    CONSTRAINT ck_maintenance_status
        CHECK (
            status IN (
                'SCHEDULED',
                'IN_PROGRESS',
                'COMPLETED',
                'CANCELLED'
            )
        ),

    -- Impide crear dos mantenimientos activos
    -- simultáneos para la misma habitación.
    CONSTRAINT ex_maintenance_room_period
        EXCLUDE USING gist (
            room_id WITH =,
            maintenance_period WITH &&
        )
        WHERE (
            status IN (
                'SCHEDULED',
                'IN_PROGRESS'
            )
        )
);


-- ============================================================
-- HISTORIAL DE ESTADOS DE RESERVA
-- ============================================================

CREATE TABLE reservation_status_history (
    history_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    reservation_id BIGINT NOT NULL,

    old_status VARCHAR(20),
    new_status VARCHAR(20) NOT NULL,

    changed_by_user_id BIGINT,

    changed_at TIMESTAMPTZ
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    notes TEXT,

    CONSTRAINT fk_reservation_history_reservation
        FOREIGN KEY (reservation_id)
        REFERENCES reservations(reservation_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_reservation_history_user
        FOREIGN KEY (changed_by_user_id)
        REFERENCES app_users(user_id)
        ON DELETE SET NULL
);


-- ============================================================
-- ÍNDICES
-- ============================================================

CREATE INDEX idx_rooms_type
    ON rooms(room_type_id);

CREATE INDEX idx_reservations_guest
    ON reservations(primary_guest_id);

CREATE INDEX idx_reservations_room
    ON reservations(room_id);

CREATE INDEX idx_reservations_dates
    ON reservations(check_in_date, check_out_date);

CREATE INDEX idx_reservations_status
    ON reservations(status);

CREATE INDEX idx_stays_status
    ON stays(status);

CREATE INDEX idx_consumptions_stay
    ON consumptions(stay_id);

CREATE INDEX idx_payments_reservation
    ON payments(reservation_id);

CREATE INDEX idx_maintenance_room
    ON maintenance(room_id);

CREATE INDEX idx_maintenance_dates
    ON maintenance(start_date, end_date);


-- ============================================================
-- TRIGGER:
-- updated_at automático para reservas
-- ============================================================

CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_reservations_updated_at
BEFORE UPDATE ON reservations
FOR EACH ROW
EXECUTE FUNCTION fn_set_updated_at();


-- ============================================================
-- VALIDACIÓN DE RESERVA:
-- capacidad, habitación operativa y mantenimiento
-- ============================================================

CREATE OR REPLACE FUNCTION fn_validate_reservation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_room_status VARCHAR(20);
    v_max_adults SMALLINT;
    v_max_children SMALLINT;
BEGIN

    SELECT
        r.operational_status,
        rt.max_adults,
        rt.max_children
    INTO
        v_room_status,
        v_max_adults,
        v_max_children
    FROM rooms r
    INNER JOIN room_types rt
        ON rt.room_type_id = r.room_type_id
    WHERE r.room_id = NEW.room_id;


    IF NOT FOUND THEN
        RAISE EXCEPTION
            'La habitación % no existe.',
            NEW.room_id;
    END IF;


    IF NEW.status IN (
        'PENDING',
        'CONFIRMED',
        'CHECKED_IN'
    ) THEN

        IF v_room_status <> 'ACTIVE' THEN
            RAISE EXCEPTION
                'La habitación % no se encuentra operativa.',
                NEW.room_id;
        END IF;


        IF NEW.adults > v_max_adults THEN
            RAISE EXCEPTION
                'La cantidad de adultos supera la capacidad de la habitación.';
        END IF;


        IF NEW.children > v_max_children THEN
            RAISE EXCEPTION
                'La cantidad de niños supera la capacidad de la habitación.';
        END IF;


        IF EXISTS (
            SELECT 1
            FROM maintenance m
            WHERE m.room_id = NEW.room_id
              AND m.status IN (
                    'SCHEDULED',
                    'IN_PROGRESS'
              )
              AND daterange(
                    NEW.check_in_date,
                    NEW.check_out_date,
                    '[)'
              ) && m.maintenance_period
        ) THEN

            RAISE EXCEPTION
                'La habitación % posee mantenimiento durante el periodo solicitado.',
                NEW.room_id;

        END IF;

    END IF;


    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_validate_reservation
BEFORE INSERT OR UPDATE
ON reservations
FOR EACH ROW
EXECUTE FUNCTION fn_validate_reservation();


-- ============================================================
-- VALIDACIÓN DE MANTENIMIENTO:
-- no permitir mantenimiento que choque con reservas
-- ============================================================

CREATE OR REPLACE FUNCTION fn_validate_maintenance()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN

    IF NEW.status IN (
        'SCHEDULED',
        'IN_PROGRESS'
    ) THEN

        IF EXISTS (
            SELECT 1
            FROM reservations r
            WHERE r.room_id = NEW.room_id
              AND r.status IN (
                    'PENDING',
                    'CONFIRMED',
                    'CHECKED_IN'
              )
              AND r.stay_period &&
                  daterange(
                      NEW.start_date,
                      NEW.end_date,
                      '[)'
                  )
        ) THEN

            RAISE EXCEPTION
                'No se puede programar mantenimiento: existen reservas activas para la habitación % durante ese periodo.',
                NEW.room_id;

        END IF;

    END IF;


    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_validate_maintenance
BEFORE INSERT OR UPDATE
ON maintenance
FOR EACH ROW
EXECUTE FUNCTION fn_validate_maintenance();


-- ============================================================
-- HISTORIAL AUTOMÁTICO DE ESTADOS DE RESERVA
-- ============================================================

CREATE OR REPLACE FUNCTION fn_reservation_status_history()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN

    IF OLD.status IS DISTINCT FROM NEW.status THEN

        INSERT INTO reservation_status_history (
            reservation_id,
            old_status,
            new_status,
            changed_by_user_id
        )
        VALUES (
            NEW.reservation_id,
            OLD.status,
            NEW.status,
            NEW.created_by_user_id
        );

    END IF;

    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_reservation_status_history
AFTER UPDATE OF status
ON reservations
FOR EACH ROW
EXECUTE FUNCTION fn_reservation_status_history();


-- ============================================================
-- ROLES INICIALES SUGERIDOS
-- ============================================================

INSERT INTO roles (
    code,
    name,
    description
)
VALUES
(
    'ADMIN',
    'Administrador',
    'Administración general del sistema.'
),
(
    'RECEPTION',
    'Recepción',
    'Gestión de huéspedes, reservas, check-in y check-out.'
),
(
    'CASHIER',
    'Caja',
    'Gestión de pagos y operaciones económicas.'
),
(
    'MAINTENANCE',
    'Mantenimiento',
    'Gestión de incidencias y mantenimiento de habitaciones.'
)
ON CONFLICT (code) DO NOTHING;
