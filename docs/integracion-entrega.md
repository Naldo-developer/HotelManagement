# Integración de entrega — 26/11/2026

Se consolidan en un único commit sobre origin/main `61393d6` los avances de
Ronaldo que aún no estaban integrados: diseño Aurora y entrega local R06.
T08 (#45), R05 y el PR #26 ya pertenecen a main; se conserva su historial.

Avances reutilizados:

- `1cca8fb`: rediseño de la interfaz.
- `089b70e`: identidad Aurora, imagen y monograma.
- `f15c734`: scripts de publicación, validación y arranque local R06.
- `a427c01`: evidencia de entrega local y README.

Las ramas originales se conservan. Este commit consolidado no debe integrarse
en paralelo con los PR anteriores del mismo contenido: usar una única vía de
integración para Aurora/R06.

## Verificación

```powershell
dotnet test HotelManagement.sln --artifacts-path <directorio-temporal> --nologo -- xUnit.ParallelizeTestCollections=false
```

Resultado: **50 aprobadas, 0 fallidas, 0 omitidas**. Incluye las nuevas pruebas
de catálogos T08, el ciclo HTTP R05 y las pruebas existentes.
`R02_POSTGRES_ADMIN` se configuró externamente. PostgreSQL 18 temporal, puerto
55448, solo loopback, usuario exclusivo y bases aleatorias. La instancia se
detuvo al finalizar; no se modificó la base demo de la aplicación.

El primer intento contra PostgreSQL con contraseña obtuvo 48 aprobadas y dos
fallidas en RoomPeriodIntegrityTests: al construir la conexión de prueba desde
admin.ConnectionString después de abrirla, no se conservó la contraseña.
La ejecución final utilizó el entorno temporal con autenticación trust en
loopback documentado para la suite. Esta limitación de la configuración de esas
dos pruebas permanece; no afecta la validación R06 del login con contraseña.

La evidencia de la versión Release R06 `f15c734` se conserva en
[r06-entrega-local.md](r06-entrega-local.md). Una integración nueva se publica en
otra carpeta vacía mediante Publish-Delivery.ps1 y se verifica con
Test-Delivery.ps1. No se sobrescribe la entrega anterior.

## Pendientes que no son commits disponibles

- Y06: no se encontraron commits ni pantallas de check-in, consumos, pagos y
  check-out para integrar en esta revisión.
- Y08: no se encontró integración ni evidencia de aceptación.
- R07: se actualizan los resultados disponibles, pero sigue pendiente la
  aceptación Y08 y la reproducción por otro integrante con fecha, hash,
  comandos y resultados. Esta ejecución de Ronaldo no sustituye esa evidencia.

No se atribuyen tareas ni validaciones no realizadas a los compañeros.
