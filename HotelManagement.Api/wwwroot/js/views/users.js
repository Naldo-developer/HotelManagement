
import { api } from '../api.js';
import { el, showError, showMessage } from '../ui.js';
import { ROLE_LABELS } from '../permissions.js';
import { getUser } from '../session.js';

const ROLE_DETAILS = {
    ADMIN: 'Control administrativo del sistema.',
    RECEPTION: 'Huéspedes, reservas, estadías y consumos.',
    CASHIER: 'Cobros, pagos y consultas financieras.',
    MAINTENANCE: 'Gestión y cierre de mantenimientos.'
};

export function renderUsers(container) {
    let users = [];
    let employees = [];
    let editing = null;
    let busy = false;
    let loading = true;

    const results = el('div', { class: 'results' });

    const searchInput = el('input', {
        type: 'search',
        placeholder: 'Buscar usuario, empleado o rol...'
    });

    const employeeSelect = el('select', {
        required: true
    });

    const usernameInput = el('input', {
        type: 'text',
        required: true,
        minLength: 3,
        maxLength: 60,
        autocomplete: 'off',
        placeholder: 'nombre.usuario'
    });

    const passwordInput = el('input', {
        type: 'password',
        required: true,
        minLength: 6,
        maxLength: 100,
        autocomplete: 'new-password'
    });

    const activeSelect = el('select', {},
        el('option', { value: 'true', text: 'Activo' }),
        el('option', { value: 'false', text: 'Inactivo' })
    );

    const roleControls = new Map();

    const rolesGrid = el('div', { class: 'aurora-role-grid' },
        ...Object.entries(ROLE_LABELS).map(([code, name]) => {
            const checkbox = el('input', {
                type: 'checkbox',
                value: code
            });

            roleControls.set(code, checkbox);

            return el('label', { class: 'aurora-role-option' },
                checkbox,
                el('div', {},
                    el('strong', { text: name }),
                    el('small', {
                        class: 'muted',
                        text: ROLE_DETAILS[code]
                    })
                )
            );
        })
    );

    function field(label, control) {
        return el('label', { class: 'field' },
            el('span', { text: label }),
            control
        );
    }

    const employeeField = field('Empleado vinculado', employeeSelect);
    const activeField = field('Estado de la cuenta', activeSelect);

    const passwordHint = el('p', {
        class: 'muted',
        text: 'Mínimo 6 caracteres. Utiliza una contraseña segura.'
    });

    const formTitle = el('h2', {
        text: 'Crear cuenta de acceso'
    });

    const saveButton = el('button', {
        type: 'submit',
        class: 'btn',
        text: 'Crear usuario',
        disabled: true
    });

    const cancelButton = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Cancelar edición'
    });

    function populateEmployees() {
        const assigned = new Set(
            users.map(user => user.employeeId)
        );

        const available = employees.filter(employee =>
            employee.active && !assigned.has(employee.employeeId)
        );

        employeeSelect.replaceChildren(
            el('option', {
                value: '',
                text: 'Selecciona un empleado sin cuenta'
            }),
            ...available.map(employee => el('option', {
                value: String(employee.employeeId),
                text: `${employee.firstName} ${employee.lastName}`
            }))
        );
    }

    function resetForm() {
        editing = null;

        form.reset();
        populateEmployees();

        employeeField.hidden = false;
        employeeSelect.disabled = false;
        passwordInput.required = true;

        activeField.hidden = true;
        activeSelect.disabled = true;
        activeSelect.value = 'true';

        roleControls.forEach(control => {
            control.checked = false;
        });

        formTitle.textContent = 'Crear cuenta de acceso';

        passwordHint.textContent =
            'Mínimo 6 caracteres. Utiliza una contraseña segura.';

        cancelButton.hidden = true;
        saveButton.textContent = 'Crear usuario';
        saveButton.disabled = busy || loading;
    }

    function startEditing(user) {
        if (busy || loading) return;

        editing = user;

        employeeField.hidden = true;
        employeeSelect.disabled = true;

        usernameInput.value = user.username;

        passwordInput.value = '';
        passwordInput.required = false;

        passwordHint.textContent =
            'Déjala vacía para conservar la contraseña actual.';

        activeField.hidden = false;
        activeSelect.disabled = false;
        activeSelect.value = String(user.active);

        roleControls.forEach((control, code) => {
            control.checked = user.roles.includes(code);
        });

        formTitle.textContent = `Editar cuenta · ${user.username}`;

        saveButton.textContent = 'Guardar cambios';
        cancelButton.hidden = false;

        formCard.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }

    function renderList() {
        const term = searchInput.value.trim().toLowerCase();

        const filtered = users.filter(user =>
            [
                user.username,
                user.employeeName,
                ...user.roles
            ].join(' ').toLowerCase().includes(term)
        );

        const table = el('table', {},
            el('thead', {},
                el('tr', {},
                    ...[
                        'Usuario',
                        'Empleado',
                        'Roles',
                        'Estado',
                        'Acción'
                    ].map(text => el('th', { text }))
                )
            ),
            el('tbody', {},
                ...filtered.map(user => {
                    const editButton = el('button', {
                        type: 'button',
                        class: 'btn secondary',
                        text: 'Editar'
                    });

                    editButton.addEventListener(
                        'click',
                        () => startEditing(user)
                    );

                    return el('tr', {},
                        el('td', { text: user.username }),
                        el('td', { text: user.employeeName }),
                        el('td', {},
                            el('div', { class: 'aurora-badge-list' },
                                ...user.roles.map(code =>
                                    el('span', {
                                        class: 'aurora-badge',
                                        text: ROLE_LABELS[code] || code
                                    })
                                )
                            )
                        ),
                        el('td', {},
                            el('span', {
                                class: user.active
                                    ? 'aurora-badge success'
                                    : 'aurora-badge',
                                text: user.active ? 'Activo' : 'Inactivo'
                            })
                        ),
                        el('td', {}, editButton)
                    );
                })
            )
        );

        results.replaceChildren(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', { text: 'Cuentas del sistema' }),
                    el('span', {
                        class: 'aurora-badge',
                        text: `${filtered.length} usuarios`
                    })
                ),
                filtered.length
                    ? el('div', { class: 'table-wrap' }, table)
                    : el('p', {
                        class: 'muted',
                        text: 'No se encontraron cuentas.'
                    })
            )
        );
    }

    async function load() {
        loading = true;
        saveButton.disabled = true;

        try {
            const [userData, employeeData] = await Promise.all([
                api.get('/api/users'),
                api.get('/api/employees')
            ]);

            if (!results.isConnected) return;

            users = userData;
            employees = employeeData;

            populateEmployees();
            renderList();
        } catch (error) {
            if (results.isConnected) showError(error);
        } finally {
            loading = false;
            saveButton.disabled = busy;
        }
    }

    const form = el('form', { class: 'form-grid' },
        employeeField,
        field('Nombre de usuario', usernameInput),
        field('Contraseña', passwordInput),
        passwordHint,
        activeField,
        el('div', { class: 'aurora-full-width' },
            el('h3', { text: 'Roles y permisos' }),
            el('p', {
                class: 'muted',
                text: 'Selecciona uno o más roles para esta cuenta.'
            }),
            rolesGrid
        ),
        el('div', { class: 'form-actions' },
            saveButton,
            cancelButton
        )
    );

    form.addEventListener('submit', async event => {
        event.preventDefault();

        if (busy || loading) return;

        const username = usernameInput.value.trim();
        const password = passwordInput.value;

        const roleCodes = [...roleControls.entries()]
            .filter(([, control]) => control.checked)
            .map(([code]) => code);

        const active = activeSelect.value === 'true';

        if (username.length < 3) {
            showMessage(
                'El usuario debe tener al menos 3 caracteres.',
                'error'
            );
            return;
        }

        if (!roleCodes.length) {
            showMessage('Selecciona al menos un rol.', 'error');
            return;
        }

        if ((!editing || password) && password.length < 6) {
            showMessage(
                'La contraseña debe tener al menos 6 caracteres.',
                'error'
            );
            return;
        }

        // Evita desactivar accidentalmente la cuenta propia.
        if (
            editing &&
            editing.userId === getUser()?.userId &&
            (!active || !roleCodes.includes('ADMIN'))
        ) {
            showMessage(
                'No puedes quitar tu propio acceso administrativo desde esta pantalla.',
                'error'
            );
            return;
        }

        busy = true;
        saveButton.disabled = true;
        cancelButton.disabled = true;

        try {
            if (editing) {
                await api.put(`/api/users/${editing.userId}`, {
                    username,
                    password: password || null,
                    roleCodes,
                    active
                });
            } else {
                const employeeId = Number(employeeSelect.value);

                if (!Number.isSafeInteger(employeeId) || employeeId <= 0) {
                    throw new Error('Selecciona un empleado válido.');
                }

                await api.post('/api/users', {
                    employeeId,
                    username,
                    password,
                    roleCodes
                });
            }

            if (!form.isConnected) return;

            showMessage(
                editing
                    ? 'Cuenta actualizada. Los cambios de acceso se reflejarán al iniciar una nueva sesión.'
                    : 'Cuenta creada correctamente.',
                'success'
            );

            resetForm();
            await load();
        } catch (error) {
            if (form.isConnected) showError(error);
        } finally {
            passwordInput.value = '';
            busy = false;
            saveButton.disabled = loading;
            cancelButton.disabled = false;
        }
    });

    cancelButton.addEventListener('click', () => {
        if (!busy) resetForm();
    });

    searchInput.addEventListener('input', renderList);

    const formCard = el('section', {
        class: 'card aurora-module'
    }, formTitle, form);

    container.append(
        el('section', { class: 'card aurora-module' },
            el('span', {
                class: 'eyebrow',
                text: 'ADMINISTRACIÓN / SEGURIDAD'
            }),
            el('h1', { text: 'Usuarios y accesos' }),
            el('p', {
                class: 'muted',
                text: 'Administra las cuentas y responsabilidades del equipo Aurora.'
            }),
            field('Buscar cuentas', searchInput)
        ),
        formCard,
        results
    );

    resetForm();
    load();
}
