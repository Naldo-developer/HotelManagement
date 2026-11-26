# Verificación de Ronaldo — 26/11/2026

## R05 completado en código

Fetch `git fetch origin --prune` realizado. Main remoto: bd3b48b.
Dependencias integradas: T03 (#43), T06 (#28), C08 (#39) y R04 (#25).
Se reutilizaron 08fc50a y 7586d41; 7cf7807 amplía la prueba existente.

PostgreSQL 18 temporal, solo loopback 55446, usuario exclusivo y bases aleatorias
eliminadas en finally. Esquema 001–004. Password demo aleatoria en memoria.

Comando ejecutado con R02_POSTGRES_ADMIN externo:

```powershell
dotnet test HotelManagement.sln --nologo -- xUnit.ParallelizeTestCollections=false
```

Resultado final: 45 aprobadas, 0 fallidas, 0 omitidas. El ciclo HTTP comprueba
login real, huésped, dos reservas concurrentes (201/409), check-in y repetición
(409), servicio ficticio, consumo de 30, saldo positivo, check-out rechazado
(409) conservando ACTIVE, pago completo, saldo cero, check-out, repetición
(409), cambio operativo T03 y persistencia con contexto nuevo de reserva y
estadía COMPLETED, fecha de salida, un consumo y un pago.
La primera ejecución falló por no copiar SQL 004; se corrigió el proyecto de
pruebas y las dos ejecuciones posteriores pasaron 45/45.

## R06 parcial

R03 (fcbaec1), Y07 (#33) y R05 están disponibles. Se integraron en la rama
existente del PR #26 sin duplicar sus commits. Versión publicada:
`d6690a3c2ec4d5d96ae59af777cb55eedfea9edc`.

```powershell
./scripts/Publish-Delivery.ps1 -OutputDirectory "$env:TEMP/hotel-release-20261126"
```

Resultado: Release correcto, DLL, HTML, CSS y JS presentes en carpeta nueva.
Configuración revisada: conexión por ConnectionStrings__DefaultConnection;
appsettings.Development.json solo tiene Logging. Program usa variables mediante
CreateBuilder y solo aprovisiona cuentas demo en Development. El seed 002
contiene nombres ficticios y correos example.invalid; no incluye contraseñas.

La revisión automática rechazó antes de ejecutarse el comando de creación de
base demo, seed y arranque del DLL: `blocked by policy`, sin motivo adicional.
No se verificaron arranque limpio, HTTP Production, login publicado ni logout.
No se intentó eludir el rechazo. Test-Delivery.ps1 existente queda reutilizable.

Falta ejecutar ese script con base y cuenta autorizada; proporcionar y verificar
URL de entrega, login en destino, HTTPS/proxy y claves Data Protection según
alojamiento. No existe URL externa registrada en los archivos revisados.

## R07 parcial

README actualizado con resultados reales y recorrido disponible. No se encontró
Y08 en el historial de origin/main ni evidencia versionada de reproducción por
otro integrante. Falta aceptación Y08, reproducción ajena con fecha/comandos/hash,
URL verificada y completar R06. Las pruebas de Ronaldo no sustituyen esa evidencia.
Los reportes del 22/11 son históricos; este informe actualiza su estado.
