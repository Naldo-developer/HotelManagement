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
import {
    PERMISSIONS,
    canManageConfiguration,
    canManageOperations,
    getRoleLabels
} from './permissions.js';
import { renderStays } from './views/stays.js';
import { renderConsumptions } from './views/consumptions.js';
import { renderPayments } from './views/payments.js';
import { renderMaintenance } from './views/maintenance.js';
import { renderHotels } from './views/hotels.js';
import { renderRoomTypes } from './views/room-types.js';
import { renderServices } from './views/services.js';
import { renderEmployees } from './views/employees.js';
import { renderUsers } from './views/users.js';
import { renderDashboard } from './views/dashboard.js';

// Cada pantalla se registra aquí. Si una vista define "roles" (códigos de rol),
// solo la ven y la abren los usuarios que tengan alguno de ellos.
const views = {
    inicio: {
        title: 'Inicio',
        render: renderDashboard
    },
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
        roles: PERMISSIONS.OPERATIONS,
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
        roles: PERMISSIONS.OPERATIONS,
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
    estadias: {
        title: 'Estadías',
        roles: PERMISSIONS.OPERATIONS,
        render: renderStays
    },

    consumos: {
        title: 'Consumos',
        roles: PERMISSIONS.OPERATIONS,
        render: renderConsumptions
    },

    pagos: {
        title: 'Pagos',
        roles: PERMISSIONS.FINANCIAL,
        render: renderPayments
    },
    mantenimiento: {
        title: 'Mantenimiento',
        roles: PERMISSIONS.MAINTENANCE,
        render: renderMaintenance
    },

    hoteles: {
        title: 'Hoteles',
        roles: PERMISSIONS.CONFIGURATION,
        render: renderHotels
    },

    'tipos-habitacion': {
        title: 'Tipos de habitación',
        roles: PERMISSIONS.CONFIGURATION,
        render: renderRoomTypes
    },

    servicios: {
        title: 'Servicios',
        roles: PERMISSIONS.OPERATIONS,
        render: renderServices
    },
    empleados: {
        title: 'Empleados',
        roles: PERMISSIONS.CONFIGURATION,
        render: renderEmployees
    },

    usuarios: {
        title: 'Usuarios',
        roles: PERMISSIONS.CONFIGURATION,
        render: renderUsers
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
        el('div', { class: 'session-person' }, el('strong', { text: user.fullName || user.username }), el('small', { text: getRoleLabels(user).join(' · ') || 'Mi cuenta' })), logoutButton);
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
    document.title = 'Iniciar sesión · Aurora Hotel';

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
    document.title = `${view.title} · Aurora Hotel`;
    container.replaceChildren();

    const requiresConfiguration =
        name === 'habitaciones' &&
        (param === 'nueva' || action === 'editar');

    if (
        !hasAnyRole(getUser(), view.roles) ||
        (requiresConfiguration && !canManageConfiguration())
    ) {
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
    ['estadias', 'Estadías',
        'Gestiona entradas y salidas de los huéspedes.',
        [
            'M3 21h18',
            'M6 21V3h12v18',
            'M14 12h.01'
        ]
    ],

    ['consumos', 'Consumos',
        'Registra los servicios utilizados durante la estancia.',
        [
            'M4 3h16v18l-4-2-4 2-4-2-4 2z',
            'M8 8h8',
            'M8 13h8'
        ]
    ],

    ['pagos', 'Pagos',
        'Administra cobros, saldos e historial financiero.',
        [
            'M3 5h18v14H3z',
            'M3 10h18',
            'M7 15h4'
        ]
    ],
    ['mantenimiento', 'Mantenimiento',
        'Gestiona el cuidado y conservación de las instalaciones.',
        [
            'M14 6l4 4',
            'M3 17l9-9 4 4-9 9H3z',
            'M14 6l3-3 4 4-3 3'
        ]
    ],

    ['hoteles', 'Hoteles',
        'Administra los establecimientos y su configuración.',
        [
            'M4 21V3h16v18',
            'M2 21h20',
            'M8 7h2 M14 7h2',
            'M8 11h2 M14 11h2',
            'M10 21v-6h4v6'
        ]
    ],

    ['tipos-habitacion', 'Tipos de habitación',
        'Configura categorías, capacidades y tarifas.',
        [
            'M12 2l10 5-10 5L2 7z',
            'M2 12l10 5 10-5',
            'M2 17l10 5 10-5'
        ]
    ],

    ['servicios', 'Servicios',
        'Consulta el catálogo de experiencias para huéspedes.',
        [
            'M3 17h18',
            'M5 17a7 7 0 0 1 14 0',
            'M12 7V5',
            'M2 20h20'
        ]
    ],
    ['empleados', 'Empleados',
        'Administra el equipo de trabajo.',
        [
            'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0z',
            'M4 21v-3a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v3'
        ]
    ],

    ['usuarios', 'Usuarios',
        'Gestiona cuentas y permisos.',
        [
            'M12 22s8-4 8-11V5l-8-3-8 3v6c0 7 8 11 8 11z',
            'M9 12l2 2 4-4'
        ]
    ],
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
