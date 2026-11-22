import { api, onLoadingChange } from './api.js';
import { bindLoadingIndicator, clearMessages, el, flushQueuedMessages, queueMessage, showError, showMessage } from './ui.js';
import { fetchCurrentUser, getUser, hasAnyRole, logout, setUser } from './session.js';
import { renderLogin } from './views/login.js';
import { renderRoomDetail, renderRoomList } from './views/rooms.js';
import { renderRoomForm } from './views/room-form.js';
import { renderGuestDetail, renderGuestList } from './views/guests.js';
import { renderGuestForm } from './views/guest-form.js';
import { renderReservationDetail, renderReservationList } from './views/reservations.js';
import { renderReservationForm } from './views/reservation-form.js';

// Cada pantalla se registra aquí. Si una vista define "roles" (códigos de rol),
// solo la ven y la abren los usuarios que tengan alguno de ellos.
const views = {
    inicio: { title: 'Inicio', render: renderHome },
    habitaciones: {
        title: 'Habitaciones',
        render: (container, param, action) => {
            if (param === 'nueva') {
                renderRoomForm(container);
            } else if (param && action === 'editar') {
                renderRoomForm(container, param);
            } else if (param) {
                renderRoomDetail(container, param);
            } else {
                renderRoomList(container);
            }
        },
    },
    huespedes: {
        title: 'Huéspedes',
        render: (container, param, action) => {
            if (param === 'nuevo') {
                renderGuestForm(container);
            } else if (param && action === 'editar') {
                renderGuestForm(container, param);
            } else if (param) {
                renderGuestDetail(container, param);
            } else {
                renderGuestList(container);
            }
        },
    },
    reservas: {
        title: 'Reservas',
        render: (container, param, action) => {
            if (param === 'nueva') {
                renderReservationForm(container);
            } else if (param) {
                renderReservationDetail(container, param, action === 'cancelar');
            } else {
                renderReservationList(container);
            }
        },
    },
};

const DEFAULT_VIEW = 'inicio';

// Hash al que volver después de iniciar sesión (por ejemplo tras una sesión vencida).
let returnTo = '';

// La ruta tiene la forma #vista, #vista/parametro o #vista/parametro/accion.
function parseRoute() {
    const [name = '', param = '', action = ''] = window.location.hash.replace('#', '').split('/');
    const known = Object.hasOwn(views, name);
    return {
        name: known ? name : DEFAULT_VIEW,
        param: known ? param : '',
        action: known ? action : '',
    };
}

function renderNav(active) {
    const nav = document.getElementById('nav');
    nav.replaceChildren();
    Object.entries(views)
        .filter(([, view]) => hasAnyRole(getUser(), view.roles))
        .forEach(([name, view]) => {
            const link = el('a', { href: `#${name}`, text: view.title });
            if (name === active) {
                link.setAttribute('aria-current', 'page');
            }
            nav.append(link);
        });
}

function renderSessionBar() {
    const bar = document.getElementById('session');
    bar.replaceChildren();

    const user = getUser();
    if (!user) {
        return;
    }

    const roles = user.roles.length > 0 ? ` · ${user.roles.join(', ')}` : '';
    const logoutButton = el('button', { type: 'button', class: 'btn secondary', text: 'Cerrar sesión' });
    logoutButton.addEventListener('click', onLogout);

    bar.append(el('span', { class: 'muted', text: `${user.fullName || user.username}${roles}` }), logoutButton);
}

// ---------------------------------------------------------------- sesión

function showLogin() {
    const container = document.getElementById('view');
    container.replaceChildren();
    document.getElementById('nav').replaceChildren();
    renderSessionBar();
    clearMessages();
    flushQueuedMessages();
    document.title = 'Iniciar sesión · HotelManagement';

    renderLogin(container, (user) => {
        setUser(user);
        queueMessage(`Sesión iniciada como ${user.fullName || user.username}.`, 'success');
        history.replaceState(null, '', returnTo || '#inicio');
        returnTo = '';
        renderRoute();
    });
}

