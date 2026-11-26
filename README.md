# HotelManagement

API .NET 10, PostgreSQL e interfaz estática del mismo origen. Capas Api,
Application, Domain e Infrastructure; CQRS con MediatR.

Verificación de Ronaldo del 26/11/2026: R05 implementado y probado; R06 y R07
parciales. [Resultados completos](docs/verificacion-20261126.md).
Main revisado: bd3b48b. Código del ciclo probado: 7cf7807.
Versión Release publicada: d6690a3c2ec4d5d96ae59af777cb55eedfea9edc.
No se acredita una entrega pública ni aceptación Y08.

## Preparar y ejecutar

Requiere SDK .NET 10 y PostgreSQL (pruebas con PostgreSQL 18).
En una base vacía aplicar en orden:

1. database/001_initial_schema.sql
2. database/002_demo_seed.sql
3. database/003_room_period_integrity.sql
4. database/004_maintenance_trigger_search_path.sql

Configurar externamente `ConnectionStrings__DefaultConnection` con host, puerto,
base, usuario y contraseña del entorno. No guardar secretos en Git.
En Development, `DemoAccounts__Password` (mínimo ocho caracteres) aprovisiona
admin.demo, recepcion.demo, caja.demo y mantenimiento.demo con hash.
No cambia contraseñas existentes. Production requiere cuentas ya aprovisionadas.
El seed contiene datos ficticios y correos example.invalid.

```powershell
dotnet build HotelManagement.sln --nologo
dotnet run --project src/HotelManagement.Api --launch-profile http
```

El perfil http configura Development en http://localhost:5075. Web `/`, salud
`/api/health`, Swagger `/swagger` solo en Development. Salud comprueba HTTP,
no PostgreSQL. Estas rutas proceden de configuración; el arranque de esta sesión
no quedó verificado. **URL de entrega: pendiente de proporcionar y verificar.**

## Pruebas ejecutadas

Configurar `R02_POSTGRES_ADMIN` con conexión administrativa a PostgreSQL de prueba.
Cada prueba crea y elimina su base aleatoria. Ver [aislamiento y comandos de
PostgreSQL](tests/HotelManagement.Api.Tests/README.md).

```powershell
dotnet test HotelManagement.sln --nologo -- xUnit.ParallelizeTestCollections=false
```

Resultado real: **45 aprobadas, 0 fallidas, 0 omitidas**. Compilación correcta.
Sin R02_POSTGRES_ADMIN las pruebas de integración se omiten y no acreditan R05.
El ciclo usa login real y comprueba huésped, reserva concurrente 201/409,
check-in repetido 409, consumo, saldo pendiente 409 sin cerrar la estadía,
pago, saldo cero, check-out, segundo check-out 409 y estado operativo T03.
Tras detener TestServer verifica persistencia con contexto nuevo.

## Recorrido de demo

Con datos ficticios y una cuenta autorizada:

1. Iniciar sesión en la web; consultar habitaciones y registrar un huésped.
2. Buscar disponibilidad, reservar y consultar la reserva. Una reserva de prueba
   independiente permite mostrar cancelación T06.
3. Por API, abrir la estadía con `POST /api/stays/check-in` y reservationId.
   Repetir devuelve 409.
4. Crear un servicio ficticio y registrar `POST /api/consumptions` con stayId,
   serviceId y quantity. Consultar `/api/payments/summary/by-reservation/{id}`.
5. Intentar `POST /api/stays/check-out` con stayId mientras hay saldo: 409.
6. Registrar `POST /api/payments` con reservationId, amount y paymentMethod CASH.
   Confirmar saldo cero y completar check-out. Consultar estadía y reserva.
7. Cerrar sesión y comprobar que las rutas protegidas requieren autenticación.

El ciclo HTTP está automatizado; la aceptación visual del ciclo completo y Y08
siguen pendientes del equipo. No se atribuyen esas tareas a Ronaldo.

## Release y entrega

Comando probado desde la rama del PR #26, con destino nuevo:

```powershell
./scripts/Publish-Delivery.ps1 -OutputDirectory "$env:TEMP/hotel-release-20261126"
```

Release correcto: DLL, HTML, CSS y JavaScript presentes. Para el destino usar
runtime ASP.NET Core 10 y configurar externamente ConnectionStrings__DefaultConnection,
ASPNETCORE_ENVIRONMENT=Production y ASPNETCORE_URLS. Desde la carpeta publicada:

```powershell
dotnet HotelManagement.Api.dll
```

Comprobación preparada (pendiente de ejecución), con DELIVERY_USERNAME y
DELIVERY_PASSWORD externos y una cuenta ya aprovisionada:

```powershell
./scripts/Test-Delivery.ps1 -PublishedDirectory "$env:TEMP/hotel-release-20261126"
```

La revisión automática bloqueó el comando de aprovisionamiento y arranque antes
de ejecutarlo (`blocked by policy`, sin razón adicional). No se verificaron
Production, web publicada, login ni URL de entrega. Falta completar esos pasos
y comprobar HTTPS/proxy y persistencia de claves Data Protection en el destino.

## Autores y estado de revisión

Autores identificados en los commits de los PR (la identidad Git no sustituye
una revisión de aceptación):

| Autor | Aportes y PR |
| --- | --- |
| Ronaldo Cusi / Naldo-developer | R03, R04 y preparación R05–R07; [#25](https://github.com/Naldo-developer/HotelManagement/pull/25), [#26](https://github.com/Naldo-developer/HotelManagement/pull/26) |
| chuchuv3l0z (Thomas) | T06 [#28](https://github.com/Naldo-developer/HotelManagement/pull/28), T03 [#43](https://github.com/Naldo-developer/HotelManagement/pull/43) |
| charly calderon / charlycalderon-oss | C06 [#37](https://github.com/Naldo-developer/HotelManagement/pull/37), C07 [#38](https://github.com/Naldo-developer/HotelManagement/pull/38), C08 [#39](https://github.com/Naldo-developer/HotelManagement/pull/39) |
| Starqx-ym (Yadhira) | Y07 [#33](https://github.com/Naldo-developer/HotelManagement/pull/33) y mejora visual #36 |

PR #26 confirmado abierto el 26/11/2026; se reutiliza su rama y preparación.
R05 ya cuenta con sus dependencias integradas y suite completa aprobada.
R06 queda parcial por arranque y entrega pendientes. R07 queda parcial por R06,
URL verificada, integración/aceptación Y08 y reproducción por otro integrante.
No se encontró evidencia versionada de esa reproducción ni un merge Y08.
Registrar integrante, fecha, hash, comandos y resultados cuando ocurra.