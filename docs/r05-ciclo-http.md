# R05 parcial y orden pendiente de Ronaldo — 22/11/2026

Base: origin/main 28cfee6 (PR #35, C05). Guia consultada:
Downloads/Guia_Commits_HotelManagement (1).docx, modificada 15/11/2026.
Su diagnostico inicial 936e9f8 ya no representa el codigo actual.

## Auditoria

- Main limpio se actualizo por fast-forward desde 19b5c67. Sin commits locales
  pendientes de subir antes de empezar. Los dos stashes se conservaron.
- R01: scripts de esquema y seed; R02: RoomPersistenceTests; R03: web estatica;
  R04: 003_room_period_integrity.sql y RoomPeriodIntegrityTests ya integrados.
- Se integraron T05/T06 (reservas), C04 (roles), C05 (estadias),
  Y02/Y03/Y04/Y04B/Y05/Y07 (pantallas y sesion).
- Consulta GitHub REST: unico PR abierto #26, rama
  chore/r05-r07-preparacion, commit 9a642e8. Contiene preparacion de publicacion
  y escenarios; no duplicar esos archivos. gh no esta instalado.
- No hay endpoints de consumos, pagos, check-out ni cambio operativo T03.
  Las entidades del esquema no equivalen a casos de uso implementados.

## Avance verificable de R05

HotelCycleHttpTests recorre controladores reales con CQRS, DI, autorizacion
y repositorios PostgreSQL. Crea un huesped, lanza dos POST de reserva del mismo
intervalo, exige un 201 y un 409, abre la estadia y rechaza repetir check-in.
Consulta Location y verifica reserva CHECKED_IN y una sola estadia ACTIVE
con un contexto nuevo despues de detener TestServer.

Usa el mismo R02_POSTGRES_ADMIN y atributo PostgreSqlFact de la suite.
Cada ejecucion crea una base aleatoria, aplica 001/002/003 y la elimina en finally.
La cookie y usuario son exclusivos de prueba; no valida login con contrasena.
Los tests existentes de C05 cubren bloqueos concurrentes a nivel repositorio;
esta prueba agrega el recorrido HTTP, no sustituye esas comprobaciones.

## Orden actualizado

Se adelanta solo la parte HTTP de R05 porque T06/C05/R04 ya estan integrados.
La guia mantiene su orden de cierre:

1. R05 restante: T03 y C06 -> C07 -> C08. Agregar consumos, pagos,
   check-out con saldo y ciclo completo. No inventar contratos del equipo.
2. R06: R05 completo; revisar PR #26, aprovisionamiento de cuentas de entrega,
   alojamiento, HTTPS y claves Data Protection. Y07 y R03 ya estan integrados.
3. R07: R06 y aceptacion Y08; Y08 requiere tambien Y04A y Y06.
   README y evidencia final deben reflejar una entrega realmente validada.

No se acredita URL de entrega, login real ni ciclo hasta check-out.
No se modificaron secretos ni appsettings.Development.json.

## Verificacion ejecutada

- dotnet build HotelManagement.sln --nologo: correcto, 0 errores y 0 advertencias.
- Suite final: 45 aprobadas, 0 fallidas, 0 omitidas en PostgreSQL 18 temporal.
  Comando: dotnet test HotelManagement.sln --no-build --nologo -- xUnit.ParallelizeTestCollections=false
- R02_POSTGRES_ADMIN externo con Command Timeout=120 y Timeout=60.
  Puerto loopback 55445; instancia detenida al terminar.
- Primera ejecucion: 44 existentes aprobadas y error de nombre de propiedad
  en el nuevo test; corregido usando GuestDto.Id. Segunda ejecucion tuvo
  timeouts de infraestructura incluso en clases existentes. La ejecucion
  final sin paralelismo entre clases paso completa.

## Continuacion — 22/11/2026

Fetch y GitHub REST confirman main 28cfee6 sin novedades y PR #26 abierto.
T03, C06, C07, C08 y Y08 no estan integrados.
Se reutiliza 08fc50a y se sustituye la cookie artificial por login real:
cuentas de DemoAccountSeeder con password aleatoria efimera, credenciales
incorrectas 401, login valido 200 y /api/auth/me 200.
El ciclo usa la cookie real y usuario persistido; sin endpoint test/login.
Build: 0 errores, 0 advertencias. Suite PostgreSQL real: 45/45, 0 omitidas,
sin paralelismo entre clases (Command Timeout=120; Timeout=60).
Esta evidencia sustituye la limitacion anterior de login no verificado.
Consumos, pagos, saldo y check-out siguen pendientes de C06-C08 y T03.
