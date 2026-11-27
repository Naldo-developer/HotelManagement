import test from 'node:test';
import assert from 'node:assert/strict';
import { api } from '../../src/HotelManagement.Api/wwwroot/js/api.js';
import { clearAccessToken, getAccessToken, setAccessToken } from '../../src/HotelManagement.Api/wwwroot/js/auth-token.js';

test('El cliente usa Bearer y omite cookies, sin enviar un token previo al login', async () => {
    const requests = [];
    const oldFetch = globalThis.fetch;
    const oldWindow = globalThis.window;
    globalThis.window = { location: { origin: 'http://hotel.test' }, dispatchEvent() {} };
    globalThis.fetch = async (url, options) => {
        requests.push({ url: String(url), options });
        return Response.json({ ok: true });
    };
    try {
        setAccessToken('token-de-prueba', new Date(Date.now() + 60_000).toISOString());
        await api.get('/api/rooms');
        await api.post('/api/auth/login', { username: 'demo', password: 'prueba' });
        assert.equal(requests[0].options.headers.Authorization, 'Bearer token-de-prueba');
        assert.equal(requests[0].options.credentials, 'omit');
        assert.equal(requests[1].options.headers.Authorization, undefined);
    } finally {
        clearAccessToken();
        globalThis.fetch = oldFetch;
        globalThis.window = oldWindow;
    }
});

test('Un 401 borra el token y notifica el fin de sesión', async () => {
    const events = [];
    const oldFetch = globalThis.fetch;
    const oldWindow = globalThis.window;
    globalThis.window = { location: { origin: 'http://hotel.test' }, dispatchEvent(event) { events.push(event.type); } };
    globalThis.fetch = async () => new Response(null, { status: 401 });
    try {
        setAccessToken('token-de-prueba', new Date(Date.now() + 60_000).toISOString());
        await assert.rejects(api.get('/api/rooms'), error => error.status === 401);
        assert.equal(getAccessToken(), null);
        assert.deepEqual(events, ['api:unauthorized']);
    } finally {
        clearAccessToken();
        globalThis.fetch = oldFetch;
        globalThis.window = oldWindow;
    }
});

test('La expiración local elimina el token sin persistirlo', () => {
    const originalNow = Date.now;
    const now = originalNow();
    try {
        setAccessToken('token-de-prueba', new Date(now + 60_000).toISOString());
        Date.now = () => now + 60_001;
        assert.equal(getAccessToken(), null);
        assert.throws(() => setAccessToken('token', 'fecha-invalida'));
    } finally {
        Date.now = originalNow;
        clearAccessToken();
    }
});
