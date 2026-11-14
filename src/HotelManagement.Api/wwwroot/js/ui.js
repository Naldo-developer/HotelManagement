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