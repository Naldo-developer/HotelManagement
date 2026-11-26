# R06 — entrega local verificada

Fecha: 26/11/2026. Modalidad confirmada por Ronaldo: entrega en esta PC.
R06 queda completado para esa modalidad; R07/Y08 siguen su revisión independiente.
No se acredita alojamiento público ni acceso desde otra computadora.

## Versión y ubicación

- Main consultado mediante fetch: `51664bc` (PR #26 integrado).
- Dependencias disponibles: R03, R05 (45/45 en su ejecución registrada) y Y07.
- Fuente Release probada: `f15c734a7177579722c7bb6af0018bfa0872553a`.
- Directorio: `%LOCALAPPDATA%/HotelManagement/Delivery/f15c734a`.
- URL verificada: **http://127.0.0.1:5086**.
- Runtime: ASP.NET Core/.NET 10; PostgreSQL 18 local, base `hotelmanagement`.
- Esquema 001–004 y datos ficticios R01 ya aprovisionados. Production no crea
  tablas ni cuentas. La cuenta demo se creó antes, durante la configuración local.

## Comandos ejecutados

Desde la raíz del repositorio (PowerShell), usando una carpeta nueva:

```powershell
./scripts/Publish-Delivery.ps1 -OutputDirectory "$env:LOCALAPPDATA/HotelManagement/Delivery/f15c734a"
```

Este comando ya se ejecutó. Para otra publicación, elegir otra carpeta vacía;
el script rechaza carpetas con contenido para evitar mezclar versiones.

Con `ConnectionStrings__DefaultConnection`, `DELIVERY_USERNAME` y
`DELIVERY_PASSWORD` proporcionadas externamente, sin imprimir sus valores:

```powershell
./scripts/Test-Delivery.ps1 -PublishedDirectory "$env:LOCALAPPDATA/HotelManagement/Delivery/f15c734a" -RequireDemoData
```

La prueba necesita el puerto 5086 libre. También admite `-Port` para usar otro.
Usa las cuentas ya aprovisionadas y no crea ni elimina datos de negocio; el login
actualiza su fecha de último acceso. Detiene sus procesos al terminar.

Para iniciar la entrega en esta PC:

```powershell
./scripts/Start-LocalDelivery.ps1 -PublishedDirectory "$env:LOCALAPPDATA/HotelManagement/Delivery/f15c734a"
```

Abrir http://127.0.0.1:5086. Detener con Ctrl+C en la terminal del lanzador.
Si ya hay una instancia funcionando, usar esa URL y no iniciar otra en el mismo
puerto. El desarrollo en 5075 es un proceso distinto.

El lanzador fija Production y escucha solo en loopback. Toma la conexión de la
variable de entorno si existe; en su defecto, lee la conexión de los User Secrets
locales y la pasa al proceso mediante el entorno. No copia archivos de secretos
al publish ni aprovisiona usuarios. Restaura las variables al finalizar.

## Resultados reales

| Comprobación | Resultado |
| --- | --- |
| Publish Release desde carpeta nueva | Correcto |
| Arranque del DLL sin launchSettings | Correcto, Production |
| Conexión configurada por entorno | Correcto |
| Arranque sin conexión configurada | Rechazado con el mensaje esperado |
| Configuración publicada | Sin cadena de conexión ni password demo |
| `/`, CSS, JS, imagen y favicon Aurora | HTTP 200 |
| `/swagger`, API inexistente | HTTP 404 |
| `/api/auth/me` sin sesión | HTTP 401 |
| Contraseña incorrecta | HTTP 401 |
| Login válido y `/api/auth/me` | Correctos |
| Lectura de habitaciones y huéspedes con el lanzador | 3 habitaciones y 3 huéspedes |
| Datos ficticios | DEMO-101 y ana@example.invalid presentes |
| Reinicio del DLL | Correcto, cookie de sesión conservada |
| Logout y consulta posterior de sesión | Correcto, HTTP 401 |
| Lanzador local y login en la URL de entrega | Correctos |

La prueba ampliada pasó dos veces: primero sobre el Release inicial de la sesión
y después sobre el Release exacto f15c734. La comprobación del lanzador se realizó
además sobre esa versión. No se volvió a ejecutar la suite R05 en este cambio de
scripts y documentación; su evidencia 45/45 permanece en el reporte anterior.

La sesión conserva sus claves con el almacenamiento predeterminado de Data
Protection de esta cuenta Windows; se verificó el reinicio desde el mismo
directorio. No se verificó traslado de cookies a otra cuenta, PC o directorio.
El almacenamiento de claves está fuera de Git y de la carpeta publicada.

La entrega acordada usa HTTP en loopback. Si cambia a un servidor o acceso por
red, hay que validar HTTPS, proxy y almacenamiento de claves en ese destino.

Este resultado sustituye el bloqueo de arranque registrado en los informes
previos: en esta sesión el arranque autorizado sí se ejecutó y pasó las pruebas.
