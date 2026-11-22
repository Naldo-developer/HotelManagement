import { api } from '../api.js';
import { getHotels, hotelName, resolveNames, RESERVATION_STATUSES, typeName } from '../catalogs.js';
import { formatDate, formatMoney, nightsBetween } from '../format.js';
import { clearMessages, el, setSelectOptions, showError, showMessage } from '../ui.js';

const PAGE_SIZES = [10, 20, 50];
const CANCELLABLE = ['PENDING', 'CONFIRMED'];

const state = { filters: { hotelId: '', status: '', pageSize: 10 }, page: 1 };
let currentRequest = null;

const isPositiveInteger = (value) => /^[1-9]\d*$/.test(value);
const statusLabel = (status) => RESERVATION_STATUSES[status] ?? status;
const statusBadge = (status) => el('span', { class: `status ${status}`, text: statusLabel(status) });

// Habitaciones y huéspedes se piden una sola vez; si fallan, se muestran los ID.
const roomCache = new Map();
const guestCache = new Map();

function cached(cache, id, path) {
    if (!cache.has(id)) {
        cache.set(id, api.get(path).then(
            (value) => value,
            () => {
                cache.delete(id);
                return null;
            }));
    }
    return cache.get(id);
}

const getRoom = (id) => cached(roomCache, id, `/api/rooms/${id}`);
const getGuest = (id) => cached(guestCache, id, `/api/guests/${id}`);

function field(id, label, control) {
    return el('div', { class: 'field' }, el('label', { for: id, text: label }), control);
}

// ---------------------------------------------------------------- LISTA

export function renderReservationList(container) {
    const ui = {};

    const hotelSelect = el('select', { id: 'rf-hotel', name: 'hotelId' }, el('option', { value: '', text: 'Todos' }));
    const statusSelect = el('select', { id: 'rf-status', name: 'status' },
        el('option', { value: '', text: 'Todos' }),
        ...Object.entries(RESERVATION_STATUSES).map(([value, label]) => el('option', { value, text: label })));
    statusSelect.value = state.filters.status;
    const sizeSelect = el('select', { id: 'rf-size', name: 'pageSize' },
        ...PAGE_SIZES.map((size) => el('option', { value: String(size), text: String(size) })));
    sizeSelect.value = String(state.filters.pageSize);

    ui.submit = el('button', { type: 'submit', class: 'btn', text: 'Filtrar' });
    ui.clear = el('button', { type: 'button', class: 'btn secondary', text: 'Limpiar' });

    const form = el('form', { class: 'filters', novalidate: true },
        field('rf-hotel', 'Hotel', hotelSelect),
        field('rf-status', 'Estado', statusSelect),
        field('rf-size', 'Por página', sizeSelect),
        el('div', { class: 'filters-actions' }, ui.submit, ui.clear));

    ui.results = el('div', { class: 'results' });
    ui.prev = el('button', { type: 'button', class: 'btn secondary', text: '← Anterior' });
    ui.next = el('button', { type: 'button', class: 'btn secondary', text: 'Siguiente →' });
    ui.pageLabel = el('span', { class: 'muted' });
    ui.pagination = el('div', { class: 'pagination' }, ui.prev, ui.pageLabel, ui.next);

    const card = el('section', { class: 'card' },
        el('div', { class: 'card-header' },
            el('h1', { text: 'Reservas' }),
            el('a', { href: '#reservas/nueva', class: 'btn', text: '+ Nueva reserva' })),
        form, ui.results, ui.pagination);
    container.append(card);
    ui.root = card;

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        const data = new FormData(form);
        state.filters = {
            hotelId: String(data.get('hotelId') ?? ''),
            status: String(data.get('status') ?? ''),
            pageSize: Number(data.get('pageSize')) || 10,
        };
        state.page = 1; // Filtrar reinicia la página
        loadReservations(ui);
    });

    ui.clear.addEventListener('click', () => {
        state.filters = { hotelId: '', status: '', pageSize: 10 };
        state.page = 1;
        hotelSelect.value = '';
        statusSelect.value = '';
        sizeSelect.value = '10';
        loadReservations(ui);
    });

    ui.prev.addEventListener('click', () => {
        if (state.page > 1) {
            state.page -= 1;
            loadReservations(ui);
        }
    });
    ui.next.addEventListener('click', () => {
        state.page += 1;
        loadReservations(ui);
    });

    (async () => {
        try {
            const hotels = await getHotels();
            if (ui.root.isConnected) {
                setSelectOptions(hotelSelect, 'Todos', hotels, 'hotelId', state.filters.hotelId);
            }
        } catch (error) {
            if (ui.root.isConnected) {
                showError(error);
            }
        }
    })();

    loadReservations(ui);
}

