
import { api } from '../api.js';
import { el, showError } from '../ui.js';
import { getUser } from '../session.js';

import {
    canManageConfiguration,
    canManageOperations,
    canManagePayments,
    canManageMaintenance,
    getRoleLabels
} from '../permissions.js';

async function getAllPages(path, query = {}) {
    const records = [];

    for (let page = 1; ; page++) {
        const batch = await api.get(path, {
            ...query,
            page,
            pageSize: 100
        });

        records.push(...batch);

        if (batch.length < 100) {
            return records;
        }
    }
}

const STATUS_LABELS = {
    PENDING: 'Pendiente',
    CONFIRMED: 'Confirmada',
    CHECKED_IN: 'En estadía',
    COMPLETED: 'Completada',
    CANCELLED: 'Cancelada',
    NO_SHOW: 'No se presentó',
    SCHEDULED: 'Programado',
    IN_PROGRESS: 'En progreso'
};

function greeting() {
    const hour = new Date().getHours();

    if (hour < 12) return 'Buenos días';
    if (hour < 19) return 'Buenas tardes';

    return 'Buenas noches';
}

function metric(label, value, description, href) {
    return el('a', {
            class: 'aurora-dashboard-metric',
            href
        },
        el('span', {
            class: 'aurora-dashboard-metric-label',
            text: label
        }),
        el('strong', { text: String(value) }),
        el('small', { text: description })
    );
}

function shortcut(title, description, href, symbol) {
    return el('a', {
            class: 'aurora-shortcut',
            href
        },
        el('span', {
            class: 'aurora-shortcut-icon',
            text: symbol,
            'aria-hidden': 'true'
        }),
        el('div', {},
            el('h3', { text: title }),
            el('p', {
                class: 'muted',
                text: description
            })
        ),
        el('span', {
            class: 'aurora-shortcut-arrow',
            text: '↗',
            'aria-hidden': 'true'
        })
    );
}

