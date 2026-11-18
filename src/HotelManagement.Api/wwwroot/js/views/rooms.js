import { api } from '../api.js';
import { getHotels, getRoomTypes, ROOM_STATUSES } from '../catalogs.js';
import { el, setSelectOptions, showError, showMessage } from '../ui.js';

const PAGE_SIZES = [10, 20, 50, 100];

// El estado vive a nivel de módulo: al volver desde el detalle se conservan filtros y página.
const state = {
    filters: { hotelId: '', roomTypeId: '', operationalStatus: '', pageSize: 10 },
    page: 1,
};
let currentRequest = null;

const isPositiveInteger = (value) => /^[1-9]\d*$/.test(value);
const statusLabel = (status) => ROOM_STATUSES[status] ?? status;

// Traduce IDs a nombres usando los catálogos. Si fallan, se muestran los ID.
async function resolveNames(rooms) {
    const hotels = new Map();
    const types = new Map();
    try {
        (await getHotels()).forEach((hotel) => hotels.set(hotel.hotelId, hotel.name));
        const hotelIds = [...new Set(rooms.map((room) => room.hotelId))];
        const lists = await Promise.all(hotelIds.map((id) => getRoomTypes(id)));
        lists.forEach((list) => list.forEach((type) => types.set(type.roomTypeId, type.name)));
    } catch {
        // sin nombres: la tabla usa los identificadores
    }
    return { hotels, types };
}

const hotelName = (names, id) => names.hotels.get(id) ?? `ID ${id}`;
const typeName = (names, id) => names.types.get(id) ?? `ID ${id}`;

// ---------------------------------------------------------------- LISTA

export function renderRoomList(container) {
    const ui = {};

    const hotelSelect = el('select', { id: 'f-hotel', name: 'hotelId' }, el('option', { value: '', text: 'Todos' }));
    const typeSelect = el('select', { id: 'f-type', name: 'roomTypeId', disabled: true },
        el('option', { value: '', text: 'Elige un hotel primero' }));

    const statusSelect = el('select', { id: 'f-status', name: 'operationalStatus' },
        el('option', { value: '', text: 'Todos' }),
        ...Object.entries(ROOM_STATUSES).map(([value, label]) => el('option', { value, text: label })));
    statusSelect.value = state.filters.operationalStatus;

    const sizeSelect = el('select', { id: 'f-size', name: 'pageSize' },
        ...PAGE_SIZES.map((size) => el('option', { value: String(size), text: String(size) })));
    sizeSelect.value = String(state.filters.pageSize);

    ui.submit = el('button', { type: 'submit', class: 'btn', text: 'Filtrar' });
    ui.clear = el('button', { type: 'button', class: 'btn secondary', text: 'Limpiar' });

    const form = el('form', { class: 'filters', novalidate: true },
        field('f-hotel', 'Hotel', hotelSelect),
        field('f-type', 'Tipo', typeSelect),
        field('f-status', 'Estado', statusSelect),
        field('f-size', 'Por página', sizeSelect),
        el('div', { class: 'filters-actions' }, ui.submit, ui.clear));

    ui.results = el('div', { class: 'results' });
    ui.prev = el('button', { type: 'button', class: 'btn secondary', text: '← Anterior' });
    ui.next = el('button', { type: 'button', class: 'btn secondary', text: 'Siguiente →' });
    ui.pageLabel = el('span', { class: 'muted' });
    ui.pagination = el('div', { class: 'pagination' }, ui.prev, ui.pageLabel, ui.next);

    const card = el('section', { class: 'card' },
        el('div', { class: 'card-header' },
            el('h1', { text: 'Habitaciones' }),
            el('a', { href: '#habitaciones/nueva', class: 'btn', text: '+ Nueva habitación' })),
        form,
        ui.results,
        ui.pagination);
    container.append(card);
    ui.root = card;

    hotelSelect.addEventListener('change', () => loadTypeFilter(hotelSelect, typeSelect, hotelSelect.value, ''));

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        state.filters = readForm(form);
        state.page = 1; // Filtrar reinicia la página
        loadRooms(ui);
    });

    ui.clear.addEventListener('click', () => {
        state.filters = { hotelId: '', roomTypeId: '', operationalStatus: '', pageSize: 10 };
        state.page = 1;
        hotelSelect.value = '';
        statusSelect.value = '';
        sizeSelect.value = '10';
        loadTypeFilter(hotelSelect, typeSelect, '', '');
        loadRooms(ui);
    });

    // Avanzar y retroceder usan los filtros ya aplicados, no lo que haya cambiado sin enviar.
    ui.prev.addEventListener('click', () => {
        if (state.page > 1) {
            state.page -= 1;
            loadRooms(ui);
        }
    });
    ui.next.addEventListener('click', () => {
        state.page += 1;
        loadRooms(ui);
    });

    initFilters(ui, hotelSelect, typeSelect);
    loadRooms(ui);
}

function field(id, label, control) {
    return el('div', { class: 'field' }, el('label', { for: id, text: label }), control);
}

async function initFilters(ui, hotelSelect, typeSelect) {
    try {
        const hotels = await getHotels();
        if (!ui.root.isConnected) {
            return;
        }
        setSelectOptions(hotelSelect, 'Todos', hotels, 'hotelId', state.filters.hotelId);
        await loadTypeFilter(hotelSelect, typeSelect, hotelSelect.value, state.filters.roomTypeId);
    } catch (error) {
        if (ui.root.isConnected) {
            showError(error);
        }
    }
}

