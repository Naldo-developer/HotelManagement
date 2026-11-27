
import { renderCatalogEditor } from './catalog-editor.js';

export function renderHotels(container) {
    renderCatalogEditor(container, {
        title: 'Gestión de hoteles',
        eyebrow: 'ADMINISTRACIÓN / ESTABLECIMIENTOS',
        description:
            'Administra la información general de los establecimientos.',
        createTitle: 'Registrar hotel',
        listTitle: 'Hoteles registrados',
        endpoint: '/api/hotels',
        idKey: 'hotelId',
        scoped: false,

        fields: [
            {
                key: 'name',
                label: 'Nombre del hotel',
                required: true,
                maxLength: 120
            },
            {
                key: 'taxId',
                label: 'RUC / Identificación fiscal',
                maxLength: 30
            },
            {
                key: 'address',
                label: 'Dirección',
                maxLength: 200
            },
            {
                key: 'phone',
                label: 'Teléfono',
                type: 'tel',
                maxLength: 30
            },
            {
                key: 'email',
                label: 'Correo electrónico',
                type: 'email',
                maxLength: 120
            },
            {
                key: 'currencyCode',
                label: 'Código de moneda',
                required: true,
                pattern: '[A-Z]{3}',
                maxLength: 3,
                defaultValue: 'PEN'
            },
            {
                key: 'timezone',
                label: 'Zona horaria',
                required: true,
                maxLength: 50,
                defaultValue: 'America/Lima'
            }
        ],

        columns: [
            { key: 'hotelId', label: 'ID' },
            { key: 'name', label: 'Hotel' },
            { key: 'taxId', label: 'RUC' },
            { key: 'address', label: 'Dirección' },
            { key: 'phone', label: 'Teléfono' },
            { key: 'currencyCode', label: 'Moneda' },
            {
                key: 'active',
                label: 'Estado',
                format: value => value ? 'Activo' : 'Inactivo'
            }
        ]
    });
}
