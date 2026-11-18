// Utilidades de interfaz: mensajes y barra de carga.

const messagesContainer = () => document.getElementById('messages');

export function clearMessages() {
    messagesContainer().replaceChildren();
}

export function showMessage(text, type = 'info') {
    const box = document.createElement('div');
    box.className = `message ${type}`;
    box.setAttribute('role', type === 'error' ? 'alert' : 'status');

    const content = document.createElement('span');
    content.textContent = text; // textContent evita inyectar HTML

    const close = document.createElement('button');
    close.type = 'button';
    close.setAttribute('aria-label', 'Cerrar mensaje');
    close.textContent = '×';
    close.addEventListener('click', () => box.remove());

    box.append(content, close);
    messagesContainer().append(box);

    if (type === 'success') {
        setTimeout(() => box.remove(), 5000);
    }
}

export function showError(error) {
    const message = error instanceof Error ? error.message : String(error);
    showMessage(message, 'error');
}

export function bindLoadingIndicator(onLoadingChange) {
    const bar = document.getElementById('loading');
    onLoadingChange((isLoading) => {
        bar.hidden = !isLoading;
    });
}

// ---- Y03: mensajes en cola, creación de elementos y listas desplegables ----

const queuedMessages = [];

// Un mensaje en cola se muestra después del siguiente cambio de pantalla
// (por ejemplo "Habitación creada" al abrir el detalle).
export function queueMessage(text, type = 'info') {
    queuedMessages.push({ text, type });
}

export function flushQueuedMessages() {
    queuedMessages.splice(0).forEach(({ text, type }) => showMessage(text, type));
}

export function el(tag, props = {}, ...children) {
    const node = document.createElement(tag);
    Object.entries(props).forEach(([key, value]) => {
        if (key === 'class') {
            node.className = value;
        } else if (key === 'text') {
            node.textContent = value;
        } else if (key in node) {
            node[key] = value;
        } else {
            node.setAttribute(key, value);
        }
    });
    node.append(...children);
    return node;
}

export function setSelectOptions(select, placeholder, items, valueKey, selected = '') {
    select.replaceChildren(
        el('option', { value: '', text: placeholder }),
        ...items.map((item) => el('option', { value: String(item[valueKey]), text: item.name })),
    );
    const wanted = String(selected ?? '');
    select.value = items.some((item) => String(item[valueKey]) === wanted) ? wanted : '';
}