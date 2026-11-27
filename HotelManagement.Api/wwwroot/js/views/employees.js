
import { renderCatalogEditor } from './catalog-editor.js';

export function renderEmployees(container) {
    renderCatalogEditor(container, {
        title: 'Equipo Aurora',
        eyebrow: 'ADMINISTRACIÓN / TALENTO HUMANO',
        description:
            'Gestiona a las personas que hacen posible la experiencia Aurora.',
        createTitle: 'Registrar empleado',
        listTitle: 'Directorio de empleados',
        endpoint: '/api/employees',
        idKey: 'employeeId',
        scoped: true,
        includeHotelOnUpdate: true,

        fields: [
            {
                key: 'firstName',
                label: 'Nombres',
                required: true,
                maxLength: 80
            },
            {
                key: 'lastName',
                label: 'Apellidos',
                required: true,
                maxLength: 100
            },
            {
                key: 'documentType',
                label: 'Tipo de documento',
                maxLength: 20,
                placeholder: 'DNI, CE, pasaporte...'
            },
            {
                key: 'documentNumber',
                label: 'Número de documento',
                maxLength: 30
            },
            {
                key: 'position',
                label: 'Cargo',
                maxLength: 80,
                placeholder: 'Ej. Recepcionista'
            },
            {
                key: 'email',
                label: 'Correo electrónico',
                type: 'email',
                maxLength: 120
            },
            {
                key: 'phone',
                label: 'Teléfono',
                type: 'tel',
                maxLength: 30
            },
            {
                key: 'active',
                label: 'Estado laboral',
                type: 'boolean',
                defaultValue: true,
                editOnly: true
            }
        ],

        columns: [
            { key: 'employeeId', label: 'ID' },
            { key: 'firstName', label: 'Nombres' },
            { key: 'lastName', label: 'Apellidos' },
            { key: 'documentNumber', label: 'Documento' },
            { key: 'position', label: 'Cargo' },
            { key: 'email', label: 'Correo' },
            { key: 'phone', label: 'Teléfono' },
            {
                key: 'active',
                label: 'Estado',
                format: value => value ? 'Activo' : 'Inactivo'
            }
        ]
    });
}
