
import { renderCatalogEditor } from './catalog-editor.js';

export function renderRoomTypes(container) {
    renderCatalogEditor(container, {
        title: 'Tipos de habitación',
        eyebrow: 'ADMINISTRACIÓN / ALOJAMIENTO',
        description:
            'Configura las categorías, capacidades y tarifas de alojamiento.',
        createTitle: 'Nuevo tipo de habitación',
        listTitle: 'Categorías disponibles',
        endpoint: '/api/room-types',
        idKey: 'roomTypeId',
        scoped: true,

        fields: [
            {
                key: 'name',
                label: 'Nombre de la categoría',
                required: true,
                maxLength: 80,
                placeholder: 'Ej. Suite ejecutiva'
            },
            {
                key: 'description',
                label: 'Descripción',
                type: 'textarea'
            },
            {
                key: 'maxAdults',
                label: 'Capacidad de adultos',
                type: 'number',
                required: true,
                min: 1,
                max: 32767,
                step: 1,
                defaultValue: 1
            },
            {
                key: 'maxChildren',
                label: 'Capacidad de niños',
                type: 'number',
                required: true,
                min: 0,
                max: 32767,
                step: 1,
                defaultValue: 0
            },
            {
                key: 'baseRate',
                label: 'Tarifa base por noche',
                type: 'number',
                required: true,
                min: 0,
                max: 9999999999.99,
                step: '0.01',
                defaultValue: 0
            }
        ],

        columns: [
            { key: 'roomTypeId', label: 'ID' },
            { key: 'name', label: 'Categoría' },
            { key: 'maxAdults', label: 'Adultos' },
            { key: 'maxChildren', label: 'Niños' },
            {
                key: 'baseRate',
                label: 'Tarifa base',
                format: value => Number(value).toFixed(2)
            },
            {
                key: 'active',
                label: 'Estado',
                format: value => value ? 'Activo' : 'Inactivo'
            }
        ]
    });
}