export async function renderDashboard(container) {
    const user = getUser();
    const name = (user?.fullName || user?.username || '')
        .split(' ')[0];

    const admin = canManageConfiguration();
    const operations = canManageOperations();
    const financial = canManagePayments();
    const maintenance = canManageMaintenance();

    let subtitle = 'Tu espacio de trabajo en Aurora.';
    let primaryAction = null;

    if (admin) {
        subtitle =
            'Supervisa las operaciones y cada detalle de la experiencia Aurora.';

        primaryAction = {
            href: '#hoteles',
            text: 'Administrar hotel'
        };
    } else if (operations) {
        subtitle =
            'Cada llegada merece nuestra mejor versión. Prepara el día desde aquí.';

        primaryAction = {
            href: '#reservas/nueva',
            text: 'Crear una reserva'
        };
    } else if (financial) {
        subtitle =
            'Gestiona los cobros y saldos con claridad y precisión.';

        primaryAction = {
            href: '#pagos',
            text: 'Consultar pagos'
        };
    } else if (maintenance) {
        subtitle =
            'Cuida los espacios que hacen especial cada estadía.';

        primaryAction = {
            href: '#mantenimiento',
            text: 'Ver mantenimientos'
        };
    }

    const dashboard = el('div', {
        class: 'aurora-dashboard'
    });

    const hero = el('section', { class: 'hero' },
        el('div', { class: 'hero-copy' },
            el('span', {
                class: 'eyebrow',
                text: getRoleLabels(user).join(' / ').toUpperCase()
            }),
            el('h1', {
                text: `${greeting()}, ${name}.`
            }),
            el('p', { text: subtitle }),
            ...(primaryAction ? [
                el('a', {
                    href: primaryAction.href,
                    class: 'btn hero-cta',
                    text: `${primaryAction.text} ↗`
                })
            ] : [])
        ),
        el('figure', { class: 'brand-visual' },
            el('img', {
                src: 'images/aurora-lobby.png',
                alt: 'Interior del Hotel Aurora'
            }),
            el('figcaption', {},
                el('span', { text: 'AURORA' }),
                el('span', { text: 'EL ARTE DE RECIBIR' })
            )
        )
    );

    const metrics = el('div', {
        class: 'aurora-dashboard-metrics',
        'aria-live': 'polite'
    }, el('p', {
        class: 'muted',
        text: 'Consultando indicadores...'
    }));

    const shortcuts = [
        shortcut(
            'Habitaciones',
            'Consulta las instalaciones y sus estados.',
            '#habitaciones',
            '01'
        )
    ];

    if (operations) {
        shortcuts.push(
            shortcut(
                'Huéspedes',
                'Gestiona el directorio de visitantes.',
                '#huespedes',
                '02'
            ),
            shortcut(
                'Reservas',
                'Consulta disponibilidad y solicitudes.',
                '#reservas',
                '03'
            ),
            shortcut(
                'Estadías',
                'Registra entradas y salidas.',
                '#estadias',
                '04'
            ),
            shortcut(
                'Consumos',
                'Registra los servicios utilizados.',
                '#consumos',
                '05'
            ),
            shortcut(
                'Servicios',
                'Consulta el catálogo del hotel.',
                '#servicios',
                '06'
            )
        );
    }

    if (financial) {
        shortcuts.push(
            shortcut(
                'Pagos y saldos',
                'Consulta el estado financiero de una reserva.',
                '#pagos',
                '07'
            )
        );
    }

    if (maintenance) {
        shortcuts.push(
            shortcut(
                'Mantenimiento',
                'Planifica y cierra trabajos.',
                '#mantenimiento',
                '08'
            )
        );
    }

    if (admin) {
        shortcuts.push(
            shortcut(
                'Hoteles',
                'Administra los establecimientos.',
                '#hoteles',
                '09'
            ),
            shortcut(
                'Tipos de habitación',
                'Configura capacidades y tarifas.',
                '#tipos-habitacion',
                '10'
            ),
            shortcut(
                'Equipo Aurora',
                'Gestiona los empleados.',
                '#empleados',
                '11'
            ),
            shortcut(
                'Usuarios y accesos',
                'Administra cuentas y permisos.',
                '#usuarios',
                '12'
            )
        );
    }

    const activity = el('div', {
        class: 'aurora-dashboard-activity'
    });

    const healthButton = el('button', {
        type: 'button',
        class: 'btn secondary',
        text: 'Comprobar conexión'
    });

    healthButton.addEventListener('click', async () => {
        healthButton.disabled = true;

        try {
            await api.get('/api/health');

            if (healthButton.isConnected) {
                healthButton.textContent = 'Servidor conectado';
            }
        } catch (error) {
            if (healthButton.isConnected) showError(error);
        } finally {
            healthButton.disabled = false;
        }
    });

    dashboard.append(
        hero,
        el('div', { class: 'section-heading' },
            el('div', {},
                el('span', {
                    class: 'eyebrow',
                    text: 'INFORMACIÓN EN TIEMPO DE CONSULTA'
                }),
                el('h2', { text: 'Panorama del hotel' })
            )
        ),
        metrics,
        el('div', { class: 'section-heading' },
            el('div', {},
                el('span', {
                    class: 'eyebrow',
                    text: 'TU ESPACIO DE TRABAJO'
                }),
                el('h2', { text: 'Accesos rápidos' })
            )
        ),
        el('div', {
            class: 'aurora-shortcuts'
        }, ...shortcuts),
        activity,
        el('section', { class: 'card status-card' },
            el('div', { class: 'status-copy' },
                el('div', {},
                    el('h2', { text: 'Conexión con el hotel' }),
                    el('p', {
                        class: 'muted',
                        text: 'Comprueba la respuesta del servidor.'
                    })
                )
            ),
            healthButton
        )
    );

    container.append(dashboard);

    // Solo consulta los endpoints permitidos para el rol actual.
    const requests = {
        rooms: getAllPages('/api/rooms')
    };

    if (operations) {
        requests.reservations =
            getAllPages('/api/reservations');

        requests.guests =
            getAllPages('/api/guests');
    }

    if (maintenance) {
        requests.maintenance =
            api.get('/api/maintenance');
    }

    if (admin) {
        requests.employees =
            api.get('/api/employees');

        requests.users =
            api.get('/api/users');

        requests.hotels =
            api.get('/api/hotels');
    }

    const keys = Object.keys(requests);

    const settled = await Promise.allSettled(
        Object.values(requests)
    );

    if (!dashboard.isConnected) return;

    const data = {};

    settled.forEach((result, index) => {
        if (result.status === 'fulfilled') {
            data[keys[index]] = result.value;
        }
    });

    const cards = [];

    if (data.rooms) {
        cards.push(
            metric(
                'Habitaciones',
                data.rooms.length,
                'Total registrado',
                '#habitaciones'
            ),
            metric(
                'Operativas',
                data.rooms.filter(
                    room => room.operationalStatus === 'ACTIVE'
                ).length,
                'Estado operativo activo',
                '#habitaciones'
            )
        );
    }

    if (data.reservations) {
        cards.push(
            metric(
                'Reservas pendientes',
                data.reservations.filter(
                    item => item.status === 'PENDING'
                ).length,
                'Pendientes de atención',
                '#reservas'
            ),
            metric(
                'Estadías abiertas',
                data.reservations.filter(
                    item => item.status === 'CHECKED_IN'
                ).length,
                'Reservas con check-in',
                '#estadias'
            )
        );
    }

    if (data.guests) {
        cards.push(
            metric(
                'Huéspedes',
                data.guests.length,
                'Personas registradas',
                '#huespedes'
            )
        );
    }

    if (data.maintenance) {
        cards.push(
            metric(
                'Mantenimientos',
                data.maintenance.filter(item =>
                    ['SCHEDULED', 'IN_PROGRESS'].includes(item.status)
                ).length,
                'Trabajos abiertos',
                '#mantenimiento'
            )
        );
    }

    if (data.hotels) {
        cards.push(
            metric(
                'Hoteles',
                data.hotels.length,
                'Establecimientos registrados',
                '#hoteles'
            )
        );
    }

    if (data.employees) {
        cards.push(
            metric(
                'Equipo activo',
                data.employees.filter(item => item.active).length,
                'Empleados activos',
                '#empleados'
            )
        );
    }

    if (data.users) {
        cards.push(
            metric(
                'Cuentas activas',
                data.users.filter(item => item.active).length,
                'Usuarios del sistema',
                '#usuarios'
            )
        );
    }

    if (financial) {
        cards.push(
            metric(
                'Caja',
                'Consultar',
                'Saldos por reserva',
                '#pagos'
            )
        );
    }

    metrics.replaceChildren(...cards);

    if (settled.some(result => result.status === 'rejected')) {
        metrics.append(el('p', {
            class: 'muted',
            text: 'Algunos indicadores no pudieron consultarse.'
        }));
    }

    if (data.reservations) {
        const items = [...data.reservations]
            .sort((a, b) => b.reservationId - a.reservationId)
            .slice(0, 5);

        activity.append(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', { text: 'Últimas reservas registradas' }),
                    el('a', {
                        href: '#reservas',
                        class: 'btn secondary',
                        text: 'Ver todas'
                    })
                ),
                ...(items.length
                        ? items.map(item =>
                            el('a', {
                                    class: 'aurora-activity-row',
                                    href: `#reservas/${item.reservationId}`
                                },
                                el('div', {},
                                    el('strong', {
                                        text: item.confirmationCode
                                            || `Reserva #${item.reservationId}`
                                    }),
                                    el('p', {
                                        class: 'muted',
                                        text: `${item.checkInDate} — ${item.checkOutDate}`
                                    })
                                ),
                                el('span', {
                                    class: 'aurora-badge',
                                    text: STATUS_LABELS[item.status] || item.status
                                })
                            )
                        )
                        : [el('p', {
                            class: 'muted',
                            text: 'No existen reservas registradas.'
                        })]
                )
            )
        );
    }

    if (data.maintenance) {
        const items = data.maintenance
            .filter(item =>
                ['SCHEDULED', 'IN_PROGRESS'].includes(item.status)
            )
            .slice(0, 5);

        activity.append(
            el('section', { class: 'card' },
                el('div', { class: 'card-header' },
                    el('h2', { text: 'Mantenimientos pendientes' }),
                    el('a', {
                        href: '#mantenimiento',
                        class: 'btn secondary',
                        text: 'Ver todos'
                    })
                ),
                ...(items.length
                        ? items.map(item =>
                            el('a', {
                                    class: 'aurora-activity-row',
                                    href: '#mantenimiento'
                                },
                                el('div', {},
                                    el('strong', {
                                        text: item.category || 'Mantenimiento general'
                                    }),
                                    el('p', {
                                        class: 'muted',
                                        text: item.description
                                    })
                                ),
                                el('span', {
                                    class: 'aurora-badge',
                                    text: STATUS_LABELS[item.status] || item.status
                                })
                            )
                        )
                        : [el('p', {
                            class: 'muted',
                            text: 'No existen trabajos pendientes.'
                        })]
                )
            )
        );
    }
}
