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

    container.append(el('section', { class: 'card login-card' },
        el('h1', { text: 'Iniciar sesión' }),
        el('p', { class: 'muted', text: 'Ingresa con tu cuenta del hotel.' }),
        form));

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