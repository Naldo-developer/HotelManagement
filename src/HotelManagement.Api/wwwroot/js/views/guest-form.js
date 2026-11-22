import { api } from '../api.js';
import { clearMessages, el, queueMessage, showError, showMessage } from '../ui.js';

function field(id, label, control, hint) {
    return el('div', { class: 'field' },
        el('label', { for: id, text: label }),
        control,
        hint ? el('small', { class: 'muted', text: hint }) : '');
}

const blankToNull = (value) => {
    const trimmed = value.trim();
    return trimmed === '' ? null : trimmed;
};

// Sin guestId: alta de un huésped nuevo. Con guestId: edición de ese huésped.
export async function renderGuestForm(container, guestId = null) {
    const isEdit = guestId !== null;
    const title = isEdit ? 'Editar huésped' : 'Nuevo huésped';
    const backHref = isEdit ? `#huespedes/${guestId}` : '#huespedes';

    if (isEdit && !/^[1-9]\d*$/.test(guestId)) {
        container.append(el('section', { class: 'card' },
            el('h1', { text: title }),
            el('p', { class: 'muted', text: 'El identificador del huésped no es válido.' }),
            el('div', { class: 'actions' },
                el('a', { href: '#huespedes', class: 'btn secondary', text: '← Volver al listado' }))));
        showMessage('El identificador del huésped no es válido.', 'error');
        return;
    }

    const firstName = el('input', { id: 'h-first', name: 'firstName', type: 'text', maxLength: 80, autocomplete: 'off' });
    const lastName = el('input', { id: 'h-last', name: 'lastName', type: 'text', maxLength: 100, autocomplete: 'off' });
    const documentType = el('input', { id: 'h-doctype', name: 'documentType', type: 'text', maxLength: 20, placeholder: 'Ej.: DNI', autocomplete: 'off' });
    const documentNumber = el('input', { id: 'h-docnumber', name: 'documentNumber', type: 'text', maxLength: 30, autocomplete: 'off' });
    const email = el('input', { id: 'h-email', name: 'email', type: 'email', maxLength: 120, autocomplete: 'off' });
    const phone = el('input', { id: 'h-phone', name: 'phone', type: 'tel', maxLength: 30, autocomplete: 'off' });

    const submit = el('button', { type: 'submit', class: 'btn', text: isEdit ? 'Guardar cambios' : 'Guardar huésped' });
    const cancel = el('a', { href: backHref, class: 'btn secondary', text: 'Cancelar' });

    const form = el('form', { class: 'form-grid', novalidate: true },
        field('h-first', 'Nombre *', firstName),
        field('h-last', 'Apellido *', lastName),
        field('h-doctype', 'Tipo de documento', documentType, 'Opcional, pero va junto con el número'),
        field('h-docnumber', 'Número de documento', documentNumber, 'Opcional, pero va junto con el tipo'),
        field('h-email', 'Correo', email, 'Opcional'),
        field('h-phone', 'Teléfono', phone, 'Opcional'),
        el('div', { class: 'form-actions' }, submit, cancel));

    const card = el('section', { class: 'card' },
        el('h1', { text: title }),
        el('p', { class: 'muted', text: 'Los campos con * son obligatorios.' }),
        form);
    container.append(card);

    function validate() {
        if (!firstName.value.trim()) {
            return 'El nombre es obligatorio.';
        }
        if (!lastName.value.trim()) {
            return 'El apellido es obligatorio.';
        }
        const hasType = documentType.value.trim() !== '';
        const hasNumber = documentNumber.value.trim() !== '';
        if (hasType && !hasNumber) {
            return 'Si informas el tipo de documento, también debes informar el número.';
        }
        if (!hasType && hasNumber) {
            return 'Si informas el número de documento, también debes informar el tipo.';
        }
        const mail = email.value.trim();
        if (mail && !/^\S+@\S+\.\S+$/.test(mail)) {
            return 'El correo no tiene un formato válido.';
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
            firstName: firstName.value.trim(),
            lastName: lastName.value.trim(),
            documentType: blankToNull(documentType.value),
            documentNumber: blankToNull(documentNumber.value),
            email: blankToNull(email.value),
            phone: blankToNull(phone.value),
        };

        saving = true;
        submit.disabled = true;
        const label = submit.textContent;
        submit.textContent = 'Guardando…';
        try {
            const guest = isEdit
                ? await api.put(`/api/guests/${guestId}`, body)
                : await api.post('/api/guests', body);
            queueMessage(isEdit ? 'Huésped actualizado correctamente.' : 'Huésped registrado correctamente.', 'success');
            window.location.hash = `#huespedes/${guest.id}`;
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

    if (isEdit) {
        submit.disabled = true;
        try {
            const guest = await api.get(`/api/guests/${guestId}`);
            if (!card.isConnected) {
                return;
            }
            firstName.value = guest.firstName ?? '';
            lastName.value = guest.lastName ?? '';
            documentType.value = guest.documentType ?? '';
            documentNumber.value = guest.documentNumber ?? '';
            email.value = guest.email ?? '';
            phone.value = guest.phone ?? '';
            submit.disabled = false;
        } catch (error) {
            if (card.isConnected) {
                card.replaceChildren(
                    el('h1', { text: title }),
                    el('p', { class: 'muted', text: 'No se pudo cargar el huésped.' }),
                    el('div', { class: 'actions' },
                        el('a', { href: '#huespedes', class: 'btn secondary', text: '← Volver al listado' })));
                showError(error); // un 404 muestra el mensaje de la API
            }
        }
    }
}