async function loadReservations(ui) {
    currentRequest?.abort();
    const controller = new AbortController();
    currentRequest = controller;
    [ui.submit, ui.clear, ui.prev, ui.next].forEach((button) => { button.disabled = true; });

    try {
        const reservations = await api.get('/api/reservations', {
            hotelId: state.filters.hotelId,
            status: state.filters.status,
            page: state.page,
            pageSize: state.filters.pageSize,
        }, controller.signal);

        const roomIds = [...new Set(reservations.map((r) => r.roomId))];
        const guestIds = [...new Set(reservations.map((r) => r.primaryGuestId))];
        await Promise.all([...roomIds.map(getRoom), ...guestIds.map(getGuest)]);

        if (controller.signal.aborted || !ui.root.isConnected) {
            return;
        }
        renderResults(ui, reservations);
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

function renderResults(ui, reservations) {
    ui.results.replaceChildren();

    if (reservations.length === 0) {
        const text = state.page > 1
            ? 'No hay más resultados en esta página. Vuelve a la anterior.'
            : 'No se encontraron reservas con esos filtros.';
        ui.results.append(el('p', { class: 'muted', text }));
    } else {
        const head = el('tr', {},
            ...['Código', 'Habitación', 'Huésped', 'Entrada', 'Salida', 'Estado', ''].map((title) => el('th', { text: title })));

        // Habitación y huésped se rellenan después con los datos ya cargados en caché.
        const body = reservations.map((reservation) => el('tr', {},
            el('td', {}, el('a', { href: `#reservas/${reservation.reservationId}`, text: reservation.confirmationCode.slice(0, 8).toUpperCase() })),
            el('td', { text: `ID ${reservation.roomId}` }),
            el('td', { text: `ID ${reservation.primaryGuestId}` }),
            el('td', { text: formatDate(reservation.checkInDate) }),
            el('td', { text: formatDate(reservation.checkOutDate) }),
            el('td', {}, statusBadge(reservation.status)),
            el('td', {}, ...actionLinks(reservation))));

        const table = el('table', {}, el('thead', {}, head), el('tbody', {}, ...body));
        ui.results.append(el('div', { class: 'table-wrap' }, table));

        Promise.all(reservations.map(async (reservation, index) => {
            const row = table.tBodies[0].rows[index];
            const room = await getRoom(reservation.roomId);
            const guest = await getGuest(reservation.primaryGuestId);
            row.cells[1].textContent = room ? room.roomNumber : `ID ${reservation.roomId}`;
            row.cells[2].textContent = guest ? `${guest.firstName} ${guest.lastName}` : `ID ${reservation.primaryGuestId}`;
        }));
    }

    // La API no devuelve el total: si llegó una página completa, puede haber otra.
    ui.pageLabel.textContent = `Página ${state.page}`;
    ui.prev.disabled = state.page <= 1;
    ui.next.disabled = reservations.length < state.filters.pageSize;
}

function actionLinks(reservation) {
    const links = [el('a', { href: `#reservas/${reservation.reservationId}`, text: 'Ver' })];
    if (CANCELLABLE.includes(reservation.status)) {
        links.push(document.createTextNode(' · '));
        links.push(el('a', { href: `#reservas/${reservation.reservationId}/cancelar`, text: 'Cancelar' }));
    }
    return links;
}

// -------------------------------------------------------------- DETALLE

export async function renderReservationDetail(container, rawId, openCancel = false) {
    currentRequest?.abort();
    currentRequest = null;

    const back = el('a', { href: '#reservas', class: 'btn secondary', text: '← Volver al listado' });
    const status = el('p', { class: 'muted', text: 'Cargando…' });
    const card = el('section', { class: 'card' }, el('h1', { text: 'Detalle de la reserva' }), status);
    container.append(card);

    if (!isPositiveInteger(rawId)) {
        status.textContent = 'El identificador de la reserva no es válido.';
        card.append(el('div', { class: 'actions' }, back));
        showMessage('El identificador de la reserva no es válido.', 'error');
        return;
    }

    try {
        const reservation = await api.get(`/api/reservations/${rawId}`);
        const [room, guest] = await Promise.all([getRoom(reservation.roomId), getGuest(reservation.primaryGuestId)]);
        const names = room ? await resolveNames([room]) : { hotels: new Map(), types: new Map() };
        if (!card.isConnected) {
            return;
        }

        const nights = nightsBetween(reservation.checkInDate, reservation.checkOutDate);
        const roomText = room
            ? `${room.roomNumber} · ${typeName(names, room.roomTypeId)} · ${hotelName(names, room.hotelId)}`
            : `ID ${reservation.roomId}`;

        const rows = [
            ['Código de confirmación', reservation.confirmationCode],
            ['Estado', statusBadge(reservation.status)],
            ['Habitación', roomText],
            ['Huésped', guest
                ? el('a', { href: `#huespedes/${guest.id}`, text: `${guest.firstName} ${guest.lastName}` })
                : `ID ${reservation.primaryGuestId}`],
            ['Entrada', formatDate(reservation.checkInDate)],
            ['Salida', formatDate(reservation.checkOutDate)],
            ['Noches', String(nights)],
            ['Adultos', String(reservation.adults)],
            ['Niños', String(reservation.children)],
            ['Tarifa por noche', formatMoney(reservation.nightlyRate)],
            ['Solicitudes especiales', reservation.specialRequests ?? '—'],
        ];
        if (reservation.cancellationReason) {
            rows.push(['Motivo de cancelación', reservation.cancellationReason]);
        }

        const details = el('dl', { class: 'details' },
            ...rows.flatMap(([term, value]) => [el('dt', { text: term }), el('dd', {}, value)]));

        const actions = el('div', { class: 'actions' }, back);
        status.replaceWith(details);
        card.append(actions);

        if (CANCELLABLE.includes(reservation.status)) {
            const cancelButton = el('button', { type: 'button', class: 'btn danger', text: 'Cancelar reserva' });
            actions.append(cancelButton);

            const openForm = () => {
                cancelButton.disabled = true;
                card.append(buildCancelForm(container, reservation, () => { cancelButton.disabled = false; }));
            };
            cancelButton.addEventListener('click', openForm);
            if (openCancel) {
                openForm();
            }
        }
    } catch (error) {
        if (!card.isConnected) {
            return;
        }
        status.textContent = 'No se pudo cargar la reserva.';
        card.append(el('div', { class: 'actions' }, back));
        showError(error); // un 404 muestra el mensaje de la API
    }
}

function buildCancelForm(container, reservation, onClose) {
    const reason = el('textarea', { id: 'c-reason', rows: 3, maxLength: 500 });
    const confirm = el('button', { type: 'submit', class: 'btn danger', text: 'Confirmar cancelación' });
    const keep = el('button', { type: 'button', class: 'btn secondary', text: 'No cancelar' });

    const form = el('form', { class: 'inline-form', novalidate: true },
        el('p', { text: 'Esta acción libera las fechas de la habitación y no se puede deshacer.' }),
        field('c-reason', 'Motivo de la cancelación *', reason),
        el('div', { class: 'actions' }, confirm, keep));

    keep.addEventListener('click', () => {
        form.remove();
        onClose();
    });

    let saving = false;
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (saving) {
            return; // bloquea el envío doble
        }
        clearMessages();

        const text = reason.value.trim();
        if (!text) {
            showMessage('El motivo de cancelación es obligatorio.', 'error');
            return;
        }

        saving = true;
        confirm.disabled = true;
        confirm.textContent = 'Cancelando…';
        try {
            await api.patch(`/api/reservations/${reservation.reservationId}/cancel`, { cancellationReason: text });
            showMessage('Reserva cancelada. Las fechas quedaron libres.', 'success');
        } catch (error) {
            showError(error); // por ejemplo 409 si ya tiene estadía
        } finally {
            // Se vuelve a dibujar para mostrar el estado real que tiene el servidor.
            if (container.isConnected) {
                container.replaceChildren();
                renderReservationDetail(container, String(reservation.reservationId));
            }
        }
    });

    return form;
}