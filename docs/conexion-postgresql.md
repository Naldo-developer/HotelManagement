\# Conexión a PostgreSQL



\## Preparación local

\- Crear la base hotel\_management.

\- En una base nueva, ejecutar database/001\_initial\_schema.sql.

\- Si el esquema ya existe, no volver a ejecutar el script inicial.



\## Configuración

La API lee ConnectionStrings:DefaultConnection desde la configuración.

En desarrollo, configurar ese valor mediante .NET User Secrets.



En Rider:

1\. Clic derecho en HotelManagement.Api.

2\. Tools > .NET User Secrets.

3\. Agregar ConnectionStrings:DefaultConnection con los datos locales.



Formato de la cadena:

Host=localhost;Port=5432;Database=hotel\_management;Username=USUARIO;Password=CONTRASEÑA



Cada integrante configura sus propias credenciales.

No guardar contraseñas en archivos versionados.



\## Registro

Program.cs llama a AddInfrastructure.

Infrastructure registra HotelManagementDbContext con UseNpgsql.

El contexto tiene duración scoped.



La API requiere la cadena de conexión para arrancar.

No crea tablas ni ejecuta migraciones automáticamente.



\## Verificación realizada

\- Compilación correcta.

\- Seis pruebas automatizadas aprobadas.

\- API iniciada en Development.

\- Consulta manual mediante el contexto a Hotels, RoomTypes y Rooms.

\- Las tres consultas finalizaron correctamente con cero registros.



La comprobación manual se ejecutó desde una consola temporal

fuera del repositorio. El endpoint de salud no verifica PostgreSQL.
