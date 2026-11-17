using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Api.Tests;

public sealed class RoomPeriodIntegrityTests
{
    private const string Reservation = """
        INSERT INTO hotel.reservations
            (confirmation_code, room_id, primary_guest_id, check_in_date, check_out_date, nightly_rate)
        SELECT 'R04-' || left(gen_random_uuid()::text, 24), r.room_id, (SELECT min(guest_id) FROM hotel.guests),
            DATE '2030-01-12', DATE '2030-01-14', 100
        FROM hotel.rooms r WHERE room_number = 'DEMO-101'
        """;
    private const string Maintenance = """
        INSERT INTO hotel.maintenance (room_id, start_date, end_date, description)
        SELECT room_id, DATE '2030-01-13', DATE '2030-01-15', 'R04 test'
        FROM hotel.rooms WHERE room_number = 'DEMO-101'
        """;

    [PostgreSqlFact]
    public async Task Clean_and_upgrade_installations_match_and_enforce_periods()
    {
        string? cleanSchema = null;
        foreach (var withPreviousData in new[] { false, true })
        {
            await InDatabase(async (connection, connectionString) =>
            {
                await Script(connection, "001_initial_schema.sql");
                if (withPreviousData)
                {
                    await Script(connection, "002_demo_seed.sql");
                    await Execute(connection, Reservation);
                    await Execute(connection, Maintenance.Replace("2030-01", "2030-02"));
                }
                var before = await DataSnapshot(connection);
                await Script(connection, "003_room_period_integrity.sql");
                Assert.Equal(before, await DataSnapshot(connection));
                var schema = (string)(await Scalar(connection, """
                    SELECT string_agg(definition, E'\n' ORDER BY definition) FROM (
                        SELECT c.relname || ':' || a.attname || ':' || format_type(a.atttypid,a.atttypmod)
                            || ':' || a.attnotnull AS definition
                        FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                        JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
                        WHERE n.nspname='hotel' AND c.relkind='r'
                        UNION ALL SELECT conname || ':' || pg_get_constraintdef(oid)
                        FROM pg_constraint WHERE connamespace='hotel'::regnamespace
                        UNION ALL SELECT pg_get_functiondef(p.oid) FROM pg_proc p
                        WHERE p.pronamespace='hotel'::regnamespace
                        UNION ALL SELECT pg_get_triggerdef(t.oid) FROM pg_trigger t
                        JOIN pg_class c ON c.oid=t.tgrelid
                        WHERE c.relnamespace='hotel'::regnamespace AND NOT t.tgisinternal
                    ) definitions
                    """))!;
                if (cleanSchema is null) cleanSchema = schema;
                else Assert.Equal(cleanSchema, schema);
                if (!withPreviousData) await Script(connection, "002_demo_seed.sql");
                else
                {
                    Assert.Equal(2L, await Scalar(connection, "SELECT count(*) FROM hotel.room_period_blocks"));
                    await Execute(connection, "DELETE FROM hotel.reservations; DELETE FROM hotel.maintenance;");
                }

                await Execute(connection, Maintenance);
                var options = new DbContextOptionsBuilder<HotelManagementDbContext>().UseNpgsql(connectionString).Options;
                await using var context = new HotelManagementDbContext(options);
                var repository = new RoomRepository(context);
                var hotelId = (long)(await Scalar(connection, "SELECT min(hotel_id) FROM hotel.hotels"))!;
                var blocked = await repository.GetAvailableAsync(hotelId, new(2030, 1, 12), new(2030, 1, 14), null, default);
                Assert.DoesNotContain(blocked, r => r.RoomNumber == "DEMO-101");
                var adjacent = await repository.GetAvailableAsync(hotelId, new(2030, 1, 15), new(2030, 1, 16), null, default);
                Assert.Contains(adjacent, r => r.RoomNumber == "DEMO-101");
                await Assert.ThrowsAsync<PostgresException>(() => Execute(connection, Reservation));
                await Execute(connection, "UPDATE hotel.maintenance SET status='CANCELLED'");
                await Execute(connection, Reservation);
                var overlap = await Assert.ThrowsAsync<PostgresException>(() => Execute(connection, Reservation));
                Assert.Equal("23P01", overlap.SqlState);
                Assert.Equal("ex_reservations_room_period", overlap.ConstraintName);
                await Execute(connection, "UPDATE hotel.reservations SET status='CANCELLED'");
                await Execute(connection, "UPDATE hotel.maintenance SET status='SCHEDULED'");
                await Execute(connection, "UPDATE hotel.maintenance SET start_date='2030-01-16', end_date='2030-01-18'");
                await Execute(connection, "UPDATE hotel.reservations SET status='PENDING'");
                await Execute(connection, "UPDATE hotel.maintenance SET room_id=(SELECT room_id FROM hotel.rooms WHERE room_number='DEMO-102')");
                await Execute(connection, "UPDATE hotel.maintenance SET start_date='2030-01-13', end_date='2030-01-15'");
                await Assert.ThrowsAsync<PostgresException>(() => Execute(connection,
                    "UPDATE hotel.maintenance SET room_id=(SELECT room_id FROM hotel.rooms WHERE room_number='DEMO-101')"));
                await Execute(connection, "DELETE FROM hotel.maintenance; DELETE FROM hotel.reservations;");
                Assert.Equal(0L, await Scalar(connection, "SELECT count(*) FROM hotel.room_period_blocks"));

                await ConcurrentConflict(connectionString, Reservation, Maintenance);
                await Execute(connection, "DELETE FROM hotel.reservations; DELETE FROM hotel.maintenance;");
                await ConcurrentConflict(connectionString, Maintenance, Reservation);
            });
        }
    }

