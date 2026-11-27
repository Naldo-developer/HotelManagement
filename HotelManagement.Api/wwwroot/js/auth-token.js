// El JWT solo vive en memoria: recargar o cerrar esta página termina la sesión.
let accessToken = null;
let expiresAt = 0;

export function setAccessToken(token, expiresAtUtc) {
    const expiration = Date.parse(expiresAtUtc);
    if (typeof token !== 'string' || !token || !Number.isFinite(expiration) || expiration <= Date.now()) {
        throw new Error('El servidor no devolvió una sesión válida. Inicia sesión nuevamente.');
    }
    accessToken = token;
    expiresAt = expiration;
}

export function clearAccessToken() {
    accessToken = null;
    expiresAt = 0;
}

export function getAccessToken() {
    if (expiresAt <= Date.now()) clearAccessToken();
    return accessToken;
}
