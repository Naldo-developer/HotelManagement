# Preparacion parcial de R05, R06 y R07

Este documento registra pasos verificables; no sustituye la aceptacion final.
Base revisada: origin/main 19b5c67. R04 se propone en PR #25 y debe fusionarse y
aplicarse antes del ciclo. No se registra un hash final ni una URL de entrega aun.

## R05: escenarios para automatizar al existir los contratos

Reutilizar la base aleatoria y limpieza en finally de RoomPersistenceTests.
No fijar rutas ni DTO de reservas/estadias/pagos antes de los PR de sus autores.
Cada escenario necesita datos propios y una lectura posterior con contexto nuevo.

| Escenario | Resultado verificable | Dependencia |
| --- | --- | --- |
| Huesped -> reserva -> check-in -> consumo -> pago -> check-out | IDs relacionados, importes persistidos y estadia cerrada | T06, C08 |
| Dos reservas simultaneas de una habitacion e intervalo | Un 201 y un 409; una reserva activa | T05, R04 |
| Reserva frente a mantenimiento simultaneo | Un solo periodo activo; conflicto sin alta parcial | R04, endpoint correspondiente |
| Check-in repetido y cancelacion concurrente | Una estadia; transicion consistente | T06, C05 |
| Check-out con saldo | 409; estadia sigue abierta y no cambia el historial indebidamente | C08 |
| Pago completo y segundo check-out | Cierra una vez; habitacion disponible si es operativa | C07, C08, T03 |
| Reintento de pago | No cobra ni descuenta saldo dos veces | Contrato de idempotencia de Charly |
| Cambio operativo con reserva futura/estadia abierta | Rechazo sin perder referencias | T03 |

R05 no se cierra con pruebas SQL aisladas: necesita atravesar los endpoints reales
de T03, T06 y C08. Thomas tiene el siguiente paso T05 -> T06; Charly C05 -> C08;
Thomas T03 despues de T06/C05; Ronaldo integra y automatiza R05.

## R06: publicar y arrancar sin launchSettings

Requisitos: SDK .NET 10 para publicar; runtime ASP.NET Core 10 en destino cuando
se use publicacion dependiente del framework; PostgreSQL accesible y esquema/seed
aplicados con credenciales externas. La publicacion no aplica SQL ni crea cuentas.

Desde la raiz del clon, elegir una carpeta nueva para evitar residuos de otra version:

```powershell
./scripts/Publish-Delivery.ps1 -OutputDirectory C:/hotel-delivery
# Configurar ConnectionStrings__DefaultConnection fuera del repositorio.
# Ejemplo de estructura: Host=...;Database=...;Username=...;Password=...
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5086'
Push-Location C:/hotel-delivery
try { dotnet HotelManagement.Api.dll } finally { Pop-Location }
```

La variable de conexion es obligatoria: appsettings.Development.json solo contiene
logging y los secretos de desarrollo no viajan en publish. En otra terminal:

```powershell
Invoke-RestMethod http://127.0.0.1:5086/api/health
(Invoke-WebRequest http://127.0.0.1:5086/ -UseBasicParsing).StatusCode
```

La prueba de salud solo acredita proceso HTTP; no acredita PostgreSQL ni login.
En Production Swagger debe responder 404 y /api/auth/me sin cookie debe dar 401.
La web usa rutas /api relativas al mismo origen.

Autenticacion actual: cookie hotel_session; cuentas demo solo se aprovisionan en
Development cuando DemoAccounts:Password esta configurada externamente. Antes de
cerrar R06, acordar con Charly el aprovisionamiento en entrega y persistencia de
claves Data Protection (especialmente reinicios, contenedores o varias replicas).
Definir HTTPS y proxy segun el alojamiento real. No hay URL de entrega validada.

Bloqueos: R05, Y07 y validacion de inicio de sesion en el destino. Charly debe
integrar C04; Yadhira Y07 y su comprobacion con Y05/Y06. Ronaldo prepara y valida
el alojamiento tras esas integraciones.

Verificacion parcial ejecutada el 16 de noviembre de 2026: el script publico
Release desde 19b5c67 con HTML, CSS y JS. Se arranco el DLL publicado en Production
sin launchSettings, con conexion por entorno a una base temporal PostgreSQL 18.6
con 001 aplicado. Salud, raiz, /js/app.js y /css/styles.css respondieron 200;
/swagger y /api/no-existe 404; /api/auth/me y login con usuario inexistente 401.
No se probo login valido, HTTPS, reinicio de sesion ni ciclo completo. La instancia
temporal y el proceso publicado se detuvieron despues de verificar.

## R07: datos que debe contener el README final

