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