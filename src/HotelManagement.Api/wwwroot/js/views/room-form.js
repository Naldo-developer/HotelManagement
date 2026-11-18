import { api } from '../api.js';
import { getHotels, getRoomTypes, ROOM_STATUSES } from '../catalogs.js';
import { clearMessages, el, queueMessage, setSelectOptions, showError, showMessage } from '../ui.js';

function field(id, label, control, hint) {
    return el('div', { class: 'field' },
        el('label', { for: id, text: label }),
        control,
        hint ? el('small', { class: 'muted', text: hint }) : '');
}

export async function renderRoomForm(container) {
    const hotelSelect = el('select', { id: 'r-hotel', name: 'hotelId', disabled: true },
        el('option', { value: '', text: 'Cargando hoteles…' }));
    const typeSelect = el('select', { id: 'r-type', name: 'roomTypeId', disabled: true },
        el('option', { value: '', text: 'Elige un hotel primero' }));
    const numberInput = el('input', { id: 'r-number', name: 'roomNumber', type: 'text', maxLength: 20, autocomplete: 'off' });
    const floorInput = el('input', { id: 'r-floor', name: 'floor', type: 'text', maxLength: 20, autocomplete: 'off' });
    const statusSelect = el('select', { id: 'r-status', name: 'operationalStatus' },
        ...Object.entries(ROOM_STATUSES).map(([value, label]) => el('option', { value, text: label })));
    statusSelect.value = 'ACTIVE';

    const submit = el('button', { type: 'submit', class: 'btn', text: 'Guardar habitación' });
    const cancel = el('a', { href: '#habitaciones', class: 'btn secondary', text: 'Cancelar' });

    const form = el('form', { class: 'form-grid', novalidate: true },
        field('r-hotel', 'Hotel *', hotelSelect),
        field('r-type', 'Tipo de habitación *', typeSelect),
        field('r-number', 'Número de habitación *', numberInput),
        field('r-floor', 'Piso', floorInput, 'Opcional'),
        field('r-status', 'Estado operativo *', statusSelect),
        el('div', { class: 'form-actions' }, submit, cancel));

    container.append(el('section', { class: 'card' },
        el('h1', { text: 'Nueva habitación' }),
        el('p', { class: 'muted', text: 'Los campos con * son obligatorios.' }),
        form));

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
            if (token !== typesToken || !form.isConnected) {
                return;
            }
            if (types.length === 0) {
                setSelectOptions(typeSelect, 'Este hotel no tiene tipos de habitación', [], 'roomTypeId');
                return;
            }
            setSelectOptions(typeSelect, 'Selecciona un tipo', types, 'roomTypeId');
            typeSelect.disabled = false;
        } catch (error) {
            if (token !== typesToken || !form.isConnected) {
                return;
            }
            setSelectOptions(typeSelect, 'No se pudieron cargar los tipos', [], 'roomTypeId');
            showError(error);
        }
    }

    hotelSelect.addEventListener('change', () => loadTypes(hotelSelect.value));

    function validate() {
        if (!hotelSelect.value) {
            return 'Selecciona un hotel.';
        }
        if (!typeSelect.value) {
            return 'Selecciona un tipo de habitación.';
        }
        if (!numberInput.value.trim()) {
            return 'El número de habitación es obligatorio.';
        }
        return null;
    }

    let saving = false;

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (saving) {
            return; // bloquea el envío doble
        }

        clearMessages();
        const problem = validate();
        if (problem) {
            showMessage(problem, 'error');
            return;
        }

        const body = {
            hotelId: Number(hotelSelect.value),
            roomTypeId: Number(typeSelect.value),
            roomNumber: numberInput.value.trim(),
            floor: floorInput.value.trim() || null,
            operationalStatus: statusSelect.value,
        };

        saving = true;
        submit.disabled = true;
        submit.textContent = 'Guardando…';
        try {
            const room = await api.post('/api/rooms', body);
            queueMessage('Habitación creada correctamente.', 'success');
            window.location.hash = `#habitaciones/${room.roomId}`;
        } catch (error) {
            // El formulario no se vuelve a dibujar, así que los valores se conservan.
            showError(error);
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } finally {
            saving = false;
            submit.disabled = false;
            submit.textContent = 'Guardar habitación';
        }
    });

    try {
        const hotels = await getHotels();
        if (!form.isConnected) {
            return;
        }
        setSelectOptions(hotelSelect, 'Selecciona un hotel', hotels, 'hotelId');
        hotelSelect.disabled = false;
        if (hotels.length === 1) {
            hotelSelect.value = String(hotels[0].hotelId);
            await loadTypes(hotelSelect.value);
        }
    } catch (error) {
        if (form.isConnected) {
            setSelectOptions(hotelSelect, 'No se pudieron cargar los hoteles', [], 'hotelId');
            showError(error);
        }
    }
}