    [PostgreSqlFact]
    public async Task Upgrade_with_existing_cross_conflict_rolls_back_without_data_loss()
    {
        await InDatabase(async (connection, _) =>
        {
            await Script(connection, "001_initial_schema.sql");
            await Script(connection, "002_demo_seed.sql");
            await Execute(connection, Reservation);
            // Simula datos heredados de la carrera que R04 corrige.
            await Execute(connection, "ALTER TABLE hotel.maintenance DISABLE TRIGGER trg_validate_maintenance");
            await Execute(connection, Maintenance);
            await Execute(connection, "ALTER TABLE hotel.maintenance ENABLE TRIGGER trg_validate_maintenance");
            var before = await DataSnapshot(connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => Script(connection, "003_room_period_integrity.sql"));
            Assert.Equal("23P01", error.SqlState);
            await Execute(connection, "ROLLBACK");
            Assert.Equal(before, await DataSnapshot(connection));
            Assert.Equal(true, await Scalar(connection, "SELECT to_regclass('hotel.room_period_blocks') IS NULL"));
        });
    }

    private static async Task ConcurrentConflict(string connectionString, string firstSql, string secondSql)
    {
        await using var first = new NpgsqlConnection(connectionString);
        await using var second = new NpgsqlConnection(connectionString);
        await first.OpenAsync();
        await second.OpenAsync();
        await using var transaction = await first.BeginTransactionAsync();
        await Execute(first, firstSql);
        var pending = Execute(second, secondSql);
        // Espera al bloqueo real, no a una pausa arbitraria; segundo comando aun no ve el primero.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!(bool)(await Scalar(first, $"SELECT cardinality(pg_blocking_pids({second.ProcessID})) > 0"))!)
        {
            if (pending.IsCompleted || DateTime.UtcNow > deadline)
            {
                await transaction.RollbackAsync();
                await pending;
                Assert.Fail("La escritura concurrente no espero la transaccion rival.");
            }
            await Task.Delay(20);
        }
        await transaction.CommitAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() => pending);
        Assert.Equal("23P01", error.SqlState);
        Assert.Equal("ex_room_period_blocks_overlap", error.ConstraintName);
    }

    private static async Task<string> DataSnapshot(NpgsqlConnection connection) =>
        (string)(await Scalar(connection, """
            SELECT jsonb_build_object(
                'rooms', (SELECT jsonb_agg(to_jsonb(r) ORDER BY room_id) FROM hotel.rooms r),
                'guests', (SELECT jsonb_agg(to_jsonb(g) ORDER BY guest_id) FROM hotel.guests g),
                'reservations', (SELECT jsonb_agg(to_jsonb(r) ORDER BY reservation_id) FROM hotel.reservations r),
                'maintenance', (SELECT jsonb_agg(to_jsonb(m) ORDER BY maintenance_id) FROM hotel.maintenance m)
            )::text
            """))!;

    private static async Task InDatabase(Func<NpgsqlConnection, string, Task> test)
    {
        var database = "hotel_r04_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(Environment.GetEnvironmentVariable("R02_POSTGRES_ADMIN"));
        await admin.OpenAsync();
        await Execute(admin, $"CREATE DATABASE {database}");
        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(admin.ConnectionString)
                { Database = database, Pooling = false, SearchPath = "hotel,public" }.ConnectionString;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await test(connection, connectionString);
        }
        finally { await Execute(admin, $"DROP DATABASE {database} WITH (FORCE)"); }
    }

    private static async Task Script(NpgsqlConnection connection, string file) =>
        await Execute(connection, await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", file)));

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> Scalar(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
