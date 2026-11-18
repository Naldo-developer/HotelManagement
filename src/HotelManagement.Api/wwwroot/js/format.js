// Utilidades de fechas y dinero. Las fechas viajan como texto AAAA-MM-DD.

function toIso(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

export function todayIso() {
    return toIso(new Date());
}

export function addDaysIso(iso, days) {
    const [year, month, day] = iso.split('-').map(Number);
    return toIso(new Date(year, month - 1, day + days));
}

export function nightsBetween(fromIso, toIsoDate) {
    const [y1, m1, d1] = fromIso.split('-').map(Number);
    const [y2, m2, d2] = toIsoDate.split('-').map(Number);
    return Math.round((Date.UTC(y2, m2 - 1, d2) - Date.UTC(y1, m1 - 1, d1)) / 86400000);
}

export function formatDate(iso) {
    if (!iso) {
        return '—';
    }
    const [year, month, day] = iso.split('-');
    return `${day}/${month}/${year}`;
}

export function formatMoney(value) {
    return Number(value).toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}