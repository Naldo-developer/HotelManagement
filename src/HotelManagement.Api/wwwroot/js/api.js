const loadingListeners = new Set();
let pendingRequests = 0;

export function onLoadingChange(listener) {
    loadingListeners.add(listener);
}

function changePending(delta) {
    pendingRequests += delta;
    const isLoading = pendingRequests > 0;
    loadingListeners.forEach((listener) => listener(isLoading));
}

export class ApiError extends Error {
    constructor(message, { status = 0, problem = null, fieldErrors = {} } = {}) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.problem = problem;
        this.fieldErrors = fieldErrors;
    }
}

function defaultMessage(status) {
    switch (status) {
        case 400: return 'Los datos enviados no son válidos.';
        case 401: return 'Tu sesión no es válida. Inicia sesión de nuevo.';
        case 403: return 'No tienes permiso para realizar esta acción.';
        case 404: return 'No se encontró el recurso solicitado.';
        case 409: return 'La operación entra en conflicto con el estado actual.';
        default:
            return status >= 500
                ? 'El servidor tuvo un problema. Intenta de nuevo más tarde.'
                : `La petición falló (código ${status}).`;
    }
}

async function readProblem(response) {
    const contentType = response.headers.get('content-type') ?? '';
    if (!contentType.includes('json')) {
        return null;
    }
    try {
        return await response.json();
    } catch {
        return null;
    }
}

function buildError(status, problem) {
    const fieldErrors =
        problem && typeof problem.errors === 'object' && problem.errors !== null
            ? problem.errors
            : {};
    const fieldMessages = Object.values(fieldErrors).flat();

    let message;
    if (fieldMessages.length > 0) {
        message = fieldMessages.join(' ');
    } else {
        message = problem?.detail || problem?.title || defaultMessage(status);
    }

    return new ApiError(message, { status, problem, fieldErrors });
}

function buildUrl(path, query) {
    if (!path.startsWith('/api')) {
        throw new Error(`Ruta de API no válida: ${path}`);
    }
    const url = new URL(path, window.location.origin);
    if (query) {
        Object.entries(query).forEach(([key, value]) => {
            if (value !== null && value !== undefined && value !== '') {
                url.searchParams.set(key, value);
            }
        });
    }
    return url;
}

async function request(method, path, { query, body, signal } = {}) {
    const headers = { Accept: 'application/json, application/problem+json' };
    const options = { method, headers, signal };

    if (body !== undefined) {
        headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(body);
    }

    changePending(1);
    try {
        let response;
        try {
            response = await fetch(buildUrl(path, query), options);
        } catch (error) {
            if (error.name === 'AbortError') {
                throw error;
            }
            throw new ApiError('No se pudo conectar con el servidor. Revisa tu conexión e intenta de nuevo.');
        }

        if (!response.ok) {
            const problem = await readProblem(response);
            if (response.status === 401) {
                window.dispatchEvent(new CustomEvent('api:unauthorized'));
            }
            if (response.status === 403) {
                window.dispatchEvent(new CustomEvent('api:forbidden'));
            }
            throw buildError(response.status, problem);
        }

        if (response.status === 204) {
            return null;
        }
        const contentType = response.headers.get('content-type') ?? '';
        return contentType.includes('json') ? await response.json() : null;
    } finally {
        changePending(-1);
    }
}

export const api = {
    get: (path, query, signal) => request('GET', path, { query, signal }),
    post: (path, body) => request('POST', path, { body }),
    put: (path, body) => request('PUT', path, { body }),
    delete: (path) => request('DELETE', path),
};