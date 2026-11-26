using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Dtos.Stays;
using System.Net;
using System.Net.Http.Json;
using HotelManagement.Infrastructure.Security;
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
    public async Task Complete_cycle_rejects_reservation_race_repeated_check_in_and_unpaid_checkout()
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
                foreach (var file in new[] { "001_initial_schema.sql", "002_demo_seed.sql", "003_room_period_integrity.sql", "004_maintenance_trigger_search_path.sql" })
                {
                    var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", file));
                    await using var command = new NpgsqlCommand(sql, connection);
                    await command.ExecuteNonQueryAsync();
                }
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
            app.MapControllers();
            var password = Guid.NewGuid().ToString("N");
            await app.Services.SeedDemoAccountsAsync(password);
            await app.StartAsync();
            using var anonymous = app.GetTestClient();
            using (var unauthorized = await anonymous.GetAsync("/api/rooms"))
                Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            using var client = app.GetTestClient();
            using (var invalid = await client.PostAsJsonAsync("/api/auth/login",
                new { username = "admin.demo", password = "incorrecta" }))
                Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
            using (var login = await client.PostAsJsonAsync("/api/auth/login",
                new { username = "admin.demo", password }))
            {
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                var cookie = login.Headers.GetValues("Set-Cookie").First().Split(';')[0];
                client.DefaultRequestHeaders.Add("Cookie", cookie);
            }
            using (var me = await client.GetAsync("/api/auth/me"))
                Assert.Equal(HttpStatusCode.OK, me.StatusCode);

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

                using var serviceResponse = await client.PostAsJsonAsync("/api/services",
                    new { hotelId = room.HotelId, name = "Servicio ficticio R05", defaultPrice = 15m, unit = "UNIT" });
                Assert.Equal(HttpStatusCode.Created, serviceResponse.StatusCode);
                var service = await serviceResponse.Content.ReadFromJsonAsync<HotelManagement.Application.Dtos.Services.ServiceDto>();
                using var consumption = await client.PostAsJsonAsync("/api/consumptions",
                    new { stayId = stay.StayId, serviceId = service!.ServiceId, quantity = 2m });
                Assert.Equal(HttpStatusCode.Created, consumption.StatusCode);
                var summaryUrl = $"/api/payments/summary/by-reservation/{reservation.ReservationId}";
                var summary = await client.GetFromJsonAsync<HotelManagement.Application.Dtos.Payments.PaymentSummaryDto>(summaryUrl);
                Assert.Equal(30m, summary!.ConsumptionTotal);
                Assert.True(summary.BalanceDue > 0);
                using var unpaid = await client.PostAsJsonAsync("/api/stays/check-out", new { stayId = stay.StayId });
                Assert.Equal(HttpStatusCode.Conflict, unpaid.StatusCode);
                var stillActive = await client.GetFromJsonAsync<StayDto>(checkIn.Headers.Location);
                Assert.Equal("ACTIVE", stillActive!.Status);
                Assert.Null(stillActive.CheckOutAt);
                using var payment = await client.PostAsJsonAsync("/api/payments",
                    new { reservationId = reservation.ReservationId, amount = summary.BalanceDue, paymentMethod = "CASH" });
                Assert.Equal(HttpStatusCode.Created, payment.StatusCode);
                var paid = await client.GetFromJsonAsync<HotelManagement.Application.Dtos.Payments.PaymentSummaryDto>(summaryUrl);
                Assert.Equal(0m, paid!.BalanceDue);
                using var checkout = await client.PostAsJsonAsync("/api/stays/check-out", new { stayId = stay.StayId });
                Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
                using var repeatedCheckout = await client.PostAsJsonAsync("/api/stays/check-out", new { stayId = stay.StayId });
                Assert.Equal(HttpStatusCode.Conflict, repeatedCheckout.StatusCode);
                using var operational = await client.PatchAsJsonAsync($"/api/rooms/{room.RoomId}/operational-status",
                    new { operationalStatus = "OUT_OF_SERVICE" });
                Assert.Equal(HttpStatusCode.OK, operational.StatusCode);
                var updatedRoom = await client.GetFromJsonAsync<RoomDto>($"/api/rooms/{room.RoomId}");
                Assert.Equal("OUT_OF_SERVICE", updatedRoom!.OperationalStatus);
                await app.StopAsync();
                var options = new DbContextOptionsBuilder<HotelManagementDbContext>()
                    .UseNpgsql(connectionString).Options;
                await using var fresh = new HotelManagementDbContext(options);
                Assert.Equal(1, await fresh.Reservations.CountAsync(r => r.RoomId == room.RoomId));
                var persisted = await fresh.Reservations.AsNoTracking().SingleAsync();
                Assert.Equal("COMPLETED", persisted.Status);
                Assert.Equal(guestId, persisted.PrimaryGuestId);
                var persistedStay = await fresh.Stays.AsNoTracking().SingleAsync();
                Assert.Equal(reservation.ReservationId, persistedStay.ReservationId);
                Assert.Equal("COMPLETED", persistedStay.Status);
                Assert.NotNull(persistedStay.CheckOutAt);
                Assert.Equal(1, await fresh.Consumptions.CountAsync());
                Assert.Equal(1, await fresh.Payments.CountAsync());
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

}
