import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';

const PAGE_SIZES = [10, 20, 50, 100];

// El estado vive a nivel de módulo: al volver desde el detalle se conservan filtros y página.
const state = {
    filters: { name: '', documentType: '', documentNumber: '', pageSize: 20 },
    page: 1,
};
let currentRequest = null;

const isPositiveInteger = (value) => /^[1-9]\d*$/.test(value);
const fullName = (guest) => `${guest.firstName} ${guest.lastName}`;
const documentText = (guest) =>
    guest.documentType && guest.documentNumber ? `${guest.documentType} ${guest.documentNumber}` : '—';

function field(id, label, control) {
    return el('div', { class: 'field' }, el('label', { for: id, text: label }), control);
}

// ---------------------------------------------------------------- LISTA

export function renderGuestList(container) {
    const ui = {};

    const nameInput = el('input', { id: 'g-name', name: 'name', type: 'search', maxLength: 100, placeholder: 'Nombre o apellido', value: state.filters.name });
    const typeInput = el('input', { id: 'g-type', name: 'documentType', type: 'text', maxLength: 20, placeholder: 'Ej.: DNI', value: state.filters.documentType });
    const numberInput = el('input', { id: 'g-number', name: 'documentNumber', type: 'text', maxLength: 30, value: state.filters.documentNumber });
    const sizeSelect = el('select', { id: 'g-size', name: 'pageSize' },
        ...PAGE_SIZES.map((size) => el('option', { value: String(size), text: String(size) })));
    sizeSelect.value = String(state.filters.pageSize);

    ui.submit = el('button', { type: 'submit', class: 'btn', text: 'Filtrar' });
    ui.clear = el('button', { type: 'button', class: 'btn secondary', text: 'Limpiar' });

    const form = el('form', { class: 'filters', novalidate: true },
        field('g-name', 'Nombre', nameInput),
        field('g-type', 'Tipo de documento', typeInput),
        field('g-number', 'Número de documento', numberInput),
        field('g-size', 'Por página', sizeSelect),
        el('div', { class: 'filters-actions' }, ui.submit, ui.clear));

    ui.results = el('div', { class: 'results' });
    ui.prev = el('button', { type: 'button', class: 'btn secondary', text: '← Anterior' });
    ui.next = el('button', { type: 'button', class: 'btn secondary', text: 'Siguiente →' });
    ui.pageLabel = el('span', { class: 'muted' });
    ui.pagination = el('div', { class: 'pagination' }, ui.prev, ui.pageLabel, ui.next);

    const card = el('section', { class: 'card' },
        el('div', { class: 'card-header' },
            el('h1', { text: 'Huéspedes' }),
            el('a', { href: '#huespedes/nuevo', class: 'btn', text: '+ Nuevo huésped' })),
        form,
        ui.results,
        ui.pagination);
    container.append(card);
    ui.root = card;

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        const data = new FormData(form);
        state.filters = {
            name: String(data.get('name') ?? '').trim(),
            documentType: String(data.get('documentType') ?? '').trim(),
            documentNumber: String(data.get('documentNumber') ?? '').trim(),
            pageSize: Number(data.get('pageSize')) || 20,
        };
        state.page = 1; // Filtrar reinicia la página
        loadGuests(ui);
    });

    ui.clear.addEventListener('click', () => {
        state.filters = { name: '', documentType: '', documentNumber: '', pageSize: 20 };
        state.page = 1;
        nameInput.value = '';
        typeInput.value = '';
        numberInput.value = '';
        sizeSelect.value = '20';
        loadGuests(ui);
    });

    // Avanzar y retroceder usan los filtros ya aplicados, no lo que se haya escrito sin enviar.
    ui.prev.addEventListener('click', () => {
        if (state.page > 1) {
            state.page -= 1;
            loadGuests(ui);
        }
    });
    ui.next.addEventListener('click', () => {
        state.page += 1;
        loadGuests(ui);
    });

    loadGuests(ui);
}

