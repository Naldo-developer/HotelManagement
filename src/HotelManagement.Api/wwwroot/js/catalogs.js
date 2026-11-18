// Catálogos de la API con caché en memoria (hoteles y tipos de habitación por hotel).
import { api } from './api.js';

export const ROOM_STATUSES = {
    ACTIVE: 'Activa',
    OUT_OF_SERVICE: 'Fuera de servicio',
    INACTIVE: 'Inactiva',
};

let hotelsPromise = null;
const typesPromises = new Map();

export function getHotels() {
    if (!hotelsPromise) {
        hotelsPromise = api.get('/api/hotels').catch((error) => {
            hotelsPromise = null; // un fallo no se queda guardado
            throw error;
        });
    }
    return hotelsPromise;
}

export function getRoomTypes(hotelId) {
    const key = String(hotelId);
    if (!typesPromises.has(key)) {
        typesPromises.set(
            key,
            api.get('/api/room-types', { hotelId }).catch((error) => {
                typesPromises.delete(key);
                throw error;
            }),
        );
    }
    return typesPromises.get(key);
}

export function clearCatalogCache() {
    hotelsPromise = null;
    typesPromises.clear();
}

// ---- Y05: estados de reserva y traducción de IDs a nombres ----

export const RESERVATION_STATUSES = {
    PENDING: 'Pendiente',
    CONFIRMED: 'Confirmada',
    CHECKED_IN: 'Con check-in',
    COMPLETED: 'Completada',
    CANCELLED: 'Cancelada',
    NO_SHOW: 'No se presentó',
};

// Traduce IDs de hotel y tipo a nombres. Si fallan los catálogos, se muestran los ID.
export async function resolveNames(rooms) {
    const hotels = new Map();
    const types = new Map();
    try {
        (await getHotels()).forEach((hotel) => hotels.set(hotel.hotelId, hotel.name));
        const hotelIds = [...new Set(rooms.map((room) => room.hotelId))];
        const lists = await Promise.all(hotelIds.map((id) => getRoomTypes(id)));
        lists.forEach((list) => list.forEach((type) => types.set(type.roomTypeId, type.name)));
    } catch {
        // sin nombres: las pantallas usan los identificadores
    }
    return { hotels, types };
}

export const hotelName = (names, id) => names.hotels.get(id) ?? `ID ${id}`;
export const typeName = (names, id) => names.types.get(id) ?? `ID ${id}`;