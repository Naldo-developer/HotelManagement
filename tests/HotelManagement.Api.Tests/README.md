# Pruebas R02 con PostgreSQL

`RoomPersistenceTests` usa PostgreSQL real y TestServer con los controladores,
servicios de aplicación, repositorios y manejo de errores de la API. No necesita
arrancar Kestrel ni modificar `Program.cs` o los secretos locales.

En PowerShell, iniciar una instancia temporal aislada (ajustar la ruta de los
binarios de PostgreSQL si corresponde):

```powershell
$pgBin = 'C:/Program Files/PostgreSQL/18/bin'
$r02Data = Join-Path $env:TEMP ('hotel-r02-' + [guid]::NewGuid().ToString('N'))
& "$pgBin/initdb.exe" -D $r02Data -U r02_test --auth=trust --encoding=UTF8 --locale=C
& "$pgBin/pg_ctl.exe" -D $r02Data -l "$r02Data/server.log" -o '-h 127.0.0.1 -p 55440' -w start
try {
    $env:R02_POSTGRES_ADMIN = 'Host=127.0.0.1;Port=55440;Database=postgres;Username=r02_test'
    dotnet test HotelManagement.sln --nologo
} finally {
    Remove-Item Env:R02_POSTGRES_ADMIN -ErrorAction SilentlyContinue
    & "$pgBin/pg_ctl.exe" -D $r02Data -m fast -w stop
}
```

La instancia escucha solo en loopback y usa autenticación trust para esta prueba
local. El puerto debe estar libre. No usar esta configuración en un servidor
compartido. También se puede configurar `R02_POSTGRES_ADMIN` con una conexión a
una instancia de prueba existente cuyo usuario pueda crear y eliminar bases.
No guardar credenciales en el repositorio.

La suite también admite autenticación por contraseña en la conexión externa.
RoomPeriodIntegrityTests conserva la cadena original al construir conexiones a
bases aleatorias, para no perder la contraseña después de abrir la conexión
administrativa. Esa configuración se verificó con las 50 pruebas aprobadas.

El workflow de GitHub Actions levanta PostgreSQL 18 con una cuenta desechable,
ejecuta Release y rechaza resultados con pruebas omitidas. No utiliza la base
ni las credenciales de desarrollo de los integrantes.

La prueba crea una base vacía `hotel_r02_<GUID>`, aplica los scripts 001 y 002 de
R01 y elimina únicamente esa base en `finally`. No altera la base indicada en
la conexión administrativa. Si falta la variable, se informa como omitida;
una ejecución válida de R02 debe mostrar cero pruebas omitidas.

Verifica el listado inicial de tres habitaciones, POST con 201 y Location,
GET por ID con todos los campos enviados, duplicado dentro del mismo hotel con
409 y ProblemDetails, y persistencia tras detener el servidor mediante un
DbContext y una conexión nuevos. Comprueba que solo existe una fila con ese
número en el hotel y cuatro habitaciones en total.
