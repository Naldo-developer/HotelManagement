
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';

export function renderConsumptions(container, initialStayId = '') {
    const stayInput = el('input', {
        type: 'number',
        min: 1,
        required: true,
        placeholder: 'ID de estadía'
    });

    stayInput.value = initialStayId;

    const serviceSelect = el('select', { disabled: true },
        el('option', {
            value: '',
            text: 'Consulta una estadía primero'
        })
    );

    const quantityInput = el('input', {
        type: 'number',
        min: '0.01',
        max: '1000000',
        step: '0.01',
        value: '1',
        required: true
    });

    const notesInput = el('textarea', {
        maxlength: 1000,
        rows: 3,
        placeholder: 'Detalle adicional del consumo'
    });

    const results = el('div', { class: 'results' });
    const estimate = el('p', { class: 'muted' });

    let loadedStayId = null;
    let services = [];

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

    function updateEstimate() {
        const service = services.find(
            item => String(item.serviceId) === serviceSelect.value
        );

        const quantity = Number(quantityInput.value);

        estimate.textContent = service && quantity > 0
            ? `Importe estimado: ${money(service.defaultPrice * quantity)}`
            : '';
    }

    function getStayId() {
        const id = Number(stayInput.value);

        if (!Number.isSafeInteger(id) || id <= 0) {
            throw new Error('Ingresa una estadía válida.');
        }

        return id;
    }

    const loadButton = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Consultar estadía'
    });

    const saveButton = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Registrar consumo',
        disabled: true
    });

    function resetServices() {
        loadedStayId = null;
        services = [];
        serviceSelect.disabled = true;
        saveButton.disabled = true;
        estimate.textContent = '';

        serviceSelect.replaceChildren(
            el('option', { value: '', text: 'Consulta una estadía primero' })
        );
    }

    async function load() {
        resetServices();

        const id = getStayId();

        results.replaceChildren(
            el('p', { class: 'muted', text: 'Cargando consumos...' })
        );

        loadButton.disabled = true;

        try {
            const stay = await api.get(`/api/stays/${id}`);
            const room = await api.get(`/api/rooms/${stay.roomId}`);

            const [catalog, consumptions] = await Promise.all([
                api.get('/api/services', { hotelId: room.hotelId }),
                api.get(`/api/consumptions/by-stay/${id}`)
            ]);

            if (!results.isConnected || Number(stayInput.value) !== id) {
                return;
            }

            loadedStayId = id;
            services = catalog.filter(service => service.active);

            serviceSelect.replaceChildren(
                el('option', { value: '', text: 'Selecciona un servicio' }),
                ...services.map(service => el('option', {
                    value: String(service.serviceId),
                    text: `${service.name} — ${money(service.defaultPrice)}`
                }))
            );

            const active = stay.status === 'ACTIVE';

            serviceSelect.disabled = !active;
            saveButton.disabled = !active || services.length === 0;

            renderHistory(consumptions, stay);
        } catch (error) {
            if (!results.isConnected) return;

            results.replaceChildren();
            showError(error);
        } finally {
            loadButton.disabled = false;
        }
    }

    function renderHistory(items, stay) {
        const total = items.reduce(
            (sum, item) => sum + Number(item.subtotal), 0
        );

        const table = el('table', {},
            el('thead', {},
                el('tr', {},
                    ...['Servicio', 'Cantidad', 'Precio', 'Subtotal', 'Fecha']
                        .map(text => el('th', { text }))
                )
            ),
            el('tbody', {},
                ...items.map(item => el('tr', {},
                    el('td', { text: item.serviceName }),
                    el('td', { text: String(item.quantity) }),
                    el('td', { text: money(item.unitPrice) }),
                    el('td', { text: money(item.subtotal) }),
                    el('td', {
                        text: new Date(item.consumedAt).toLocaleString('es-PE')
                    })
                ))
            )
        );

        results.replaceChildren(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', { text: `Consumos · Estadía #${stay.stayId}` }),
                    el('strong', { text: `Total: ${money(total)}` })
                ),
                el('p', {
                    class: 'muted',
                    text: `Estado: ${stay.status}`
                }),
                items.length
                    ? el('div', { class: 'table-wrap' }, table)
                    : el('p', {
                        class: 'muted',
                        text: 'Todavía no existen consumos registrados.'
                    })
            )
        );
    }

    loadButton.addEventListener('click', load);

    stayInput.addEventListener('input', () => {
        resetServices();
        results.replaceChildren();
    });

    serviceSelect.addEventListener('change', updateEstimate);
    quantityInput.addEventListener('input', updateEstimate);

    const form = el('form', { class: 'form-grid' },
        field('ID de estadía', stayInput),
        field('Servicio', serviceSelect),
        field('Cantidad', quantityInput),
        field('Observaciones', notesInput),
        estimate,
        el('div', { class: 'form-actions' },
            loadButton,
            saveButton
        )
    );

    form.addEventListener('submit', async event => {
        event.preventDefault();

        if (saveButton.disabled) return;

        saveButton.disabled = true;

        try {
            const stayId = getStayId();
            const quantity = Number(quantityInput.value);
            const serviceId = Number(serviceSelect.value);

            if (loadedStayId !== stayId) {
                throw new Error('Consulta la estadía antes de registrar.');
            }

            if (!services.some(s => s.serviceId === serviceId)) {
                throw new Error('Selecciona un servicio válido.');
            }

            if (!Number.isFinite(quantity) || quantity < 0.01 || quantity > 1000000) {
                throw new Error('Ingresa una cantidad válida.');
            }

            await api.post('/api/consumptions', {
                stayId,
                serviceId,
                quantity,
                notes: notesInput.value.trim() || null
            });

            if (!form.isConnected) return;

            notesInput.value = '';
            quantityInput.value = '1';

            showMessage('Consumo registrado correctamente.', 'success');
            await load();
        } catch (error) {
            if (form.isConnected) {
                showError(error);
                saveButton.disabled = loadedStayId === null;
            }
        }
    });

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', { class: 'eyebrow', text: 'OPERACIONES / SERVICIOS' }),
            el('h1', { text: 'Consumos del huésped' }),
            el('p', {
                class: 'muted',
                text: 'Registra los servicios utilizados durante una estadía.'
            }),
            form
        ),
        results
    );

    if (initialStayId) load();
}
