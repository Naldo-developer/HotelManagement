
import { renderCatalogEditor } from './catalog-editor.js';

export function renderServices(container) {
    renderCatalogEditor(container, {
        title: 'Servicios del hotel',
        eyebrow: 'EXPERIENCIA / SERVICIOS',
        description:
            'Consulta y administra los servicios disponibles para los huéspedes.',
        createTitle: 'Registrar servicio',
        listTitle: 'Catálogo de servicios',
        endpoint: '/api/services',
        idKey: 'serviceId',
        scoped: true,

        fields: [
            {
                key: 'name',
                label: 'Nombre del servicio',
                required: true,
                maxLength: 100,
                placeholder: 'Ej. Desayuno continental'
            },
            {
                key: 'description',
                label: 'Descripción',
                type: 'textarea',
                maxLength: 1000
            },
            {
                key: 'defaultPrice',
                label: 'Precio unitario',
                type: 'number',
                required: true,
                min: 0,
                max: 9999999999,
                step: '0.01',
                defaultValue: 0
            },
            {
                key: 'unit',
                label: 'Unidad de medida',
                required: true,
                maxLength: 30,
                placeholder: 'Ej. Unidad'
            },
            {
                key: 'active',
                label: 'Estado del servicio',
                type: 'boolean',
                defaultValue: true,
                editOnly: true
            }
        ],

        columns: [
            { key: 'serviceId', label: 'ID' },
            { key: 'name', label: 'Servicio' },
            { key: 'description', label: 'Descripción' },
            { key: 'unit', label: 'Unidad' },
            {
                key: 'defaultPrice',
                label: 'Precio',
                format: value => Number(value).toFixed(2)
            },
            {
                key: 'active',
                label: 'Estado',
                format: value => value ? 'Disponible' : 'Inactivo'
            }
        ]
    });
}
