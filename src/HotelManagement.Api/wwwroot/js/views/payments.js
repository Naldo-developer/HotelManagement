
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';

const METHODS = {
    CASH: 'Efectivo',
    CARD: 'Tarjeta',
    TRANSFER: 'Transferencia bancaria',
    DIGITAL_WALLET: 'Billetera digital',
    OTHER: 'Otro'
};

export function renderPayments(container, initialReservationId = '') {
    const reservationInput = el('input', {
        type: 'number',
        min: 1,
        required: true,
        placeholder: 'ID de reserva'
    });

    reservationInput.value = initialReservationId;

    const amountInput = el('input', {
        type: 'number',
        min: '0.01',
        max: '9999999999',
        step: '0.01',
        required: true,
        placeholder: '0.00'
    });

    const methodSelect = el('select', {},
        ...Object.entries(METHODS).map(([value, text]) =>
            el('option', { value, text })
        )
    );

    const referenceInput = el('input', {
        type: 'text',
        maxlength: 100,
        placeholder: 'Número de operación (opcional)'
    });

    const notesInput = el('textarea', {
        rows: 3,
        maxlength: 1000,
        placeholder: 'Observaciones del pago'
    });

    const results = el('div', { class: 'results' });

    let loadedReservation = null;
    let currentBalance = null;

    const money = value => Number(value).toLocaleString('es-PE', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function field(label, control) {
        return el('div', { class: 'field' },
            el('label', { text: label }),
            control
        );
    }

    function reservationId() {
        const id = Number(reservationInput.value);

        if (!Number.isSafeInteger(id) || id <= 0) {
            throw new Error('Ingresa una reserva válida.');
        }

        return id;
    }

    const loadButton = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Consultar saldo'
    });

    const saveButton = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Registrar pago',
        disabled: true
    });

    function reset() {
        loadedReservation = null;
        currentBalance = null;
        saveButton.disabled = true;
    }

    async function load() {
        reset();
        loadButton.disabled = true;

        try {
            const id = reservationId();

            results.replaceChildren(
                el('p', { class: 'muted', text: 'Consultando información financiera...' })
            );

            const [summary, history] = await Promise.all([
                api.get(`/api/payments/summary/by-reservation/${id}`),
                api.get(`/api/payments/by-reservation/${id}`)
            ]);

            if (!results.isConnected || Number(reservationInput.value) !== id) {
                return;
            }

            loadedReservation = id;
            currentBalance = Number(summary.balanceDue);

            amountInput.value = currentBalance > 0
                ? currentBalance.toFixed(2)
                : '';

            saveButton.disabled = currentBalance <= 0;

            renderSummary(summary, history);
        } catch (error) {
            if (results.isConnected) {
                results.replaceChildren();
                showError(error);
            }
        } finally {
            loadButton.disabled = false;
        }
    }

    function metric(label, amount, highlight = false) {
        return el('article', {
                class: highlight
                    ? 'aurora-metric highlighted'
                    : 'aurora-metric'
            },
            el('span', { text: label }),
            el('strong', { text: money(amount) })
        );
    }

    function renderSummary(summary, history) {
        const table = el('table', {},
            el('thead', {},
                el('tr', {},
                    ...['ID', 'Método', 'Importe', 'Estado', 'Referencia', 'Fecha']
                        .map(text => el('th', { text }))
                )
            ),
            el('tbody', {},
                ...history.map(payment => el('tr', {},
                    el('td', { text: String(payment.paymentId) }),
                    el('td', {
                        text: METHODS[payment.paymentMethod] || payment.paymentMethod
                    }),
                    el('td', { text: money(payment.amount) }),
                    el('td', { text: payment.status }),
                    el('td', { text: payment.reference || '—' }),
                    el('td', {
                        text: new Date(payment.paidAt).toLocaleString('es-PE')
                    })
                ))
            )
        );

        results.replaceChildren(
            el('section', { class: 'card' },
                el('h2', {
                    text: `Resumen financiero · Reserva #${summary.reservationId}`
                }),
                el('div', { class: 'aurora-metrics' },
                    metric('Alojamiento', summary.accommodationTotal),
                    metric('Consumos', summary.consumptionTotal),
                    metric('Total a pagar', summary.totalDue),
                    metric('Total pagado', summary.totalPaid),
                    metric('Saldo pendiente', summary.balanceDue, true)
                )
            ),
            el('section', { class: 'card' },
                el('h2', { text: 'Historial de pagos' }),
                history.length
                    ? el('div', { class: 'table-wrap' }, table)
                    : el('p', {
                        class: 'muted',
                        text: 'No se han registrado pagos para esta reserva.'
                    })
            )
        );
    }

    reservationInput.addEventListener('input', () => {
        reset();
        results.replaceChildren();
    });

    loadButton.addEventListener('click', load);

    const form = el('form', { class: 'form-grid' },
        field('ID de reserva', reservationInput),
        field('Importe', amountInput),
        field('Método de pago', methodSelect),
        field('Referencia', referenceInput),
        field('Observaciones', notesInput),
        el('div', { class: 'form-actions' },
            loadButton,
            saveButton
        )
    );

    form.addEventListener('submit', async event => {
        event.preventDefault();

        if (saveButton.disabled) return;

        const id = Number(reservationInput.value);
        const amount = Number(amountInput.value);

        if (loadedReservation !== id) {
            showMessage('Consulta primero el saldo.', 'error');
            return;
        }

        if (!Number.isFinite(amount) || amount <= 0 || amount > currentBalance) {
            showMessage('El importe debe ser positivo y no superar el saldo.', 'error');
            return;
        }

        if (!window.confirm(`¿Confirmar el pago de ${money(amount)}?`)) {
            return;
        }

        saveButton.disabled = true;

        try {
            await api.post('/api/payments', {
                reservationId: id,
                amount,
                paymentMethod: methodSelect.value,
                reference: referenceInput.value.trim() || null,
                notes: notesInput.value.trim() || null
            });

            if (!form.isConnected) return;

            referenceInput.value = '';
            notesInput.value = '';

            showMessage('Pago registrado correctamente.', 'success');

            await load();
        } catch (error) {
            if (form.isConnected) {
                showError(error);
                saveButton.disabled =
                    loadedReservation === null || currentBalance <= 0;
            }
        }
    });

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', { class: 'eyebrow', text: 'ADMINISTRACIÓN / CAJA' }),
            el('h1', { text: 'Gestión de pagos' }),
            el('p', {
                class: 'muted',
                text: 'Consulta saldos, registra cobros y revisa el historial financiero.'
            }),
            form
        ),
        results
    );

    if (initialReservationId) load();
}
