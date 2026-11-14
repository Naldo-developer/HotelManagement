import { api, onLoadingChange } from './api.js';
import { bindLoadingIndicator, clearMessages, showError, showMessage } from './ui.js';

// Cada pantalla se registra aquí. Los siguientes commits (Y02, Y04B, ...)
// reemplazan los "placeholder" por su vista real.
const views = {
    inicio: { title: 'Inicio', render: renderHome },
    habitaciones: { title: 'Habitaciones', render: placeholder('Habitaciones', 'Y02') },
    huespedes: { title: 'Huéspedes', render: placeholder('Huéspedes', 'Y04B') },
    reservas: { title: 'Reservas', render: placeholder('Reservas', 'Y05') },
};

const DEFAULT_VIEW = 'inicio';

function currentViewName() {
    const name = window.location.hash.replace('#', '');
    return Object.hasOwn(views, name) ? name : DEFAULT_VIEW;
}

function renderNav(active) {
    const nav = document.getElementById('nav');
    nav.replaceChildren();
    Object.entries(views).forEach(([name, view]) => {
        const link = document.createElement('a');
        link.href = `#${name}`;
        link.textContent = view.title;
        if (name === active) {
            link.setAttribute('aria-current', 'page');
        }
        nav.append(link);
    });
}

function renderRoute() {
    const name = currentViewName();
    const view = views[name];
    const container = document.getElementById('view');

    clearMessages();
    renderNav(name);
    document.title = `${view.title} · HotelManagement`;
    container.replaceChildren();
    view.render(container);
}

function placeholder(title, commit) {
    return (container) => {
        const card = document.createElement('section');
        card.className = 'card';

        const heading = document.createElement('h1');
        heading.textContent = title;

        const text = document.createElement('p');
        text.className = 'muted';
        text.textContent = `Esta pantalla se implementa en el commit ${commit}.`;

        card.append(heading, text);
        container.append(card);
    };
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

bindLoadingIndicator(onLoadingChange);
window.addEventListener('hashchange', renderRoute);
renderRoute();