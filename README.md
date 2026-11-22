# HotelManagement

API .NET 10 con interfaz estatica del mismo origen y PostgreSQL.
Organizada en Api, Application, Domain e Infrastructure; los casos de uso
siguen CQRS con MediatR y repositorios registrados por DI.

Estado comprobado al 22/11/2026 sobre origin/main
28cfee680c51056b5a894c0a5f963efd120fac74: habitaciones, huespedes,
reservas, autenticacion por cookie, permisos y apertura de estadias.
Consumos, pagos, check-out y cambio operativo de habitaciones siguen pendientes.
Este README es un avance R07; no acredita una entrega final ni URL publica.

## Preparar y ejecutar

Requiere SDK .NET 10 y PostgreSQL (las pruebas se ejecutaron con version 18).
En una base de demo vacia, aplicar en orden los scripts descritos en
[database/README.md](database/README.md):

1. database/001_initial_schema.sql
2. database/002_demo_seed.sql
3. database/003_room_period_integrity.sql

Configurar externamente ConnectionStrings__DefaultConnection. No guardar
credenciales en appsettings ni subirlas a Git. En Development, la variable
DemoAccounts__Password permite crear cuentas demo con password hasheada:
admin.demo, recepcion.demo, caja.demo y mantenimiento.demo. Debe tener al
menos ocho caracteres; no cambia passwords de cuentas existentes.
Production no crea cuentas automaticamente.

Desde la raiz:

    dotnet build HotelManagement.sln --nologo
    dotnet run --project src/HotelManagement.Api --launch-profile http

El perfil http usa Development en http://localhost:5075.
La web esta en /, salud en /api/health y Swagger en /swagger.
Swagger solo esta disponible en Development. Salud comprueba HTTP,
no conectividad PostgreSQL. El perfil https usa https://localhost:7143
y requiere certificado de desarrollo de confianza.

## Recorrido disponible

Iniciar sesion en la web. ADMIN puede registrar y editar habitaciones;
ADMIN y RECEPTION operan huespedes y reservas. Crear un huesped,
buscar disponibilidad por fechas, reservar y consultar o cancelar la reserva.
Recargar comprueba que los datos siguen visibles.

El check-in de C05 esta disponible por API (POST /api/stays/check-in)
y requiere una reserva vigente y usuario autorizado. Consultar el detalle
devuelto en Location. Repetir check-in devuelve 409. La interfaz del ciclo
completo depende de Y06; no se puede completar aun consumo, pago y check-out.

Sin sesion las rutas protegidas devuelven 401; sin rol autorizado, 403.
Duplicados, solapamientos y transiciones incompatibles devuelven 409.
Consultar [C05](docs/c05-estadias.md) y [T06](docs/t06-reservas.md).

## Compilacion y pruebas

    dotnet build HotelManagement.sln --nologo
    dotnet test HotelManagement.sln --no-build --nologo -- xUnit.ParallelizeTestCollections=false

Para las pruebas PostgreSQL definir R02_POSTGRES_ADMIN con una conexion
administrativa a una instancia de prueba aislada. Cada prueba crea y elimina
su base aleatoria. Instrucciones en [tests](tests/HotelManagement.Api.Tests/README.md).
Sin esa variable las pruebas de integracion se omiten: no equivale a verificar
persistencia. Se uso Command Timeout=120 y Timeout=60 en el entorno local.

Evidencia del 22/11/2026:
- Build de la rama feature/r05-ciclo-http: 0 errores y 0 advertencias.
- Suite con PostgreSQL real: 45 aprobadas, 0 fallidas, 0 omitidas.
- R05 (7586d41, sobre 08fc50a): login real con cuentas efimeras, credenciales
  incorrectas, cookie, huesped, reserva concurrente y check-in repetido.
- Publish Release en la rama chore/r05-r07-preparacion: correcto; DLL y
  archivos estaticos verificados. La prueba completa del DLL publicado
  no pudo ejecutarse por rechazo automatico del comando de arranque.

## Publicacion y pendientes

    ./scripts/Publish-Delivery.ps1 -OutputDirectory C:/hotel-delivery-nuevo

En destino usar runtime ASP.NET Core 10, PostgreSQL y cuentas aprovisionadas.
Configurar ConnectionStrings__DefaultConnection, ASPNETCORE_ENVIRONMENT=Production
y ASPNETCORE_URLS externamente. Arrancar desde la carpeta publicada:

    dotnet HotelManagement.Api.dll

[Test-Delivery.ps1](scripts/Test-Delivery.ps1) prepara una comprobacion con
DELIVERY_USERNAME y DELIVERY_PASSWORD externos. Su sintaxis fue validada;
el recorrido publicado permanece sin verificar. Ver detalles y limites en
[preparacion de entrega](docs/preparacion-entrega.md).

Orden de cierre: R05 completo depende de T03 y C06-C08; R06 depende de R05
y de validar alojamiento, login publicado, HTTPS y claves Data Protection;
R07 final depende de R06 y Y08. Y08 requiere Y04A y Y06, aun pendientes.
No hay hash de release ni URL de entrega validada.

## Equipo y revision

Ronaldo Cusi: PostgreSQL, configuracion y entrega (R01-R07).
Thomas: catalogos, habitaciones y reservas. Charly: autenticacion, huespedes,
estadias y caja. Yadhira: interfaz y aceptacion en navegador.

Evidencia integrada: [R04 #25](https://github.com/Naldo-developer/HotelManagement/pull/25),
[T06 #28](https://github.com/Naldo-developer/HotelManagement/pull/28),
[Y07 #33](https://github.com/Naldo-developer/HotelManagement/pull/33),
[C04 #34](https://github.com/Naldo-developer/HotelManagement/pull/34) y
[C05 #35](https://github.com/Naldo-developer/HotelManagement/pull/35).
La preparacion R06/R07 sigue en revision en
[PR #26](https://github.com/Naldo-developer/HotelManagement/pull/26).
Los merges se realizan mediante revision del equipo, sin pushes directos a main.
