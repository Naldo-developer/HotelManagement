import { api } from '../api.js';
import { getHotels, getRoomTypes, ROOM_STATUSES } from '../catalogs.js';
import { clearMessages, el, queueMessage, setSelectOptions, showError, showMessage } from '../ui.js';

function field(id, label, control, hint) {
    return el('div', { class: 'field' },
        el('label', { for: id, text: label }),
        control,
        hint ? el('small', { class: 'muted', text: hint }) : '');
}

// Sin roomId: alta de una habitación nueva. Con roomId: edición de esa habitación.
export async function renderRoomForm(container, roomId = null) {
    const isEdit = roomId !== null;
    const title = isEdit ? 'Editar habitación' : 'Nueva habitación';
    const backHref = isEdit ? `#habitaciones/${roomId}` : '#habitaciones';

    if (isEdit && !/^[1-9]\d*$/.test(roomId)) {
        container.append(el('section', { class: 'card' },
            el('h1', { text: title }),
            el('p', { class: 'muted', text: 'El identificador de la habitación no es válido.' }),
            el('div', { class: 'actions' },
                el('a', { href: '#habitaciones', class: 'btn secondary', text: '← Volver al listado' }))));
        showMessage('El identificador de la habitación no es válido.', 'error');
        return;
    }

    const hotelSelect = el('select', { id: 'r-hotel', name: 'hotelId', disabled: true },
        el('option', { value: '', text: 'Cargando hoteles…' }));
    const typeSelect = el('select', { id: 'r-type', name: 'roomTypeId', disabled: true },
        el('option', { value: '', text: 'Elige un hotel primero' }));
    const numberInput = el('input', { id: 'r-number', name: 'roomNumber', type: 'text', maxLength: 20, autocomplete: 'off' });
    const floorInput = el('input', { id: 'r-floor', name: 'floor', type: 'text', maxLength: 20, autocomplete: 'off' });

    // El estado solo se elige al crear; editar no lo modifica.
    const statusSelect = el('select', { id: 'r-status', name: 'operationalStatus' },
        ...Object.entries(ROOM_STATUSES).map(([value, label]) => el('option', { value, text: label })));
    statusSelect.value = 'ACTIVE';

    const submit = el('button', { type: 'submit', class: 'btn', text: isEdit ? 'Guardar cambios' : 'Guardar habitación' });
    const cancel = el('a', { href: backHref, class: 'btn secondary', text: 'Cancelar' });

    const form = el('form', { class: 'form-grid', novalidate: true },
        field('r-hotel', 'Hotel *', hotelSelect),
        field('r-type', 'Tipo de habitación *', typeSelect),
        field('r-number', 'Número de habitación *', numberInput),
        field('r-floor', 'Piso', floorInput, 'Opcional'),
        ...(isEdit ? [] : [field('r-status', 'Estado operativo *', statusSelect)]),
        el('div', { class: 'form-actions' }, submit, cancel));

    const card = el('section', { class: 'card' },
        el('h1', { text: title }),
        el('p', { class: 'muted', text: isEdit
                ? 'Los campos con * son obligatorios. El estado operativo no se edita aquí.'
                : 'Los campos con * son obligatorios.' }),
        form);
    container.append(card);

    let typesToken = 0;

    async function loadTypes(hotelId, selected = '') {
        const token = ++typesToken;
        typeSelect.disabled = true;

        if (!hotelId) {
            setSelectOptions(typeSelect, 'Elige un hotel primero', [], 'roomTypeId');
            return;
        }

        setSelectOptions(typeSelect, 'Cargando tipos…', [], 'roomTypeId');
        try {
            const types = await getRoomTypes(hotelId);
            if (token !== typesToken || !card.isConnected) {
                return;
            }
            if (types.length === 0) {
                setSelectOptions(typeSelect, 'Este hotel no tiene tipos de habitación', [], 'roomTypeId');
                return;
            }
            setSelectOptions(typeSelect, 'Selecciona un tipo', types, 'roomTypeId', selected);
            typeSelect.disabled = false;
        } catch (error) {
            if (token !== typesToken || !card.isConnected) {
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
        };
        if (!isEdit) {
            body.operationalStatus = statusSelect.value;
        }

        saving = true;
        submit.disabled = true;
        const label = submit.textContent;
        submit.textContent = 'Guardando…';
        try {
            const room = isEdit
                ? await api.put(`/api/rooms/${roomId}`, body)
                : await api.post('/api/rooms', body);
            queueMessage(isEdit ? 'Habitación actualizada correctamente.' : 'Habitación creada correctamente.', 'success');
            window.location.hash = `#habitaciones/${room.roomId}`;
        } catch (error) {
            // El formulario no se vuelve a dibujar, así que los valores se conservan.
            showError(error);
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } finally {
            saving = false;
            submit.disabled = false;
            submit.textContent = label;
        }
    });

    // En edición se carga primero la habitación para rellenar el formulario.
    let existing = null;
    if (isEdit) {
        submit.disabled = true;
        try {
            existing = await api.get(`/api/rooms/${roomId}`);
        } catch (error) {
            if (card.isConnected) {
                card.replaceChildren(
                    el('h1', { text: title }),
                    el('p', { class: 'muted', text: 'No se pudo cargar la habitación.' }),
                    el('div', { class: 'actions' },
                        el('a', { href: '#habitaciones', class: 'btn secondary', text: '← Volver al listado' })));
                showError(error); // un 404 muestra el mensaje de la API
            }
            return;
        }
        if (!card.isConnected) {
            return;
        }
        numberInput.value = existing.roomNumber;
        floorInput.value = existing.floor ?? '';
        submit.disabled = false;
    }

    try {
        const hotels = await getHotels();
        if (!card.isConnected) {
            return;
        }
        setSelectOptions(hotelSelect, 'Selecciona un hotel', hotels, 'hotelId', existing?.hotelId ?? '');
        hotelSelect.disabled = false;

        if (existing) {
            await loadTypes(hotelSelect.value, existing.roomTypeId);
        } else if (hotels.length === 1) {
            hotelSelect.value = String(hotels[0].hotelId);
            await loadTypes(hotelSelect.value);
        }
    } catch (error) {
        if (card.isConnected) {
            setSelectOptions(hotelSelect, 'No se pudieron cargar los hoteles', [], 'hotelId');
            showError(error);
        }
    }
}