async function loadTypeFilter(hotelSelect, typeSelect, hotelId, selected) {
    if (!hotelId) {
        setSelectOptions(typeSelect, 'Elige un hotel primero', [], 'roomTypeId');
        typeSelect.disabled = true;
        return;
    }

    typeSelect.disabled = true;
    try {
        const types = await getRoomTypes(hotelId);
        if (hotelSelect.value !== hotelId) {
            return; // el usuario ya eligió otro hotel
        }
        setSelectOptions(typeSelect, 'Todos', types, 'roomTypeId', selected);
        typeSelect.disabled = false;
    } catch (error) {
        showError(error);
    }
}

function readForm(form) {
    const data = new FormData(form);
    return {
        hotelId: String(data.get('hotelId') ?? ''),
        roomTypeId: String(data.get('roomTypeId') ?? ''),
        operationalStatus: String(data.get('operationalStatus') ?? ''),
        pageSize: Number(data.get('pageSize')) || 10,
    };
}

async function loadRooms(ui) {
    currentRequest?.abort();
    const controller = new AbortController();
    currentRequest = controller;
    [ui.submit, ui.clear, ui.prev, ui.next].forEach((button) => { button.disabled = true; });

    try {
        const rooms = await api.get('/api/rooms', {
            hotelId: state.filters.hotelId,
            roomTypeId: state.filters.roomTypeId,
            operationalStatus: state.filters.operationalStatus,
            page: state.page,
            pageSize: state.filters.pageSize,
        }, controller.signal);

        const names = await resolveNames(rooms);
        if (controller.signal.aborted || !ui.root.isConnected) {
            return;
        }
        renderResults(ui, rooms, names);
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

function renderResults(ui, rooms, names) {
    ui.results.replaceChildren();

    if (rooms.length === 0) {
        const text = state.page > 1
            ? 'No hay más resultados en esta página. Vuelve a la anterior.'
            : 'No se encontraron habitaciones con esos filtros.';
        ui.results.append(el('p', { class: 'muted', text }));
    } else {
        const head = el('tr', {},
            ...['Número', 'Piso', 'Hotel', 'Tipo', 'Estado', ''].map((title) => el('th', { text: title })));

        const body = rooms.map((room) => el('tr', {},
            el('td', {}, el('a', { href: `#habitaciones/${room.roomId}`, text: room.roomNumber })),
            el('td', { text: room.floor ?? '—' }),
            el('td', { text: hotelName(names, room.hotelId) }),
            el('td', { text: typeName(names, room.roomTypeId) }),
            el('td', { text: statusLabel(room.operationalStatus) }),
            el('td', {}, el('a', { href: `#habitaciones/${room.roomId}`, text: 'Ver detalle' }))));

        ui.results.append(el('div', { class: 'table-wrap' },
            el('table', {}, el('thead', {}, head), el('tbody', {}, ...body))));
    }

    // La API no devuelve el total: si llegó una página completa, puede haber otra.
    ui.pageLabel.textContent = `Página ${state.page}`;
    ui.prev.disabled = state.page <= 1;
    ui.next.disabled = rooms.length < state.filters.pageSize;
}

// -------------------------------------------------------------- DETALLE

export async function renderRoomDetail(container, rawId) {
    currentRequest?.abort();
    currentRequest = null;

    const back = el('a', { href: '#habitaciones', class: 'btn secondary', text: '← Volver al listado' });
    const status = el('p', { class: 'muted', text: 'Cargando…' });
    const card = el('section', { class: 'card' }, el('h1', { text: 'Detalle de la habitación' }), status);
    container.append(card);

    if (!isPositiveInteger(rawId)) {
        status.textContent = 'El identificador de la habitación no es válido.';
        card.append(el('div', { class: 'actions' }, back));
        showMessage('El identificador de la habitación no es válido.', 'error');
        return;
    }

    try {
        const room = await api.get(`/api/rooms/${rawId}`);
        const names = await resolveNames([room]);
        if (!card.isConnected) {
            return;
        }

        const details = el('dl', { class: 'details' },
            ...[
                ['ID', room.roomId],
                ['Número', room.roomNumber],
                ['Piso', room.floor ?? '—'],
                ['Hotel', `${hotelName(names, room.hotelId)} (ID ${room.hotelId})`],
                ['Tipo de habitación', `${typeName(names, room.roomTypeId)} (ID ${room.roomTypeId})`],
                ['Estado operativo', statusLabel(room.operationalStatus)],
            ].flatMap(([term, value]) => [
                el('dt', { text: term }),
                el('dd', { text: String(value) }),
            ]));

        const edit = el('a', { href: `#habitaciones/${room.roomId}/editar`, class: 'btn', text: 'Editar' });

        status.replaceWith(details);
        card.append(el('div', { class: 'actions' }, back, edit));
    } catch (error) {
        if (!card.isConnected) {
            return;
        }
        status.textContent = 'No se pudo cargar la habitación.';
        card.append(el('div', { class: 'actions' }, back));
        showError(error); // un 404 muestra el mensaje de la API
    }
}