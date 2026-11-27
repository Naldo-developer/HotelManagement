using System.Data.Common;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data;
using HotelManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HotelManagement.Api.Tests;

public sealed class StayConcurrencyTests
{
    [PostgreSqlFact]
    public async Task Two_check_ins_create_only_one_stay()
    {
        await InDatabase(async connectionString =>
        {
            await AssertRace(
                connectionString,
                firstIsCheckIn: true,
                secondIsCheckIn: true);

            await AssertState(
                connectionString,
                expectedStatus: "CHECKED_IN",
                expectedStays: 1);
        });
    }

    [PostgreSqlFact]
    public async Task Cancellation_first_rejects_check_in()
    {
        await InDatabase(async connectionString =>
        {
            await AssertRace(
                connectionString,
                firstIsCheckIn: false,
                secondIsCheckIn: true);

            await AssertState(
                connectionString,
                expectedStatus: "CANCELLED",
                expectedStays: 0);
        });
    }

    [PostgreSqlFact]
    public async Task Check_in_first_rejects_cancellation()
    {
        await InDatabase(async connectionString =>
        {
            await AssertRace(
                connectionString,
                firstIsCheckIn: true,
                secondIsCheckIn: false);

            await AssertState(
                connectionString,
                expectedStatus: "CHECKED_IN",
                expectedStays: 1);
        });
    }

    [PostgreSqlFact]
    public async Task Previous_open_stay_blocks_another_reservation()
    {
        await InDatabase(async connectionString =>
        {
            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            // Una reserva anterior terminó en fechas, pero su estadía
            // sigue abierta porque todavía no se realizó check-out.
            await Execute(connection, """
                INSERT INTO hotel.reservations
                    (confirmation_code, room_id, primary_guest_id,
                     check_in_date, check_out_date, nightly_rate, status)
                SELECT
                    'C05-PREVIOUS',
                    room_id,
                    primary_guest_id,
                    check_in_date - 2,
                    check_in_date,
                    nightly_rate,
                    'CHECKED_IN'
                FROM hotel.reservations
                WHERE confirmation_code = 'C05-CURRENT';

                INSERT INTO hotel.stays
                    (reservation_id, check_in_at, status, check_in_by_user_id)
                SELECT
                    reservation_id,
                    CURRENT_TIMESTAMP - INTERVAL '2 days',
                    'ACTIVE',
                    (SELECT min(user_id) FROM hotel.app_users)
                FROM hotel.reservations
                WHERE confirmation_code = 'C05-PREVIOUS';
                """);

            await using var context = CreateContext(connectionString);

            var reservationId = await CurrentReservationId(context);
            var userId = await context.AppUsers
                .Select(user => user.UserId)
                .SingleAsync();

            var repository = new StayRepository(context);

            var error = await Assert.ThrowsAsync<BusinessConflictException>(
                () => repository.CheckInAsync(
                    reservationId,
                    userId,
                    "No debe ocupar una habitación ya tomada.",
                    CancellationToken.None));

            Assert.Equal(
                "La habitación ya tiene una estadía abierta.",
                error.Message);

            // Contexto nuevo para comprobar lo que realmente persistió.
            await using var verification = CreateContext(connectionString);

            var reservation = await verification.Reservations
                .AsNoTracking()
                .SingleAsync(item => item.ReservationId == reservationId);

            Assert.Equal("PENDING", reservation.Status);

            Assert.False(await verification.Stays.AnyAsync(
                stay => stay.ReservationId == reservationId));

            Assert.Equal(
                1,
                await verification.Stays.CountAsync(
                    stay => stay.Status == "ACTIVE"));
        });
    }

