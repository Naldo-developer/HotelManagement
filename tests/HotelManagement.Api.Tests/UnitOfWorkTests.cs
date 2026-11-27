using HotelManagement.Application;
using HotelManagement.Application.Dtos.Guests;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Transactions;
using HotelManagement.Application.UseCases.Guests.Commands;
using HotelManagement.Application.UseCases.Reservations.Commands;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using HotelManagement.Api.Configuration;
using HotelManagement.Api.Controllers;
using HotelManagement.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace HotelManagement.Api.Tests;

public sealed class UnitOfWorkTests
{
    [PostgreSqlFact]
    public async Task Http_endpoint_requires_operations_role_and_returns_no_partial_guest_on_failure()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            var input = await Input(scope.ServiceProvider);
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddInfrastructure(connectionString);
            builder.Services.AddApplication();
            builder.Services.AddApiErrorHandling();
            JwtTestSupport.Configure(builder);
            builder.Services.AddApiAuthentication();
            builder.Services.AddControllers().AddApplicationPart(typeof(ReservationsController).Assembly);
            await using var app = builder.Build();
            app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
            app.UseAuthentication();
            app.UseAuthorization();
            JwtTestSupport.MapLogin(app);
            app.MapControllers();
            await app.StartAsync();
            using var client = app.GetTestClient();
            using (var anonymous = await client.PostAsJsonAsync("/api/reservations/with-guest", input))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            await JwtTestSupport.SignInAsync(client, "CASHIER");
            using (var forbidden = await client.PostAsJsonAsync("/api/reservations/with-guest", input))
                Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            await JwtTestSupport.SignInAsync(client, "RECEPTION");
            input.Adults = short.MaxValue;
            using (var invalid = await client.PostAsJsonAsync("/api/reservations/with-guest", input))
            {
                Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
                Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
            }
            await AssertNoWrites(connectionString);
            input.Adults = 1;
            using var created = await client.PostAsJsonAsync("/api/reservations/with-guest", input);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.NotNull(created.Headers.Location);
            using var fetched = await client.GetAsync(created.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        });
    }

    [PostgreSqlFact]
    public async Task Guest_and_reservation_commit_together_through_the_application_pipeline()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            var input = await Input(scope.ServiceProvider);
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateGuestReservationCommand(input));
            await using var verify = Context(connectionString);
            var reservation = await verify.Reservations.AsNoTracking().SingleAsync();
            Assert.Equal(result.ReservationId, reservation.ReservationId);
            Assert.True(await verify.Guests.AnyAsync(g => g.GuestId == result.PrimaryGuestId && g.FirstName == "Atomicidad"));
        });
    }

    [PostgreSqlFact]
    public async Task Reservation_conflict_rolls_back_the_guest_already_flushed_by_another_repository()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            var input = await Input(scope.ServiceProvider);
            input.Adults = short.MaxValue; // Falla la regla de capacidad después de registrar al huésped.
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateGuestReservationCommand(input)));
            await AssertNoWrites(connectionString);
            Assert.Empty(scope.ServiceProvider.GetRequiredService<HotelManagementDbContext>().ChangeTracker.Entries());
        });
    }

    [PostgreSqlFact]
    public async Task Failure_after_both_repository_writes_reverts_rows_and_trigger_effects()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var input = await Input(provider);
            var unit = provider.GetRequiredService<IUnitOfWork>();
            await Assert.ThrowsAsync<InjectedFailure>(() => unit.ExecuteAsync<int>(async ct =>
            {
                var reservation = await provider.GetRequiredService<ISender>()
                    .Send(new CreateGuestReservationCommand(input), ct);
                Assert.True(reservation.ReservationId > 0);
                Assert.True(reservation.PrimaryGuestId > 0);
                // Otro contexto no debe observar escrituras sin commit.
                await AssertNoWrites(connectionString);
                throw new InjectedFailure();
            }));
            await AssertNoWrites(connectionString);
        });
    }

    [PostgreSqlFact]
    public async Task Cancellation_after_a_write_rolls_back_without_reusing_the_cancelled_token()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            using var cancellation = new CancellationTokenSource();
            var provider = scope.ServiceProvider;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.GetRequiredService<IUnitOfWork>().ExecuteAsync(async ct =>
                {
                    var guest = await provider.GetRequiredService<ISender>().Send(new CreateGuestCommand(
                        new CreateGuestDto { FirstName = "Atomicidad", LastName = "Cancelada" }), ct);
                    cancellation.Cancel();
                    return guest;
                }, cancellation.Token));
            await AssertNoWrites(connectionString);
        });
    }

    [PostgreSqlFact]
    public async Task Caught_nested_failure_cannot_commit_partial_data_and_scope_can_be_used_again()
    {
        await InDatabase(async (services, connectionString) =>
        {
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var input = await Input(provider);
            input.Adults = short.MaxValue;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.GetRequiredService<IUnitOfWork>().ExecuteAsync(async ct =>
                {
                    try
                    {
                        await provider.GetRequiredService<ISender>().Send(new CreateGuestReservationCommand(input), ct);
                    }
                    catch (BusinessValidationException) { /* Un consumidor no puede confirmar lo parcial. */ }
                    return 1;
                }));
            await AssertNoWrites(connectionString);
            input.Adults = 1;
            var result = await provider.GetRequiredService<ISender>().Send(new CreateGuestReservationCommand(input));
            await using var verify = Context(connectionString);
            Assert.True(await verify.Reservations.AnyAsync(r => r.ReservationId == result.ReservationId));
        });
    }

    private static async Task<CreateGuestReservationDto> Input(IServiceProvider services)
    {
        var context = services.GetRequiredService<HotelManagementDbContext>();
        var room = await context.Rooms.AsNoTracking().SingleAsync(r => r.RoomNumber == "DEMO-101");
        return new CreateGuestReservationDto
        {
            Guest = new CreateGuestDto { FirstName = "Atomicidad", LastName = "Prueba" },
            RoomId = room.RoomId,
            CheckInDate = new DateOnly(2030, 6, 1),
            CheckOutDate = new DateOnly(2030, 6, 2), Adults = 1
        };
    }

    private static async Task AssertNoWrites(string connectionString)
    {
        await using var verify = Context(connectionString);
        Assert.False(await verify.Guests.AnyAsync(g => g.FirstName == "Atomicidad"));
        Assert.Empty(await verify.Reservations.ToListAsync());
        Assert.Empty(await verify.ReservationStatusHistories.ToListAsync());
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM hotel.room_period_blocks", connection);
        Assert.Equal(0L, (long)(await count.ExecuteScalarAsync())!);
    }

    private static HotelManagementDbContext Context(string connectionString) => new(
        new DbContextOptionsBuilder<HotelManagementDbContext>().UseNpgsql(connectionString).Options);

    private static async Task InDatabase(Func<ServiceProvider, string, Task> test)
    {
        var database = "hotel_uow_" + Guid.NewGuid().ToString("N");
        var adminString = Environment.GetEnvironmentVariable("R02_POSTGRES_ADMIN")!;
        await using var admin = new NpgsqlConnection(adminString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(adminString)
                { Database = database, Pooling = false }.ConnectionString;
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (var file in new[] { "001_initial_schema.sql", "002_demo_seed.sql",
                             "003_room_period_integrity.sql", "004_maintenance_trigger_search_path.sql" })
                {
                    await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(
                        Path.Combine(AppContext.BaseDirectory, "database", file)), connection);
                    await command.ExecuteNonQueryAsync();
                }
            }
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(connectionString);
            services.AddSingleton<HotelManagement.Application.Interfaces.Services.IAccessTokenIssuer, FakeAccessTokenIssuer>();
            await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
                { ValidateScopes = true, ValidateOnBuild = true });
            await test(provider, connectionString);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class InjectedFailure : Exception;
}