async function loadGuests(ui) {
    currentRequest?.abort();
    const controller = new AbortController();
    currentRequest = controller;
    [ui.submit, ui.clear, ui.prev, ui.next].forEach((button) => { button.disabled = true; });

    try {
        const guests = await api.get('/api/guests', {
            name: state.filters.name,
            documentType: state.filters.documentType,
            documentNumber: state.filters.documentNumber,
            page: state.page,
            pageSize: state.filters.pageSize,
        }, controller.signal);

        if (controller.signal.aborted || !ui.root.isConnected) {
            return;
        }
        renderResults(ui, guests);
    } catch (error) {
        if (error.name === 'AbortError' || !ui.root.isConnected) {
            return;
        }
        ui.results.replaceChildren(el('p', { class: 'muted', text: 'No se pudo cargar el listado.' }));
        ui.pageLabel.textContent = `Página ${state.page}`;
        ui.prev.disabled = state.page <= 1;
        ui.next.disabled = true;
        showError(error);
    } finally {
        if (currentRequest === controller) {
            currentRequest = null;
            ui.submit.disabled = false;
            ui.clear.disabled = false;
        }
    }
}

function renderResults(ui, guests) {
    ui.results.replaceChildren();

    if (guests.length === 0) {
        const text = state.page > 1
            ? 'No hay más resultados en esta página. Vuelve a la anterior.'
            : 'No se encontraron huéspedes con esos filtros.';
        ui.results.append(el('p', { class: 'muted', text }));
    } else {
        const head = el('tr', {},
            ...['Nombre', 'Documento', 'Correo', 'Teléfono', ''].map((title) => el('th', { text: title })));

        const body = guests.map((guest) => el('tr', {},
            el('td', {}, el('a', { href: `#huespedes/${guest.id}`, text: fullName(guest) })),
            el('td', { text: documentText(guest) }),
            el('td', { text: guest.email ?? '—' }),
            el('td', { text: guest.phone ?? '—' }),
            el('td', {}, el('a', { href: `#huespedes/${guest.id}`, text: 'Ver detalle' }))));

        ui.results.append(el('div', { class: 'table-wrap' },
            el('table', {}, el('thead', {}, head), el('tbody', {}, ...body))));
    }

    // La API no devuelve el total: si llegó una página completa, puede haber otra.
    ui.pageLabel.textContent = `Página ${state.page}`;
    ui.prev.disabled = state.page <= 1;
    ui.next.disabled = guests.length < state.filters.pageSize;
}

// -------------------------------------------------------------- DETALLE

export async function renderGuestDetail(container, rawId) {
    currentRequest?.abort();
    currentRequest = null;

    const back = el('a', { href: '#huespedes', class: 'btn secondary', text: '← Volver al listado' });
    const status = el('p', { class: 'muted', text: 'Cargando…' });
    const card = el('section', { class: 'card' }, el('h1', { text: 'Detalle del huésped' }), status);
    container.append(card);

    if (!isPositiveInteger(rawId)) {
        status.textContent = 'El identificador del huésped no es válido.';
        card.append(el('div', { class: 'actions' }, back));
        showMessage('El identificador del huésped no es válido.', 'error');
        return;
    }

    try {
        const guest = await api.get(`/api/guests/${rawId}`);
        if (!card.isConnected) {
            return;
        }

        const details = el('dl', { class: 'details' },
            ...[
                ['ID', guest.id],
                ['Nombre', guest.firstName],
                ['Apellido', guest.lastName],
                ['Documento', documentText(guest)],
                ['Correo', guest.email ?? '—'],
                ['Teléfono', guest.phone ?? '—'],
            ].flatMap(([term, value]) => [
                el('dt', { text: term }),
                el('dd', { text: String(value) }),
            ]));

        const edit = el('a', { href: `#huespedes/${guest.id}/editar`, class: 'btn', text: 'Editar' });

        status.replaceWith(details);
        card.append(el('div', { class: 'actions' }, back, edit));
    } catch (error) {
        if (!card.isConnected) {
            return;
        }
        status.textContent = 'No se pudo cargar el huésped.';
        card.append(el('div', { class: 'actions' }, back));
        showError(error); // un 404 muestra el mensaje de la API
    }
}