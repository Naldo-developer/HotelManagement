-- Datos ficticios exclusivos de demo. Aplicar después de 001_initial_schema.sql.
-- Las claves naturales permiten repetirlo sin modificar registros existentes.
BEGIN;
SET LOCAL search_path TO hotel, public;

INSERT INTO hotels (name, tax_id, address, email)
VALUES ('Hotel Demo Aurora', 'DEMO-R01-HOTEL', 'Avenida Ficticia 123, Lima', 'hotel@example.invalid')
ON CONFLICT (tax_id) DO NOTHING;

INSERT INTO room_types (hotel_id, name, description, max_adults, max_children, base_rate)
SELECT h.hotel_id, v.name, v.description, v.adults, v.children, v.rate
FROM hotels h
CROSS JOIN (VALUES
    ('Demo Simple', 'Habitación ficticia individual', 1, 0, 120.00),
    ('Demo Doble', 'Habitación ficticia para dos adultos', 2, 1, 200.00)
) AS v(name, description, adults, children, rate)
WHERE h.tax_id = 'DEMO-R01-HOTEL'
ON CONFLICT (hotel_id, name) DO NOTHING;

INSERT INTO rooms (hotel_id, room_type_id, room_number, floor, operational_status, notes)
SELECT h.hotel_id, t.room_type_id, v.number, '1', 'ACTIVE', 'Datos ficticios R01'
FROM hotels h
JOIN room_types t ON t.hotel_id = h.hotel_id
JOIN (VALUES
    ('Demo Simple', 'DEMO-101'),
    ('Demo Doble', 'DEMO-102'),
    ('Demo Doble', 'DEMO-103')
) AS v(type_name, number) ON v.type_name = t.name
WHERE h.tax_id = 'DEMO-R01-HOTEL'
ON CONFLICT (hotel_id, room_number) DO NOTHING;

INSERT INTO guests (document_type, document_number, first_name, last_name, email)
VALUES
    ('DEMO', 'R01-GUEST-001', 'Ana Demo', 'Ejemplo', 'ana@example.invalid'),
    ('DEMO', 'R01-GUEST-002', 'Luis Demo', 'Ejemplo', 'luis@example.invalid')
ON CONFLICT (document_type, document_number) DO NOTHING;

COMMIT;
