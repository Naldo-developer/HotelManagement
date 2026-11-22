using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Dtos.Stays;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HotelManagement.Api.Configuration;
using HotelManagement.Api.Controllers;
using HotelManagement.Application.Configuration;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace HotelManagement.Api.Tests;

public sealed class HotelCycleHttpTests
{
    [PostgreSqlFact]
    public async Task Guest_reservation_race_and_repeated_check_in_persist_through_HTTP()
    {
        // Solo crea y elimina la base aleatoria de esta prueba.
        var database = "hotel_r05_" + Guid.NewGuid().ToString("N");
        var adminString = Environment.GetEnvironmentVariable("R02_POSTGRES_ADMIN")!;
        await using var admin = new NpgsqlConnection(adminString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin))
            await create.ExecuteNonQueryAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(adminString)
        {
            Database = database,
            Pooling = false
        }.ConnectionString;
        try
        {
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (var file in new[] { "001_initial_schema.sql", "002_demo_seed.sql", "003_room_period_integrity.sql" })
                {
                    var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", file));
                    await using var command = new NpgsqlCommand(sql, connection);
                    await command.ExecuteNonQueryAsync();
                }
            }

            await using (var seed = new NpgsqlConnection(connectionString))
            {
                await seed.OpenAsync();
                await using var command = new NpgsqlCommand("""
                    UPDATE hotel.hotels SET timezone = 'UTC';
                    INSERT INTO hotel.employees (hotel_id, first_name, last_name, active)
                    SELECT min(hotel_id), 'Operador', 'R05', true FROM hotel.hotels;
                    INSERT INTO hotel.app_users (employee_id, username, password_hash, active)
                    SELECT min(employee_id), 'r05.test', 'NOT-A-LOGIN-HASH', true FROM hotel.employees;
                    """, seed);
                await command.ExecuteNonQueryAsync();
            }

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddInfrastructure(connectionString);
            builder.Services.AddApplication();
            builder.Services.AddApiErrorHandling();
            builder.Services.AddApiAuthentication();
            builder.Services.AddControllers().AddApplicationPart(typeof(RoomsController).Assembly);
            await using var app = builder.Build();
            app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapPost("/test/login/{role}", async (HttpContext context, string role) =>
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, "usuario.prueba"),
                    new(ClaimTypes.NameIdentifier, "1"),
                    new(ClaimTypes.Role, role)
                };
                var identity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return Results.NoContent();
            });
            app.MapControllers();
            await app.StartAsync();
            using var anonymous = app.GetTestClient();
            using (var unauthorized = await anonymous.GetAsync("/api/rooms"))
                Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            using var client = app.GetTestClient();
            await SignInAsync(client, "ADMIN");

            var rooms = await client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");
            var room = Assert.Single(rooms!, r => r.RoomNumber == "DEMO-101");
            using var guestResponse = await client.PostAsJsonAsync("/api/guests",
                new { firstName = "Prueba", lastName = "R05" });
            Assert.Equal(HttpStatusCode.Created, guestResponse.StatusCode);
            var guest = await guestResponse.Content.ReadFromJsonAsync<HotelManagement.Application.DTOs.Guests.GuestDto>();
            Assert.NotNull(guest);
            var guestId = guest.Id;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var input = new CreateReservationDto
            {
                RoomId = room.RoomId, PrimaryGuestId = guestId,
                CheckInDate = today, CheckOutDate = today.AddDays(1), Adults = 1
            };
            var responses = await Task.WhenAll(
                client.PostAsJsonAsync("/api/reservations", input),
                client.PostAsJsonAsync("/api/reservations", input));
            try
            {
                var created = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
                var conflict = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
                Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
                var reservation = await created.Content.ReadFromJsonAsync<ReservationDto>();
                Assert.NotNull(reservation);
                using var checkIn = await client.PostAsJsonAsync("/api/stays/check-in",
                    new { reservationId = reservation.ReservationId });
                Assert.Equal(HttpStatusCode.Created, checkIn.StatusCode);
                var stay = await checkIn.Content.ReadFromJsonAsync<StayDto>();
                Assert.NotNull(stay);
                var fetched = await client.GetFromJsonAsync<StayDto>(checkIn.Headers.Location);
                Assert.Equal(stay.StayId, fetched!.StayId);
                using var repeated = await client.PostAsJsonAsync("/api/stays/check-in",
                    new { reservationId = reservation.ReservationId });
                Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);

                await app.StopAsync();
                var options = new DbContextOptionsBuilder<HotelManagementDbContext>()
                    .UseNpgsql(connectionString).Options;
                await using var fresh = new HotelManagementDbContext(options);
                Assert.Equal(1, await fresh.Reservations.CountAsync(r => r.RoomId == room.RoomId));
                var persisted = await fresh.Reservations.AsNoTracking().SingleAsync();
                Assert.Equal("CHECKED_IN", persisted.Status);
                Assert.Equal(guestId, persisted.PrimaryGuestId);
                var persistedStay = await fresh.Stays.AsNoTracking().SingleAsync();
                Assert.Equal(reservation.ReservationId, persistedStay.ReservationId);
                Assert.Equal("ACTIVE", persistedStay.Status);
                Assert.Null(persistedStay.CheckOutAt);
            }
            finally
            {
                foreach (var response in responses) response.Dispose();
            }
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task SignInAsync(HttpClient client, string role)
    {
        using var login = await client.PostAsync($"/test/login/{role}", null);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").First().Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
    }
}