    [PostgreSqlFact]
    public async Task Reserved_room_rejects_type_change_but_allows_floor_edit()
    {
        await InDatabase(async connectionString =>
        {
            long roomId;
            long originalTypeId;
            string? originalFloor;

            // Intento de cambiar el tipo de una habitación reservada.
            await using (var context = CreateContext(connectionString))
            {
                var room = await context.Rooms
                    .AsNoTracking()
                    .SingleAsync(item => item.RoomNumber == "DEMO-101");

                roomId = room.RoomId;
                originalTypeId = room.RoomTypeId;
                originalFloor = room.Floor;

                var otherTypeId = await context.RoomTypes
                    .Where(type =>
                        type.HotelId == room.HotelId &&
                        type.RoomTypeId != originalTypeId)
                    .Select(type => type.RoomTypeId)
                    .FirstAsync();

                room.RoomTypeId = otherTypeId;
                room.Floor = "99";

                var repository = new RoomRepository(context);

                var error = await Assert.ThrowsAsync<BusinessConflictException>(
                    () => repository.UpdateAsync(
                        room,
                        CancellationToken.None));

                Assert.Equal(
                    "No se puede cambiar el hotel ni el tipo de una habitación con reservas registradas.",
                    error.Message);
            }

            // Un contexto nuevo confirma que el rechazo no guardó
            // ni el cambio de tipo ni el cambio de piso.
            await using (var context = CreateContext(connectionString))
            {
                var room = await context.Rooms
                    .AsNoTracking()
                    .SingleAsync(item => item.RoomId == roomId);

                Assert.Equal(originalTypeId, room.RoomTypeId);
                Assert.Equal(originalFloor, room.Floor);

                // Manteniendo hotel y tipo, editar el piso sí se permite.
                room.Floor = "2";

                await new RoomRepository(context).UpdateAsync(
                    room,
                    CancellationToken.None);
            }

            // Verifica persistencia y conservación de la reserva.
            await using (var verification = CreateContext(connectionString))
            {
                var room = await verification.Rooms
                 .AsNoTracking()
                    .SingleAsync(item => item.RoomId == roomId);

                Assert.Equal(originalTypeId, room.RoomTypeId);
                Assert.Equal("2", room.Floor);

                var reservation = await verification.Reservations
                    .AsNoTracking()
                    .SingleAsync(
                        item => item.ConfirmationCode == "C05-CURRENT");

                Assert.Equal(roomId, reservation.RoomId);
                Assert.Equal("PENDING", reservation.Status);
            }
        });
    }

    private static async Task AssertRace(
        string connectionString,
        bool firstIsCheckIn,
        bool secondIsCheckIn)
    {
        var gate = new ReservationLockGate();

        await using var first = CreateContext(connectionString, gate);
        await using var second = CreateContext(connectionString);
        await using var observer = new NpgsqlConnection(connectionString);

        await first.Database.OpenConnectionAsync();
        await second.Database.OpenConnectionAsync();
        await observer.OpenAsync();

        var reservationId = await CurrentReservationId(first);
        var userId = await first.AppUsers
            .Select(user => user.UserId)
            .SingleAsync();

        var secondConnection =
            (NpgsqlConnection)second.Database.GetDbConnection();

        // La primera operación se pausa después de adquirir
        // el bloqueo real de la reserva.
        Task<Exception?> firstTask = Record.ExceptionAsync(
            () => Operate(
                first,
                firstIsCheckIn,
                reservationId,
                userId));

        Task<Exception?>? secondTask = null;

        try
        {
            await gate.LockAcquired.Task.WaitAsync(
                TimeSpan.FromSeconds(15));

            secondTask = Record.ExceptionAsync(
                () => Operate(
                    second,
                    secondIsCheckIn,
                    reservationId,
                    userId));

            await WaitUntilBlocked(
                observer,
                secondConnection.ProcessID,
                secondTask);
        }
        finally
        {
            // Siempre libera la primera operación, incluso si falla
            // la comprobación del bloqueo.
            gate.Continue.TrySetResult(true);

            await firstTask;

            if (secondTask is not null)
            {
                await secondTask;
            }
        }

        // La primera debe confirmar; la segunda debe leer el nuevo
        // estado después del bloqueo y rechazar la operación.
        Assert.Null(await firstTask);

        Assert.NotNull(secondTask);
        Assert.IsType<BusinessConflictException>(await secondTask!);
    }

    private static async Task Operate(
        HotelManagementDbContext context,
        bool checkIn,
        long reservationId,
        long userId)
    {
        if (checkIn)
        {
            await new StayRepository(context).CheckInAsync(
                reservationId,
                userId,
                "Prueba automatizada C05.",
                CancellationToken.None);
        }
        else
        {
            await new ReservationRepository(context).CancelAsync(
                reservationId,
                "Cancelación de prueba C05.",
                CancellationToken.None);
        }
    }

    private static async Task WaitUntilBlocked(
        NpgsqlConnection observer,
        int processId,
        Task<Exception?> operation)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            await using var command = new NpgsqlCommand(
                "SELECT cardinality(pg_blocking_pids(@pid)) > 0",
                observer);

            command.Parameters.AddWithValue("pid", processId);

            var blocked = (bool)(await command.ExecuteScalarAsync())!;

            if (blocked)
            {
                return;
            }

            if (operation.IsCompleted)
            {
                var error = await operation;

                throw new InvalidOperationException(
                    "La segunda operación terminó sin esperar el bloqueo.",
                    error);
            }