- Requisitos y comandos de base, build, pruebas y publish realmente ejecutados.
- Variables externas, URL de entrega y procedimiento de cuentas sin secretos.
- Recorrido de habitaciones y huesped -> reserva -> estadia -> caja -> cierre.
- Roles y errores esperados (401/403/409), recarga y persistencia.
- Autores y enlaces de los PR integrados, hash de release y resultados reales.

No copiar resultados de una revision antigua como evidencia final. Yadhira debe
completar Y08 tras R06, Y04A, Y06 y Y07. Ronaldo actualiza el README definitivo
despues de esa aceptacion. Hasta entonces R07 permanece parcial.

## Actualizacion R06 — 22/11/2026

Rama sincronizada con origin/main 28cfee6; PR #26 sigue abierto.
R04, T05/T06, C04/C05 y Y07 ya integrados. R05 completo sigue pendiente
de T03 y C06-C08. No se acredita entrega final.

Publish-Delivery.ps1 ejecutado en Release con exito sobre esta base:
DLL, HTML, CSS y JavaScript presentes. Test-Delivery.ps1 tiene sintaxis
PowerShell validada, pero su ejecucion completa no se verifico: la revision
automatica rechazo el comando de arranque del DLL con aprovisionamiento demo,
con mensaje "blocked by policy" sin razon adicional.

Uso para el entorno de entrega, con cuenta ADMIN o RECEPTION ya aprovisionada:

    ./scripts/Publish-Delivery.ps1 -OutputDirectory C:/hotel-delivery-nuevo
    # Configurar externamente ConnectionStrings__DefaultConnection,
    # DELIVERY_USERNAME y DELIVERY_PASSWORD, sin guardarlos en el repositorio.
    ./scripts/Test-Delivery.ps1 -PublishedDirectory C:/hotel-delivery-nuevo

La prueba arranca Production sin launchSettings, comprueba estaticos,
401/404, login, /me, lectura de habitaciones y logout. Detiene su proceso
y restaura entorno/URLs al salir. Usar un puerto libre (5086 por defecto).
No crea base ni cuentas; aplicar 001, 002, 003 en orden antes de usarla.
Production no ejecuta DemoAccountSeeder. Para una demo local aislada se puede
aprovisionar primero en Development con DemoAccounts__Password externo,
detener ese proceso y quitar la variable antes de arrancar Production.
El seeder no cambia contrasenas de cuentas existentes.

El login real con hash, cookie y PostgreSQL esta verificado en la rama
feature/r05-ciclo-http: commit 7586d41, suite 45/45 sin omisiones.
Esto no verifica login del DLL publicado, HTTPS, proxy, claves persistentes,
reinicios ni una URL externa. Coordinar esos puntos antes de cerrar R06.

## Reintento R06 del DLL publicado — 22/11/2026

Solicitud: reutilizar Test-Delivery.ps1 del commit 85625a6 en la rama del PR #26.
Base exacta publicada: 0351ba7713db793c4daf25213e4227b2e4e3e061.
Fetch ejecutado; rama limpia y alineada con origin antes del intento.

Comando ejecutado con exito:

    ./scripts/Publish-Delivery.ps1 -OutputDirectory C:/Users/GIGABYTE/AppData/Local/Temp/hotel-r06-production-20261122

Resultado: publicacion Release correcta; el script comprobo HotelManagement.Api.dll,
wwwroot/index.html, wwwroot/js/app.js y wwwroot/css/styles.css.
Test-Delivery.ps1 conserva el contenido de 85625a6 y pasa el analizador de
sintaxis PowerShell. No se detecto un fallo de ejecucion del script, porque
no llego a ejecutarse.

Se inicializo y arranco PostgreSQL temporal con usuario exclusivo r06_test,
autenticacion trust local y escucha 127.0.0.1:55446. El siguiente comando,
que iba a crear la base aislada, aplicar 001/002/003, aprovisionar cuentas
ficticias con una password aleatoria en Development y ejecutar el script
sobre el DLL en Production, fue rechazado antes de ejecutarse por la revision
automatica: "Rejected ... blocked by policy". No se proporciono una razon
adicional. No se intento eludir ese rechazo.

Por tanto, en este intento no se crearon la base ni las cuentas, no arranco
el DLL y no se verificaron HTTP ni login en Production. PostgreSQL temporal
se detuvo al finalizar. No se modificaron appsettings ni secretos locales.

Pendiente para R06: ejecutar Test-Delivery.ps1 en un entorno que permita el
arranque, con PostgreSQL y cuenta autorizada ya aprovisionados. Verificar
salud, estaticos, 401/404, login, lectura PostgreSQL y logout. Tambien quedan
R05 completo (T03 y C06-C08), alojamiento/URL de entrega, HTTPS/proxy,
aprovisionamiento del destino y persistencia de claves Data Protection.
La publicacion correcta no cierra R06. Las 45 pruebas del reporte anterior
no se repitieron en este intento y no sustituyen la prueba del DLL publicado.
