import { api } from '../api.js';
import { getHotels, getRoomTypes, hotelName, resolveNames, typeName } from '../catalogs.js';
import { addDaysIso, formatDate, nightsBetween, todayIso } from '../format.js';
import { clearMessages, el, queueMessage, setSelectOptions, showError, showMessage } from '../ui.js';

function field(id, label, control) {
    return el('div', { class: 'field' }, el('label', { for: id, text: label }), control);
}

export async function renderReservationForm(container) {
    const state = {
        room: null,
        guest: null,
        search: null, // criterios con los que se buscó: { hotelId, hotelLabel, checkIn, checkOut, roomTypeId }
        names: { hotels: new Map(), types: new Map() },
        saving: false,
        searching: false,
    };

    // ---------------- 1. Hotel y fechas
    const hotelSelect = el('select', { id: 'b-hotel', disabled: true }, el('option', { value: '', text: 'Cargando hoteles…' }));
    const typeSelect = el('select', { id: 'b-type', disabled: true }, el('option', { value: '', text: 'Elige un hotel primero' }));
    const checkIn = el('input', { id: 'b-in', type: 'date', value: todayIso() });
    const checkOut = el('input', { id: 'b-out', type: 'date', value: addDaysIso(todayIso(), 1) });
    const searchButton = el('button', { type: 'button', class: 'btn', text: 'Buscar disponibilidad' });

    const stepSearch = el('section', { class: 'card' },
        el('h2', { class: 'step-title', text: '1. Hotel y fechas' }),
        el('div', { class: 'filters' },
            field('b-hotel', 'Hotel *', hotelSelect),
            field('b-type', 'Tipo de habitación', typeSelect),
            field('b-in', 'Entrada *', checkIn),
            field('b-out', 'Salida *', checkOut),
            el('div', { class: 'filters-actions' }, searchButton)));

    // ---------------- 2. Habitación
    const roomsBox = el('div', {}, el('p', { class: 'muted', text: 'Busca disponibilidad para ver las habitaciones libres.' }));
    const stepRoom = el('section', { class: 'card' },
        el('h2', { class: 'step-title', text: '2. Habitación disponible' }),
        roomsBox);

    // ---------------- 3. Huésped
    const guestInput = el('input', { id: 'b-guest', type: 'search', maxLength: 100, placeholder: 'Nombre o número de documento' });
    const guestButton = el('button', { type: 'button', class: 'btn secondary', text: 'Buscar huésped' });
    const guestsBox = el('div', {});
    const stepGuest = el('section', { class: 'card' },
        el('h2', { class: 'step-title', text: '3. Huésped' }),
        el('div', { class: 'filters' },
            field('b-guest', 'Buscar huésped', guestInput),
            el('div', { class: 'filters-actions' },
                guestButton,
                el('a', { href: '#huespedes/nuevo', class: 'btn secondary', target: '_blank', rel: 'noopener', text: 'Registrar huésped nuevo' }))),
        guestsBox);

    // ---------------- 4. Detalles y confirmación
    const adultsInput = el('input', { id: 'b-adults', type: 'number', min: '1', value: '1' });
    const childrenInput = el('input', { id: 'b-children', type: 'number', min: '0', value: '0' });
    const requestsInput = el('textarea', { id: 'b-requests', rows: 3, maxLength: 500 });
    const summaryBox = el('div', { class: 'summary' });
    const confirmButton = el('button', { type: 'button', class: 'btn', text: 'Confirmar reserva', disabled: true });

    const stepConfirm = el('section', { class: 'card' },
        el('h2', { class: 'step-title', text: '4. Detalles y confirmación' }),
        el('div', { class: 'form-grid' },
            field('b-adults', 'Adultos *', adultsInput),
            field('b-children', 'Niños', childrenInput),
            field('b-requests', 'Solicitudes especiales', requestsInput)),
        summaryBox,
        el('div', { class: 'actions' },
            confirmButton,
            el('a', { href: '#reservas', class: 'btn secondary', text: 'Cancelar' })));

    container.append(
        el('div', { class: 'card-header' }, el('h1', { text: 'Nueva reserva' })),
        stepSearch, stepRoom, stepGuest, stepConfirm);

    // ---------------- Resumen
    function updateSummary() {
        summaryBox.replaceChildren();
        const rows = [];

        if (state.search) {
            const nights = nightsBetween(state.search.checkIn, state.search.checkOut);
            rows.push(['Hotel', state.search.hotelLabel]);
            rows.push(['Fechas', `${formatDate(state.search.checkIn)} → ${formatDate(state.search.checkOut)} (${nights} ${nights === 1 ? 'noche' : 'noches'})`]);
        }
        rows.push(['Habitación', state.room
            ? `${state.room.roomNumber} · ${typeName(state.names, state.room.roomTypeId)}`
            : 'Sin elegir']);
        rows.push(['Huésped', state.guest ? `${state.guest.firstName} ${state.guest.lastName}` : 'Sin elegir']);

        summaryBox.append(el('dl', { class: 'details' },
            ...rows.flatMap(([term, value]) => [el('dt', { text: term }), el('dd', { text: value })])));

        confirmButton.disabled = state.saving || !(state.room && state.guest);
    }

    // Si cambian los criterios, la habitación elegida ya no es válida.
    function invalidateSearch() {
        state.room = null;
        state.search = null;
        roomsBox.replaceChildren(el('p', { class: 'muted', text: 'Cambiaste los criterios: vuelve a buscar disponibilidad.' }));
        updateSummary();
    }

    // ---------------- Tipos por hotel
    let typesToken = 0;
    async function loadTypes(hotelId) {
        const token = ++typesToken;
        typeSelect.disabled = true;
        if (!hotelId) {
            setSelectOptions(typeSelect, 'Elige un hotel primero', [], 'roomTypeId');
            return;
        }
        setSelectOptions(typeSelect, 'Cargando tipos…', [], 'roomTypeId');
        try {
            const types = await getRoomTypes(hotelId);
            if (token !== typesToken || !stepSearch.isConnected) {
                return;
            }
            setSelectOptions(typeSelect, 'Todos', types, 'roomTypeId');
            typeSelect.disabled = false;
        } catch (error) {
            if (token === typesToken && stepSearch.isConnected) {
                setSelectOptions(typeSelect, 'No se pudieron cargar los tipos', [], 'roomTypeId');
                showError(error);
            }
        }
    }

    hotelSelect.addEventListener('change', () => {
        invalidateSearch();
        loadTypes(hotelSelect.value);
    });
    [typeSelect, checkIn, checkOut].forEach((control) => control.addEventListener('change', invalidateSearch));

    // ---------------- Buscar disponibilidad
    async function searchAvailability({ clear = true } = {}) {
        if (state.searching) {
            return;
        }
        if (clear) {
            clearMessages();
        }

        if (!hotelSelect.value) {
            showMessage('Selecciona un hotel.', 'error');
            return;
        }
        if (!checkIn.value || !checkOut.value) {
            showMessage('Indica la fecha de entrada y la de salida.', 'error');
            return;
        }
        if (nightsBetween(checkIn.value, checkOut.value) < 1) {
            showMessage('La fecha de salida debe ser posterior a la de entrada.', 'error');
            return;
        }

        const criteria = {
            hotelId: hotelSelect.value,
            hotelLabel: hotelSelect.selectedOptions[0]?.textContent ?? '',
            checkIn: checkIn.value,
            checkOut: checkOut.value,
            roomTypeId: typeSelect.value,
        };

        state.searching = true;
        searchButton.disabled = true;
        try {
            const rooms = await api.get('/api/rooms/availability', {
                hotelId: criteria.hotelId,
                checkInDate: criteria.checkIn,
                checkOutDate: criteria.checkOut,
                roomTypeId: criteria.roomTypeId,
            });
            if (!stepSearch.isConnected) {
                return;
            }

            state.names = await resolveNames(rooms);
            state.search = criteria;
            state.room = null;
            renderRooms(rooms);
        } catch (error) {
            if (stepSearch.isConnected) {
                roomsBox.replaceChildren(el('p', { class: 'muted', text: 'No se pudo consultar la disponibilidad.' }));
                showError(error);
            }
        } finally {
            state.searching = false;
            searchButton.disabled = false;
            updateSummary();
        }
    }

    function renderRooms(rooms) {
        if (rooms.length === 0) {
            roomsBox.replaceChildren(el('p', { class: 'muted', text: 'No hay habitaciones disponibles para esos criterios. Prueba con otras fechas u otro tipo.' }));
            return;
        }

        const list = el('div', { class: 'choice-list' },
            ...rooms.map((room) => {
                const radio = el('input', { type: 'radio', name: 'room', value: String(room.roomId) });
                radio.addEventListener('change', () => {
                    state.room = room;
                    updateSummary();
                });
                return el('label', { class: 'choice' },
                    radio,
                    el('span', { text: `Habitación ${room.roomNumber} · ${typeName(state.names, room.roomTypeId)}${room.floor ? ` · Piso ${room.floor}` : ''}` }));
            }));

        roomsBox.replaceChildren(
            el('p', { class: 'muted', text: `${rooms.length} ${rooms.length === 1 ? 'habitación disponible' : 'habitaciones disponibles'}. Elige una:` }),
            list);
    }

    searchButton.addEventListener('click', () => searchAvailability());

    // ---------------- Buscar huésped
    async function searchGuests() {
        const text = guestInput.value.trim();
        if (!text) {
            showMessage('Escribe un nombre o un número de documento para buscar.', 'error');
            return;
        }

        clearMessages();
        guestButton.disabled = true;
        try {
            let guests = await api.get('/api/guests', { name: text, pageSize: 10 });
            if (guests.length === 0) {
                guests = await api.get('/api/guests', { documentNumber: text, pageSize: 10 });
            }
            if (!stepGuest.isConnected) {
                return;
            }
            renderGuests(guests);
        } catch (error) {
            if (stepGuest.isConnected) {
                showError(error);
            }
        } finally {
            guestButton.disabled = false;
        }
    }

    function renderGuests(guests) {
        if (guests.length === 0) {
            guestsBox.replaceChildren(el('p', { class: 'muted', text: 'No se encontraron huéspedes. Puedes registrarlo con el botón "Registrar huésped nuevo".' }));
            return;
        }

        const list = el('div', { class: 'choice-list' },
            ...guests.map((guest) => {
                const radio = el('input', { type: 'radio', name: 'guest', value: String(guest.id) });
                radio.checked = state.guest?.id === guest.id;
                radio.addEventListener('change', () => {
                    state.guest = guest;
                    updateSummary();
                });
                const document = guest.documentType && guest.documentNumber ? ` · ${guest.documentType} ${guest.documentNumber}` : '';
                return el('label', { class: 'choice' }, radio, el('span', { text: `${guest.firstName} ${guest.lastName}${document}` }));
            }));

        guestsBox.replaceChildren(
            el('p', { class: 'muted', text: 'Se muestran hasta 10 resultados. Afina la búsqueda si no aparece.' }),
            list);
    }

    guestButton.addEventListener('click', searchGuests);
    guestInput.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            searchGuests();
        }
    });

    // ---------------- Confirmar
    confirmButton.addEventListener('click', async () => {
        if (state.saving) {
            return; // bloquea el envío doble
        }
        clearMessages();

        if (!state.room || !state.guest || !state.search) {
            showMessage('Elige una habitación disponible y un huésped.', 'error');
            return;
        }
        const adults = Number(adultsInput.value);
        const children = Number(childrenInput.value);
        if (!Number.isInteger(adults) || adults < 1) {
            showMessage('Debe haber al menos un adulto.', 'error');
            return;
        }
        if (!Number.isInteger(children) || children < 0) {
            showMessage('La cantidad de niños debe ser cero o mayor.', 'error');
            return;
        }

        state.saving = true;
        updateSummary();
        confirmButton.textContent = 'Reservando…';
        try {
            const reservation = await api.post('/api/reservations', {
                roomId: state.room.roomId,
                primaryGuestId: state.guest.id,
                checkInDate: state.search.checkIn,
                checkOutDate: state.search.checkOut,
                adults,
                children,
                specialRequests: requestsInput.value.trim() || null,
            });
            queueMessage(`Reserva creada. Código de confirmación: ${reservation.confirmationCode}.`, 'success');
            window.location.hash = `#reservas/${reservation.reservationId}`;
        } catch (error) {
            showError(error);
            window.scrollTo({ top: 0, behavior: 'smooth' });
            if (error.status === 409 && stepSearch.isConnected) {
                // Otra reserva ocupó la habitación: se vuelve a consultar la disponibilidad.
                showMessage('Elige otra habitación de la lista actualizada o cambia las fechas.', 'info');
                state.room = null;
                await searchAvailability({ clear: false });
            }
        } finally {
            state.saving = false;
            confirmButton.textContent = 'Confirmar reserva';
            updateSummary();
        }
    });

    updateSummary();

    // ---------------- Carga inicial de hoteles
    try {
        const hotels = await getHotels();
        if (!stepSearch.isConnected) {
            return;
        }
        hotels.forEach((hotel) => state.names.hotels.set(hotel.hotelId, hotel.name));
        setSelectOptions(hotelSelect, 'Selecciona un hotel', hotels, 'hotelId');
        hotelSelect.disabled = false;
        if (hotels.length === 1) {
            hotelSelect.value = String(hotels[0].hotelId);
            await loadTypes(hotelSelect.value);
        }
    } catch (error) {
        if (stepSearch.isConnected) {
            setSelectOptions(hotelSelect, 'No se pudieron cargar los hoteles', [], 'hotelId');
            showError(error);
        }
    }
}