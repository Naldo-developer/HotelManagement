import { api } from '../api.js';
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
            el('span', { class: 'eyebrow', text: 'HOSPITALIDAD, SIN COMPLICACIONES' }),
            el('h1', { text: 'El cuidado está en los detalles.' }),
            el('p', { text: 'Un espacio para gestionar tu hotel y dedicarte a lo que más importa: recibir bien.' }),
            el('div', { class: 'hero-art', 'aria-hidden': 'true' }, el('div', { class: 'art-sun' }), el('div', { class: 'art-arch' }), el('div', { class: 'art-building' }))),
        el('section', { class: 'card login-card' },
            el('span', { class: 'eyebrow', text: 'TU ESPACIO DE TRABAJO' }),
            el('h2', { text: 'Qué gusto verte.' }),
            el('p', { class: 'muted', text: 'Inicia sesión para continuar con tu día.' }), form,
            el('p', { class: 'login-note', text: 'Acceso exclusivo para el equipo del hotel.' }))));

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
            password.value = '';
            onSuccess(user);
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