// Cualquier 401 fuera del login significa que la sesión ya no es válida.
function handleUnauthorized() {
    if (!getUser()) {
        return; // ya se está mostrando el inicio de sesión
    }
    setUser(null);
    returnTo = window.location.hash;
    queueMessage('Tu sesión venció. Inicia sesión de nuevo.', 'info');
    showLogin();
}

async function onLogout() {
    try {
        await logout();
    } catch (error) {
        showError(error);
        return;
    }
    returnTo = '';
    history.replaceState(null, '', '#inicio');
    queueMessage('Sesión cerrada.', 'info');
    showLogin();
}

// ------------------------------------------------------------- navegación

function renderRoute() {
    if (!getUser()) {
        returnTo = window.location.hash;
        showLogin();
        return;
    }

    const { name, param, action } = parseRoute();
    const view = views[name];
    const container = document.getElementById('view');

    clearMessages();
    flushQueuedMessages();
    renderSessionBar();
    renderNav(name);
    document.title = `${view.title} · HotelManagement`;
    container.replaceChildren();

    if (!hasAnyRole(getUser(), view.roles)) {
        container.append(el('section', { class: 'card' },
            el('h1', { text: 'Acceso restringido' }),
            el('p', { class: 'muted', text: 'Tu cuenta no tiene permiso para ver esta sección.' }),
            el('div', { class: 'actions' }, el('a', { href: '#inicio', class: 'btn secondary', text: 'Ir al inicio' }))));
        showMessage('No tienes permiso para ver esta sección.', 'error');
        return;
    }

    view.render(container, param, action);
}

function renderHome(container) {
    const card = document.createElement('section');
    card.className = 'card';

    const heading = document.createElement('h1');
    heading.textContent = 'Sistema de gestión hotelera';

    const text = document.createElement('p');
    text.className = 'muted';
    text.textContent = 'Usa el menú para navegar. Puedes comprobar la conexión con la API desde aquí.';

    const output = document.createElement('pre');
    output.className = 'output';
    output.textContent = 'Sin consultas todavía.';

    const actions = document.createElement('div');
    actions.className = 'actions';

    const healthButton = document.createElement('button');
    healthButton.type = 'button';
    healthButton.className = 'btn';
    healthButton.textContent = 'Probar conexión';
    healthButton.addEventListener('click', () => run(healthButton, async () => {
        const data = await api.get('/api/health');
        output.textContent = JSON.stringify(data, null, 2);
        showMessage('La API respondió correctamente.', 'success');
    }));

    const errorButton = document.createElement('button');
    errorButton.type = 'button';
    errorButton.className = 'btn secondary';
    errorButton.textContent = 'Probar error 404';
    errorButton.addEventListener('click', () => run(errorButton, async () => {
        await api.get('/api/ruta-que-no-existe');
    }));

    actions.append(healthButton, errorButton);
    card.append(heading, text, actions, output);
    container.append(card);
}

// Ejecuta una acción, evita doble clic y muestra el error de la API de forma legible.
async function run(button, action) {
    button.disabled = true;
    try {
        await action();
    } catch (error) {
        showError(error);
    } finally {
        button.disabled = false;
    }
}

// ---------------------------------------------------------------- arranque

async function start() {
    try {
        await fetchCurrentUser(); // con sesión muestra la app; sin sesión, el inicio de sesión
        renderRoute();
    } catch (error) {
        const container = document.getElementById('view');
        const retry = el('button', { type: 'button', class: 'btn', text: 'Reintentar' });
        retry.addEventListener('click', () => {
            clearMessages();
            start();
        });
        container.replaceChildren(el('section', { class: 'card' },
            el('h1', { text: 'No se pudo conectar' }),
            el('p', { class: 'muted', text: 'No fue posible comprobar tu sesión con el servidor.' }),
            el('div', { class: 'actions' }, retry)));
        showError(error);
    }
}

bindLoadingIndicator(onLoadingChange);
window.addEventListener('hashchange', renderRoute);
window.addEventListener('api:unauthorized', handleUnauthorized);
start();