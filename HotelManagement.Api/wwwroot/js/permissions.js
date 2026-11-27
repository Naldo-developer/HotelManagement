import { getUser, hasAnyRole } from './session.js';

export const ROLES = Object.freeze({
    ADMIN: 'ADMIN',
    RECEPTION: 'RECEPTION',
    CASHIER: 'CASHIER',
    MAINTENANCE: 'MAINTENANCE'
});

export const PERMISSIONS = Object.freeze({
    OPERATIONS: Object.freeze([
        ROLES.ADMIN,
        ROLES.RECEPTION
    ]),

    CONFIGURATION: Object.freeze([
        ROLES.ADMIN
    ]),

    FINANCIAL: Object.freeze([
        ROLES.ADMIN,
        ROLES.CASHIER
    ]),

    MAINTENANCE: Object.freeze([
        ROLES.ADMIN,
        ROLES.MAINTENANCE
    ])
});

export const ROLE_LABELS = Object.freeze({
    ADMIN: 'Administrador',
    RECEPTION: 'Recepcionista',
    CASHIER: 'Cajero',
    MAINTENANCE: 'Mantenimiento'
});

export function canAccess(roles) {
    const user = getUser();

    return Boolean(user) && hasAnyRole(user, roles);
}

export function canManageConfiguration() {
    return canAccess(PERMISSIONS.CONFIGURATION);
}

export function canManageOperations() {
    return canAccess(PERMISSIONS.OPERATIONS);
}

export function canManagePayments() {
    return canAccess(PERMISSIONS.FINANCIAL);
}

export function canManageMaintenance() {
    return canAccess(PERMISSIONS.MAINTENANCE);
}

export function getRoleLabels(user = getUser()) {
    return (user?.roles ?? []).map(
        role => ROLE_LABELS[role] ?? role
    );
}