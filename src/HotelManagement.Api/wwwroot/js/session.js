// Estado de la sesión del usuario (cookie hotel_session en el servidor).
import { api } from './api.js';
import { clearCatalogCache } from './catalogs.js';

let currentUser = null;

export const getUser = () => currentUser;

export function setUser(user) {
    currentUser = user;
    clearCatalogCache(); // los catálogos podrían variar según el usuario
}

// Devuelve el usuario con sesión activa o null si no hay sesión (401).
export async function fetchCurrentUser() {
    try {
        currentUser = await api.get('/api/auth/me');
    } catch (error) {
        if (error.status !== 401) {
            throw error;
        }
        currentUser = null;
    }
    return currentUser;
}

export async function logout() {
    try {
        await api.post('/api/auth/logout');
    } finally {
        setUser(null);
    }
}

// Sin códigos requeridos, la sección está abierta a cualquier usuario con sesión.
export function hasAnyRole(user, codes) {
    if (!codes || codes.length === 0) {
        return true;
    }
    return Boolean(user) && user.roles.some((role) => codes.includes(role));
}