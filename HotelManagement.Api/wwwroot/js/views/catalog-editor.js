
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';
import { canManageConfiguration } from '../permissions.js';
import { clearCatalogCache } from '../catalogs.js';

export function renderCatalogEditor(container, config) {
    const editable = canManageConfiguration();

    let records = [];
    let editing = null;
    let busy = false;
    let loading = false;
    let version = 0;

    const controls = new Map();

    const search = el('input', {
        type: 'search',
        placeholder: `Buscar ${config.title.toLowerCase()}...`
    });

    const hotelSelect = el('select', {});
    const results = el('div', { class: 'results' });

    const formTitle = el('h2', {
        text: config.createTitle
    });

    const saveButton = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Guardar registro'
    });

    const cancelButton = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Cancelar edición'
    });

    cancelButton.hidden = true;

    function field(label, control) {
        return el('label', { class: 'field' },
            el('span', { text: label }),
            control
        );
    }

    function makeControl(definition) {
        const properties = {
            name: definition.key
        };

        if (definition.required) properties.required = true;
        if (definition.maxLength) properties.maxLength = definition.maxLength;
        if (definition.min !== undefined) properties.min = definition.min;
        if (definition.max !== undefined) properties.max = definition.max;
        if (definition.step !== undefined) properties.step = definition.step;
        if (definition.pattern) properties.pattern = definition.pattern;
        if (definition.placeholder) {
            properties.placeholder = definition.placeholder;
        }

        let control;

        if (definition.type === 'textarea') {
            control = el('textarea', {
                ...properties,
                rows: 3
            });
        } else if (definition.type === 'boolean') {
            control = el('select', properties,
                el('option', { value: 'true', text: 'Activo' }),
                el('option', { value: 'false', text: 'Inactivo' })
            );
        } else {
            control = el('input', {
                ...properties,
                type: definition.type || 'text'
            });
        }

        controls.set(definition.key, control);

        return field(
            definition.label + (definition.required ? ' *' : ''),
            control
        );
    }

    const form = el('form', { class: 'form-grid' },
        ...config.fields.map(makeControl),
        el('div', { class: 'form-actions' },
            saveButton,
            cancelButton
        )
    );

    const formCard = el('section', {
        class: 'card aurora-module'
    }, formTitle, form);

    function updateButtons() {
        saveButton.disabled =
            busy ||
            loading ||
            (config.scoped && !hotelSelect.value);

        hotelSelect.disabled = busy || loading;
        cancelButton.disabled = busy;
    }

    function resetForm() {
        editing = null;

        config.fields.forEach(definition => {
            const control = controls.get(definition.key);

            control.value = String(
                definition.defaultValue ?? ''
            );

            control.closest('.field').hidden =
                Boolean(definition.editOnly);

            control.disabled =
                Boolean(definition.editOnly);
        });

        formTitle.textContent = config.createTitle;
        saveButton.textContent = 'Guardar registro';
        cancelButton.hidden = true;

        updateButtons();
    }

    function startEditing(record) {
        if (busy || loading) return;

        editing = record;

        config.fields.forEach(definition => {
            const control = controls.get(definition.key);

            control.value = String(
                record[definition.key] ??
                definition.defaultValue ??
                ''
            );

            control.closest('.field').hidden = false;
            control.disabled = false;
        });

        formTitle.textContent =
            `Editar · ${record.name || record[config.idKey]}`;

        saveButton.textContent = 'Guardar cambios';
        cancelButton.hidden = false;

        formCard.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }

    function readValues() {
        const body = {};

        for (const definition of config.fields) {
            if (definition.editOnly && !editing) continue;

            const raw = controls
                .get(definition.key)
                .value.trim();

            if (definition.required && raw === '') {
                throw new Error(
                    `Completa el campo ${definition.label}.`
                );
            }

            if (definition.type === 'number') {
                const value = Number(raw);

                if (
                    raw === '' ||
                    !Number.isFinite(value) ||
                    (definition.min !== undefined &&
                        value < definition.min) ||
                    (definition.max !== undefined &&
                        value > definition.max)
                ) {
                    throw new Error(
                        `El valor de ${definition.label} no es válido.`
                    );
                }

                body[definition.key] = value;
            } else if (definition.type === 'boolean') {
                body[definition.key] = raw === 'true';
            } else {
                body[definition.key] = raw || null;
            }
        }

        if (config.scoped && (!editing || config.includeHotelOnUpdate)) {
            const hotelId = Number(hotelSelect.value);

            if (!Number.isSafeInteger(hotelId) || hotelId <= 0) {
                throw new Error('Selecciona un hotel válido.');
            }

            body.hotelId = hotelId;
        }

        return body;
    }

    function renderTable() {
        const term = search.value.trim().toLowerCase();

        const filtered = records.filter(record =>
            config.columns.some(column =>
                String(record[column.key] ?? '')
                    .toLowerCase()
                    .includes(term)
            )
        );

        const headers = [
            ...config.columns.map(column => column.label),
            ...(editable ? ['Acción'] : [])
        ];

        const rows = filtered.map(record => {
            const cells = config.columns.map(column => {
                const value = record[column.key];

                return el('td', {
                    text: column.format
                        ? column.format(value)
                        : String(value ?? '—')
                });
            });

            if (editable) {
                const editButton = el('button', {
                    type: 'button',
                    class: 'btn secondary',
                    text: 'Editar'
                });

                editButton.addEventListener(
                    'click',
                    () => startEditing(record)
                );

                cells.push(el('td', {}, editButton));
            }

            return el('tr', {}, ...cells);
        });

        const table = el('table', {},
            el('thead', {},
                el('tr', {},
                    ...headers.map(text => el('th', { text }))
                )
            ),
            el('tbody', {}, ...rows)
        );

        results.replaceChildren(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', {
                        text: config.listTitle || 'Registros disponibles'
                    }),
                    el('span', {
                        class: 'aurora-badge',
                        text: `${filtered.length} registros`
                    })
                ),
                filtered.length
                    ? el('div', { class: 'table-wrap' }, table)
                    : el('p', {
                        class: 'muted',
                        text: 'No hay registros para mostrar.'
                    })
            )
        );
    }

    async function load() {
        const request = ++version;

        records = [];
        loading = true;
        updateButtons();

        results.replaceChildren(
            el('p', {
                class: 'muted',
                text: 'Cargando información...'
            })
        );

        try {
            if (config.scoped && !hotelSelect.value) {
                results.replaceChildren(
                    el('p', {
                        class: 'muted',
                        text: 'Selecciona un hotel.'
                    })
                );

                return;
            }

            const query = config.scoped
                ? { hotelId: Number(hotelSelect.value) }
                : undefined;

            const data = await api.get(config.endpoint, query);

            if (request !== version || !results.isConnected) {
                return;
            }

            records = data;
            renderTable();
        } catch (error) {
            if (results.isConnected && request === version) {
                results.replaceChildren();
                showError(error);
            }
        } finally {
            if (request === version) {
                loading = false;
                updateButtons();
            }
        }
    }

    async function loadHotels() {
        const hotels = await api.get('/api/hotels');

        if (!hotelSelect.isConnected) return;

        hotelSelect.replaceChildren(
            el('option', {
                value: '',
                text: 'Selecciona un hotel'
            }),
            ...hotels.map(hotel =>
                el('option', {
                    value: String(hotel.hotelId),
                    text: hotel.name
                })
            )
        );

        if (hotels.length === 1) {
            hotelSelect.value = String(hotels[0].hotelId);
        }

        updateButtons();
        await load();
    }

    form.addEventListener('submit', async event => {
        event.preventDefault();

        if (!editable || busy || loading || saveButton.disabled) {
            return;
        }

        busy = true;
        updateButtons();

        try {
            const body = readValues();

            if (editing) {
                await api.put(
                    `${config.endpoint}/${editing[config.idKey]}`,
                    body
                );
            } else {
                await api.post(config.endpoint, body);
            }

            clearCatalogCache();

            if (!form.isConnected) return;

            showMessage(
                editing
                    ? 'Registro actualizado correctamente.'
                    : 'Registro creado correctamente.',
                'success'
            );

            resetForm();
            await load();
        } catch (error) {
            if (form.isConnected) showError(error);
        } finally {
            busy = false;
            updateButtons();
        }
    });

    cancelButton.addEventListener('click', resetForm);

    search.addEventListener('input', () => {
        if (!loading) renderTable();
    });

    hotelSelect.addEventListener('change', () => {
        resetForm();
        load();
    });

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', {
                class: 'eyebrow',
                text: config.eyebrow || 'ADMINISTRACIÓN / CATÁLOGOS'
            }),
            el('h1', { text: config.title }),
            el('p', {
                class: 'muted',
                text: config.description
            }),
            el('div', { class: 'form-grid' },
                ...(config.scoped
                    ? [field('Hotel', hotelSelect)]
                    : []),
                field('Buscar registros', search)
            )
        ),
        ...(editable ? [formCard] : []),
        results
    );

    resetForm();

    if (config.scoped) {
        loadHotels().catch(error => {
            if (container.isConnected) showError(error);
        });
    } else {
        load();
    }
}
