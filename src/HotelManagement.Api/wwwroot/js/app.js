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
            const paths = TILES.find(([key]) => key === name)?.[3] || ['M3 3h7v7H3z', 'M14 3h7v7h-7z', 'M3 14h7v7H3z', 'M14 14h7v7h-7z'];
            const link = el('a', { href: `#${name}` }, icon(paths), el('span', { text: view.title }));
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

    const logoutButton = el('button', { type: 'button', class: 'btn secondary', text: 'Cerrar sesión' });
    logoutButton.addEventListener('click', onLogout);

    bar.append(el('span', { class: 'avatar', text: (user.fullName || user.username).slice(0, 1).toUpperCase(), 'aria-hidden': 'true' }),
        el('div', { class: 'session-person' }, el('strong', { text: user.fullName || user.username }), el('small', { text: user.roles.join(' · ') || 'Mi cuenta' })), logoutButton);
}

// ---------------------------------------------------------------- sesión

function showLogin() {
    document.body.classList.add('is-login');
    document.getElementById('page-label').textContent = 'Bienvenido a tu espacio';
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
    document.body.classList.remove('is-login');
    document.getElementById('page-label').textContent = name === 'inicio' ? 'Vista general' : views[name].title;
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

const SVG_NS = 'http://www.w3.org/2000/svg';

const TILES = [
    ['reservas', 'Reservas', 'Busca disponibilidad, crea una reserva o cancela una existente.',
        ['M8 2v4', 'M16 2v4', 'M3 10h18', 'M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z']],
    ['huespedes', 'Huéspedes', 'Registra a un huésped nuevo o busca sus datos.',
        ['M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2', 'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0z']],
    ['habitaciones', 'Habitaciones', 'Consulta cada habitación, su tipo y su estado.',
        ['M2 20v-8a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v8', 'M4 10V6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v4', 'M2 18h20']],
];

function icon(paths) {
    const svg = document.createElementNS(SVG_NS, 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('width', '24');
    svg.setAttribute('height', '24');
    svg.setAttribute('fill', 'none');
    svg.setAttribute('stroke', 'currentColor');
    svg.setAttribute('stroke-width', '1.8');
    svg.setAttribute('stroke-linecap', 'round');
    svg.setAttribute('stroke-linejoin', 'round');
    svg.setAttribute('aria-hidden', 'true');
    paths.forEach((d) => {
        const path = document.createElementNS(SVG_NS, 'path');
        path.setAttribute('d', d);
        svg.append(path);
    });
    return svg;
}

function greeting() {
    const hour = new Date().getHours();
    if (hour < 12) {
        return 'Buenos días';
    }
    return hour < 19 ? 'Buenas tardes' : 'Buenas noches';
}

function renderHome(container) {
    const user = getUser();
    const name = (user?.fullName || user?.username || '').split(' ')[0];

    const hero = el('section', { class: 'hero' },
        el('div', { class: 'hero-copy' },
            el('span', { class: 'eyebrow', text: 'CADA ESTANCIA EMPIEZA CONTIGO' }),
            el('h1', { text: name ? `${greeting()}, ${name}.` : `${greeting()}.` }),
            el('p', { text: 'Grandes experiencias. Pequeños detalles. Todo tu hotel, en un solo lugar.' }),
            el('a', { href: '#reservas/nueva', class: 'btn hero-cta', text: 'Crear una reserva ↗' })),
        el('div', { class: 'hero-art', 'aria-hidden': 'true' },
            el('div', { class: 'art-sun' }), el('div', { class: 'art-arch' }), el('div', { class: 'art-building' }),
            el('span', { class: 'art-caption', text: 'EL ARTE DE RECIBIR BIEN' })));

    const tiles = el('div', { class: 'tiles' },
        ...TILES
            .filter(([key]) => hasAnyRole(user, views[key].roles))
            .map(([key, title, text, paths]) => el('a', { href: `#${key}`, class: 'tile' },
                el('div', { class: 'tile-top' }, el('span', { class: 'tile-icon' }, icon(paths)), el('span', { class: 'tile-arrow', text: '↗', 'aria-hidden': 'true' })),
                el('h2', { text: title }),
                el('p', { text }))));

    const healthButton = el('button', { type: 'button', class: 'btn secondary', text: 'Comprobar conexión' });
    healthButton.addEventListener('click', () => run(healthButton, async () => {
        await api.get('/api/health');
        showMessage('La conexión con el servidor funciona correctamente.', 'success');
    }));

    const status = el('section', { class: 'card status-card' },
        el('div', { class: 'status-copy' }, el('span', { class: 'connection-symbol', text: '⌁', 'aria-hidden': 'true' }), el('div', {}, el('h2', { text: 'Siempre a tu servicio' }), el('p', { class: 'muted', text: 'Comprueba la conexión con el servidor cuando lo necesites.' }))), healthButton);

    const sectionHeading = el('div', { class: 'section-heading' }, el('div', {}, el('span', { class: 'eyebrow', text: 'TODO A MANO' }), el('h2', { text: 'Tu operación, sin complicaciones' })), el('span', { class: 'muted', text: '¿Por dónde empezamos?' }));
    container.append(hero, sectionHeading, tiles, status);
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
document.querySelector('.skip-link').addEventListener('click', (event) => {
    event.preventDefault();
    document.getElementById('view').focus();
});
document.getElementById('workspace-date').textContent = new Intl.DateTimeFormat('es-PE', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());
window.addEventListener('hashchange', renderRoute);
window.addEventListener('api:unauthorized', handleUnauthorized);
start();