            await Task.Delay(25);
        }

        throw new TimeoutException(
            "No se detectó el bloqueo esperado en PostgreSQL.");
    }

    private static async Task AssertState(
        string connectionString,
        string expectedStatus,
        int expectedStays)
    {
        await using var context = CreateContext(connectionString);

        var reservation = await context.Reservations
            .AsNoTracking()
            .SingleAsync(
                item => item.ConfirmationCode == "C05-CURRENT");

        Assert.Equal(expectedStatus, reservation.Status);

        var stays = await context.Stays
            .AsNoTracking()
            .Where(stay => stay.ReservationId == reservation.ReservationId)
            .ToListAsync();

        Assert.Equal(expectedStays, stays.Count);

        if (expectedStays == 1)
        {
            var stay = Assert.Single(stays);

            Assert.Equal("ACTIVE", stay.Status);
            Assert.Null(stay.CheckOutAt);

            var userId = await context.AppUsers
                .Select(user => user.UserId)
                .SingleAsync();

            Assert.Equal(userId, stay.CheckInByUserId);
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("""
            SELECT count(*)
            FROM hotel.room_period_blocks
            WHERE reservation_id = @reservationId
            """, connection);

        command.Parameters.AddWithValue(
            "reservationId", reservation.ReservationId);

        var blockCount = (long)(await command.ExecuteScalarAsync())!;

        Assert.Equal(
            expectedStatus == "CANCELLED" ? 0L : 1L,
            blockCount);

        Assert.Equal(
            1,
            await context.ReservationStatusHistories.CountAsync(
                history =>
                    history.ReservationId == reservation.ReservationId &&
                    history.OldStatus == "PENDING" &&
                    history.NewStatus == expectedStatus));
    }

    private static Task<long> CurrentReservationId(
        HotelManagementDbContext context)
    {
        return context.Reservations
            .Where(item => item.ConfirmationCode == "C05-CURRENT")
            .Select(item => item.ReservationId)
            .SingleAsync();
    }

    private static HotelManagementDbContext CreateContext(
        string connectionString,
        ReservationLockGate? gate = null)
    {
        var options =
            new DbContextOptionsBuilder<HotelManagementDbContext>()
                .UseNpgsql(connectionString);

        if (gate is not null)
        {
            options.AddInterceptors(gate);
        }

        return new HotelManagementDbContext(options.Options);
    }

    private static async Task InDatabase(Func<string, Task> test)
    {
        var database = "hotel_c05_" + Guid.NewGuid().ToString("N");

        var adminString =
            Environment.GetEnvironmentVariable("R02_POSTGRES_ADMIN")
            ?? throw new InvalidOperationException(
                "Falta R02_POSTGRES_ADMIN.");

        await using var admin = new NpgsqlConnection(adminString);
        await admin.OpenAsync();

        await Execute(admin, $"CREATE DATABASE {database}");

        try
        {
            var connectionString =
                new NpgsqlConnectionStringBuilder(adminString)
                {
                    Database = database,
                    Pooling = false,
                    SearchPath = "hotel,public",
                    CommandTimeout = 30
                }.ConnectionString;

            await using (var connection =
                         new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var file in new[]
                {
                    "001_initial_schema.sql",
                    "002_demo_seed.sql",
                    "003_room_period_integrity.sql"
                })
                {
                    var sql = await File.ReadAllTextAsync(
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "database",
                            file));

                    await Execute(connection, sql);
                }

                await Execute(connection, """
                    UPDATE hotel.hotels SET timezone = 'UTC';

                    WITH employee AS (
                        INSERT INTO hotel.employees
                            (hotel_id, first_name, last_name, active)
                        SELECT min(hotel_id), 'Operador', 'C05', true
                        FROM hotel.hotels
                        RETURNING employee_id
                    )
                    INSERT INTO hotel.app_users
                        (employee_id, username, password_hash, active)
                    SELECT
                        employee_id,
                        'c05.test',
                        'NOT-A-LOGIN-HASH',
                        true
                    FROM employee;

                    INSERT INTO hotel.reservations
                        (confirmation_code, room_id, primary_guest_id,
                         check_in_date, check_out_date,
                         adults, children, nightly_rate, status)
                    SELECT
                        'C05-CURRENT',
                        room_id,
                        (SELECT min(guest_id) FROM hotel.guests),
                        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')::date,
                        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')::date + 1,
                        1,
                        0,
                        120,
                        'PENDING'
                    FROM hotel.rooms
                    WHERE room_number = 'DEMO-101';
                    """);
            }

            await test(connectionString);
        }
        finally
        {
            // Solo elimina la base aleatoria creada por esta prueba.
            await Execute(
                admin,
                $"DROP DATABASE {database} WITH (FORCE)");
        }
    }

    private static async Task Execute(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ReservationLockGate : DbCommandInterceptor
    {
        public TaskCompletionSource<bool> LockAcquired { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Continue { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                    "FOR UPDATE",
                    StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains(
                    "hotel.reservations",
                    StringComparison.OrdinalIgnoreCase))
            {
                LockAcquired.TrySetResult(true);

                await Continue.Task.WaitAsync(
                    TimeSpan.FromSeconds(15),
                    cancellationToken);
            }

            return result;
        }
    }
}