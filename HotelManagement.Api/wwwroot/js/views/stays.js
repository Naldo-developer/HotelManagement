
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';
import { canManagePayments } from '../permissions.js';

export function renderStays(container, initialReservationId = '') {
    let currentStay = null;

    const reservationInput = el('input', {
        type: 'number',
        min: 1,
        required: true,
        placeholder: 'Ej. 12'
    });

    reservationInput.value = initialReservationId;

    const notesInput = el('textarea', {
        maxlength: 1000,
        placeholder: 'Observaciones de la estadía...',
        rows: 3
    });

    const results = el('div', { class: 'results' });

    function field(label, control) {
        return el('div', { class: 'field' },
            el('label', {}, label),
            control
        );
    }

    function button(text, action, secondary = false) {
        const control = el('button', {
            type: 'button',
            class: secondary ? 'btn secondary' : 'btn',
            text
        });

        control.addEventListener('click', async () => {
            control.disabled = true;

            try {
                await action();
            } catch (error) {
                if (control.isConnected) showError(error);
            } finally {
                control.disabled = false;
            }
        });

        return control;
    }

    function reservationId() {
        const id = Number(reservationInput.value);

        if (!Number.isSafeInteger(id) || id <= 0) {
            throw new Error('Ingresa un ID de reserva válido.');
        }

        return id;
    }

    function renderDetails(stay) {
        if (!results.isConnected) return;

        currentStay = stay;

        const active = stay.status === 'ACTIVE';

        results.replaceChildren(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', { text: `Estadía #${stay.stayId}` }),
                    el('span', {
                        class: active ? 'aurora-badge success' : 'aurora-badge',
                        text: active ? 'En curso' : stay.status
                    })
                ),
                el('dl', { class: 'details' },
                    ...[
                        ['ID de reserva para caja', stay.reservationId],
                        ['Habitación ID', stay.roomId],
                        ['Entrada', new Date(stay.checkInAt).toLocaleString('es-PE')],
                        ['Salida', stay.checkOutAt
                            ? new Date(stay.checkOutAt).toLocaleString('es-PE')
                            : 'Pendiente'],
                        ['Observaciones', stay.notes || 'Sin observaciones']
                    ].flatMap(([label, value]) => [
                        el('dt', { text: label }),
                        el('dd', { text: String(value) })
                    ])
                ),
                el('div', { class: 'actions' },
                    el('a', {
                        href: `#reservas/${stay.reservationId}`,
                        class: 'btn secondary',
                        text: 'Ver reserva'
                    }),
                    ...(canManagePayments() ? [
                        el('a', {
                            href: `#pagos/${stay.reservationId}`,
                            class: 'btn secondary',
                            text: 'Consultar pagos'
                        })
                    ] : []),
                    ...(active ? [
                        button('Realizar check-out', checkOut)
                    ] : [])
                )
            )
        );
    }

    async function search() {
        const id = reservationId();

        currentStay = null;
        results.replaceChildren(
            el('p', { class: 'muted', text: 'Consultando estadía...' })
        );

        try {
            const stay = await api.get(`/api/stays/by-reservation/${id}`);

            if (
                !results.isConnected ||
                Number(reservationInput.value) !== id
            ) {
                return;
            }

            renderDetails(stay);
        } catch (error) {
            if (!results.isConnected) return;

            results.replaceChildren();

            if (error.status !== 404) throw error;

            results.append(
                el('p', {
                    class: 'muted',
                    text: 'No existe una estadía para esta reserva. Puedes realizar el check-in.'
                })
            );
        }
    }

    async function checkIn() {
        const id = reservationId();

        if (!window.confirm(`¿Registrar la entrada de la reserva #${id}?`)) {
            return;
        }

        const stay = await api.post('/api/stays/check-in', {
            reservationId: id,
            notes: notesInput.value.trim() || null
        });

        if (!results.isConnected) return;

        renderDetails(stay);
        showMessage('Check-in registrado correctamente.', 'success');
    }

    async function checkOut() {
        if (!currentStay) return;

        if (!window.confirm('¿Confirmar la salida de esta estadía?')) {
            return;
        }

        const stay = await api.post('/api/stays/check-out', {
            stayId: currentStay.stayId,
            notes: notesInput.value.trim() || null
        });

        if (!results.isConnected) return;

        renderDetails(stay);
        showMessage('Check-out completado correctamente.', 'success');
    }

    reservationInput.addEventListener('input', () => {
        currentStay = null;
        results.replaceChildren();
    });

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', { class: 'eyebrow', text: 'OPERACIONES / RECEPCIÓN' }),
            el('h1', { text: 'Gestión de estadías' }),
            el('p', {
                class: 'muted',
                text: 'Gestiona la llegada, permanencia y salida de los huéspedes.'
            }),
            el('div', { class: 'form-grid' },
                field('ID de reserva', reservationInput),
                field('Observaciones', notesInput),
                el('div', { class: 'form-actions' },
                    button('Consultar estadía', search, true),
                    button('Registrar check-in', checkIn)
                )
            )
        ),
        results
    );

    if (initialReservationId) {
        search().catch(error => {
            if (results.isConnected) showError(error);
        });
    }
}
