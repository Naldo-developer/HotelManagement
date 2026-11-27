import { api } from '../api.js';
import { setAccessToken } from '../auth-token.js';
import { clearMessages, el, showError, showMessage } from '../ui.js';

export function renderLogin(container, onSuccess) {
    const username = el('input', { id: 'l-user', name: 'username', type: 'text', maxLength: 60, autocomplete: 'username' });
    const password = el('input', { id: 'l-pass', name: 'password', type: 'password', maxLength: 128, autocomplete: 'current-password' });
    const submit = el('button', { type: 'submit', class: 'btn', text: 'Iniciar sesión' });

    const form = el('form', { class: 'login-form', novalidate: true },
        el('div', { class: 'field' }, el('label', { for: 'l-user', text: 'Usuario' }), username),
        el('div', { class: 'field' }, el('label', { for: 'l-pass', text: 'Contraseña' }), password),
        el('div', { class: 'actions' }, submit));

    container.append(el('div', { class: 'login-layout' },
        el('section', { class: 'login-story' },
            el('span', { class: 'eyebrow', text: 'AURORA HOTEL / LIMA' }),
            el('h1', { text: 'Una estancia. Mil detalles.' }),
            el('p', { text: 'La hospitalidad empieza con nuestro equipo.' }),
            el('figure', { class: 'brand-visual' }, el('img', { src: 'images/aurora-lobby.png', alt: 'Concepto del lobby de Aurora Hotel.', width: 1536, height: 1024, decoding: 'async' }))),
        el('section', { class: 'card login-card' },
            el('span', { class: 'eyebrow', text: 'ACCESO DEL EQUIPO' }),
            el('h2', { text: 'Bienvenido de nuevo.' }),
            el('p', { class: 'muted', text: 'Ingresa a tu espacio de gestión.' }), form,
            el('p', { class: 'login-note', text: 'Acceso del equipo. Al recargar la página deberás iniciar sesión nuevamente.' }))));

    let saving = false;

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (saving) {
            return; // bloquea el envío doble
        }

        clearMessages();
        if (!username.value.trim() || !password.value) {
            showMessage('Escribe tu usuario y tu contraseña.', 'error');
            return;
        }

        saving = true;
        submit.disabled = true;
        submit.textContent = 'Ingresando…';
        try {
            const user = await api.post('/api/auth/login', {
                username: username.value.trim(),
                password: password.value,
            });
            setAccessToken(user.accessToken, user.expiresAtUtc);
            password.value = '';
            const { accessToken, tokenType, expiresAtUtc, ...identity } = user;
            onSuccess(identity);
        } catch (error) {
            showError(error); // 401: "Usuario o contraseña incorrectos."
            password.value = '';
            password.focus();
        } finally {
            saving = false;
            submit.disabled = false;
            submit.textContent = 'Iniciar sesión';
        }
    });

    setTimeout(() => username.focus(), 0);
}
