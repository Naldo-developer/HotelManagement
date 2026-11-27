
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';

export function renderMaintenance(container) {
    let rooms = [];
    let records = [];
    let editingId = null;
    let busy = false;
    let loadVersion = 0;

    const roomSelect = el('select', { required: true });
    const startInput = el('input', { type: 'date', required: true });
    const endInput = el('input', { type: 'date', required: true });

    const categoryInput = el('input', {
        type: 'text',
        maxlength: 50,
        placeholder: 'Ej. Electricidad'
    });

    const descriptionInput = el('textarea', {
        rows: 4,
        required: true,
        placeholder: 'Describe el trabajo que debe realizarse'
    });

    const costInput = el('input', {
        type: 'number',
        min: 0,
        max: '9999999999.99',
        step: '0.01',
        value: '0'
    });

    const filterRoom = el('select', {});
    const searchInput = el('input', {
        type: 'search',
        placeholder: 'Buscar por descripción, estado...'
    });

    const results = el('div', { class: 'results' });

    const saveButton = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Registrar mantenimiento',
        disabled: true
    });

    function field(label, control) {
        return el('label', { class: 'field' },
            el('span', { text: label }),
            control
        );
    }

    function roomName(id) {
        const room = rooms.find(r => r.roomId === id);

        return room
            ? `${room.roomNumber} · Hotel #${room.hotelId}`
            : `Habitación #${id}`;
    }

    async function loadRooms() {
        const all = [];

        for (let page = 1; ; page++) {
            const batch = await api.get('/api/rooms', {
                page,
                pageSize: 100
            });

            all.push(...batch);

            if (batch.length < 100) break;
        }

        rooms = all;

        roomSelect.replaceChildren(
            el('option', {
                value: '',
                text: 'Selecciona una habitación'
            }),
            ...rooms.map(room => el('option', {
                value: String(room.roomId),
                text: roomName(room.roomId)
            }))
        );

        filterRoom.replaceChildren(
            el('option', { value: '', text: 'Todas las habitaciones' }),
            ...rooms.map(room => el('option', {
                value: String(room.roomId),
                text: roomName(room.roomId)
            }))
        );

        saveButton.disabled = rooms.length === 0;
    }

    function renderList() {
        const term = searchInput.value.trim().toLowerCase();

        const items = records.filter(item =>
            [
                roomName(item.roomId),
                item.description,
                item.category,
                item.status
            ].join(' ').toLowerCase().includes(term)
        );

        results.replaceChildren();

        if (!items.length) {
            results.append(el('section', { class: 'card' },
                el('p', {
                    class: 'muted',
                    text: 'No se encontraron mantenimientos.'
                })
            ));

            return;
        }

        const table = el('table', {},
            el('thead', {},
                el('tr', {},
                    ...[
                        'Habitación', 'Categoría', 'Descripción',
                        'Inicio', 'Fin', 'Estado', 'Costo', 'Acción'
                    ].map(text => el('th', { text }))
                )
            ),
            el('tbody', {},
                ...items.map(item => {
                    const action = el('button', {
                        type: 'button',
                        class: 'btn secondary',
                        text: 'Cerrar'
                    });

                    action.addEventListener('click', () => {
                        editingId = item.maintenanceId;
                        costInput.value = '0';
                        closePanel.hidden = false;
                        closeTitle.textContent =
                            `Cerrar mantenimiento #${editingId}`;

                        closePanel.scrollIntoView({
                            behavior: 'smooth',
                            block: 'center'
                        });
                    });

                    const closed = ['COMPLETED', 'CANCELLED']
                        .includes(item.status);

                    return el('tr', {},
                        el('td', { text: roomName(item.roomId) }),
                        el('td', { text: item.category || 'General' }),
                        el('td', { text: item.description }),
                        el('td', { text: item.startDate }),
                        el('td', { text: item.endDate }),
                        el('td', {},
                            el('span', {
                                class: closed
                                    ? 'aurora-badge'
                                    : 'aurora-badge success',
                                text: item.status
                            })
                        ),
                        el('td', {
                            text: Number(item.cost).toFixed(2)
                        }),
                        el('td', {}, closed ? '—' : action)
                    );
                })
            )
        );

        results.append(el('section', { class: 'card' },
            el('div', { class: 'card-header' },
                el('h2', { text: 'Historial de mantenimiento' }),
                el('span', {
                    class: 'aurora-badge',
                    text: `${items.length} registros`
                })
            ),
            el('div', { class: 'table-wrap' }, table)
        ));
    }

    async function load() {
        const version = ++loadVersion;

        try {
            const data = await api.get('/api/maintenance', {
                roomId: filterRoom.value
            });

            if (!results.isConnected || version !== loadVersion) return;

            records = data;
            renderList();
        } catch (error) {
            if (results.isConnected) showError(error);
        }
    }

    const closeTitle = el('h2', {
        text: 'Cerrar mantenimiento'
    });

    const confirmClose = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Confirmar cierre'
    });

    const cancelClose = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Cancelar'
    });

    const closeForm = el('form', { class: 'form-grid' },
        field('Costo final', costInput),
        el('div', { class: 'form-actions' },
            confirmClose,
            cancelClose
        )
    );

    const closePanel = el('section', {
        class: 'card aurora-module'
    }, closeTitle, closeForm);

    closePanel.hidden = true;

    cancelClose.addEventListener('click', () => {
        editingId = null;
        closePanel.hidden = true;
    });

    closeForm.addEventListener('submit', async event => {
        event.preventDefault();

        if (busy || !editingId) return;

        const cost = Number(costInput.value);

        if (!Number.isFinite(cost) || cost < 0) {
            showMessage('Ingresa un costo válido.', 'error');
            return;
        }

        if (!window.confirm('¿Confirmar el cierre del mantenimiento?')) {
            return;
        }

        busy = true;
        confirmClose.disabled = true;

        try {
            await api.patch(
                `/api/maintenance/${editingId}/close`,
                { cost }
            );

            editingId = null;
            closePanel.hidden = true;

            showMessage('Mantenimiento cerrado correctamente.', 'success');
            await load();
        } catch (error) {
            showError(error);
        } finally {
            busy = false;
            confirmClose.disabled = false;
        }
    });

    const form = el('form', { class: 'form-grid' },
        field('Habitación', roomSelect),
        field('Categoría', categoryInput),
        field('Fecha de inicio', startInput),
        field('Fecha de finalización', endInput),
        field('Descripción', descriptionInput),
        el('div', { class: 'form-actions' }, saveButton)
    );

    form.addEventListener('submit', async event => {
        event.preventDefault();

        if (busy) return;

        if (!roomSelect.value || !descriptionInput.value.trim()) {
            showMessage('Completa los campos obligatorios.', 'error');
            return;
        }

        if (endInput.value <= startInput.value) {
            showMessage(
                'La fecha final debe ser posterior a la inicial.',
                'error'
            );
            return;
        }

        busy = true;
        saveButton.disabled = true;

        try {
            await api.post('/api/maintenance', {
                roomId: Number(roomSelect.value),
                startDate: startInput.value,
                endDate: endInput.value,
                category: categoryInput.value.trim() || null,
                description: descriptionInput.value.trim()
            });

            form.reset();

            showMessage('Mantenimiento registrado correctamente.', 'success');
            await load();
        } catch (error) {
            showError(error);
        } finally {
            busy = false;
            saveButton.disabled = rooms.length === 0;
        }
    });

    filterRoom.addEventListener('change', load);
    searchInput.addEventListener('input', renderList);

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', {
                class: 'eyebrow',
                text: 'INFRAESTRUCTURA / MANTENIMIENTO'
            }),
            el('h1', { text: 'Cuidado de las instalaciones' }),
            el('p', {
                class: 'muted',
                text: 'Planifica y registra los trabajos de mantenimiento del hotel.'
            }),
            form
        ),
        closePanel,
        el('section', { class: 'card' },
            el('div', { class: 'form-grid' },
                field('Filtrar habitación', filterRoom),
                field('Buscar mantenimiento', searchInput)
            )
        ),
        results
    );

    loadRooms().catch(error => {
        if (container.isConnected) showError(error);
    });

    load();
}
