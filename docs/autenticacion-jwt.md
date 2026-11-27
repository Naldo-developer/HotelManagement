# Autenticación JWT

La API autentica mediante JWT Bearer. El login sigue verificando el hash de
contraseña, la actividad de la cuenta y del empleado y los roles actuales.
Tras una autenticación válida emite un token de acceso firmado con HS256.
La cookie `hotel_session` de versiones anteriores ya no autentica solicitudes.

## Contrato HTTP

`POST /api/auth/login` recibe username y password. La respuesta mantiene userId,
username, fullName y roles, y añade accessToken, tokenType (`Bearer`) y
expiresAtUtc. Incluye Cache-Control no-store. No se emite ninguna cookie.
Los tokens son credenciales: no se deben imprimir ni guardar en Git.

Las solicitudes protegidas envían:

```http
Authorization: Bearer <accessToken devuelto por el login>
```

`GET /api/auth/me` devuelve identidad y roles, sin repetir el token.
La API valida firma HS256, emisor, audiencia, presencia de expiración,
vigencia y not-before. Mantiene las políticas Operations, Configuration,
Financial y Maintenance. Token inválido o ausente: 401; rol insuficiente: 403.

## Configuración sin secretos

| Clave | Uso |
| --- | --- |
| Jwt:Issuer | HotelManagement.Api por defecto |
| Jwt:Audience | HotelManagement.Web por defecto |
| Jwt:AccessTokenMinutes | 15 por defecto; rango admitido 1–60 |
| Jwt:SigningKey | Clave aleatoria externa, mínimo 32 bytes UTF-8 |

La clave no tiene valor por defecto. El arranque se rechaza si falta o es corta.
En desarrollo se guarda en User Secrets. En Production se configura
`Jwt__SigningKey`; los demás campos también admiten variables `Jwt__...`.
Para generar una clave de desarrollo, desde PowerShell:

```powershell
$bytes = New-Object byte[] 64
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set 'Jwt:SigningKey' $key --project src/HotelManagement.Api
Remove-Variable key,bytes
```

Ejecutar este comando cambia la clave y deja de aceptar tokens anteriores.
La configuración publicada solo contiene emisor, audiencia y duración.
Start-LocalDelivery.ps1 pasa conexión y clave JWT existentes por entorno sin
copiar archivos de secretos. Conservar la misma clave permite validar un token
no vencido tras reiniciar el servidor.

## Cliente web y Swagger

La web conserva el token únicamente en memoria de la página. El cliente central
añade Bearer a las operaciones y omite cookies. No utiliza localStorage ni
sessionStorage. Recargar o cerrar la página exige autenticarse otra vez.
Un 401 elimina el token y devuelve el flujo al login; un 403 mantiene la sesión.

En Development abrir `/swagger`, ejecutar `/api/auth/login`, copiar accessToken
y pulsar Authorize. Pegar solo el token, sin escribir otra vez el prefijo Bearer.
Login y health son públicos; Swagger marca las operaciones protegidas. Swagger
sigue deshabilitado en Production.

## Expiración y cierre de sesión

Esta versión emite tokens de acceso cortos, sin refresh tokens. Al expirar se
debe iniciar sesión nuevamente. La web cierra sesión descartando el token.
`POST /api/auth/logout` reconoce la acción con 204, pero no revoca copias emitidas:
una copia continúa siendo válida hasta su expiración si la clave no cambia.
No se presenta esta acción como una revocación central.

Los roles están incluidos en el JWT. Cambiar un rol o desactivar una cuenta no
revoca automáticamente sus tokens. Para ese requisito se necesita revalidación,
versionado o un mecanismo persistente de revocación. Un despliegue fuera de
loopback también requiere HTTPS y custodia adecuada de la clave.

## Verificación

Las pruebas cubren firma y claims, rol insuficiente, cookies rechazadas, token
vencido, futuro, alterado, sin firma, con algoritmo no admitido, emisor o audiencia
incorrectos y ausencia de expiración. También comprueban el rechazo de claves
ausentes o cortas y el alcance real del logout.
El ciclo R05 usa login real con Bearer contra PostgreSQL y las pruebas de roles
usan tokens de prueba emitidos exclusivamente en TestServer.

El cliente tiene pruebas con `node --test tests/web/*.test.mjs` para Bearer,
ausencia de cookies, eliminación en 401 y expiración en memoria. CI ejecuta
esas pruebas además de la suite .NET. Test-Delivery.ps1 comprueba el DLL publicado
en Production, login JWT, consultas y token válido tras reinicio.

Resultado local de la migración: **74 pruebas .NET aprobadas, 0 fallidas y 0
omitidas** con PostgreSQL real y bases aleatorias; **3 pruebas JavaScript
aprobadas**. Release publicado en `%LOCALAPPDATA%/HotelManagement/Delivery/jwt-20261126`.
Test-Delivery pasó con datos ficticios. En Development se comprobó además el
documento Swagger con esquema Bearer, login sin Set-Cookie y acceso a identidad
y habitaciones enviando el JWT.

Referencias: [JWT Bearer en ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
y [Swagger con esquemas de seguridad](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/configure-and-customize-swaggergen.md).
