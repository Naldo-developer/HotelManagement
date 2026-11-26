using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using HotelManagement.Api.Configuration;
using HotelManagement.Api.Controllers;
using HotelManagement.Application.Configuration;
using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HotelManagement.Api.Tests;

public sealed class CatalogPersistenceTests
{
    [PostgreSqlFact]
    public async Task Catalogs_can_be_created_updated_and_preserve_relationships()
    {
        var database = "hotel_t08_" +
                       Guid.NewGuid().ToString("N");

        var adminString = Environment
            .GetEnvironmentVariable("R02_POSTGRES_ADMIN")!;

        await using var admin =
            new NpgsqlConnection(adminString);

        await admin.OpenAsync();

        await using (var create = new NpgsqlCommand(
                         $"CREATE DATABASE {database}", admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        var connectionString =
            new NpgsqlConnectionStringBuilder(adminString)
            {
                Database = database,
                Pooling = false
            }.ConnectionString;

        try
        {
            await using (var connection =
                         new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var file in new[]
                         {
                             "001_initial_schema.sql",
                             "002_demo_seed.sql",
                             "003_room_period_integrity.sql",
                             "004_maintenance_trigger_search_path.sql"
                         })
                {
                    var sql = await File.ReadAllTextAsync(
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "database",
                            file));

                    await using var command =
                        new NpgsqlCommand(sql, connection);

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

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(HotelsController).Assembly);

            await using var app = builder.Build();

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                SuppressDiagnosticsCallback = _ => true
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/test/login/{role}",
                async (HttpContext context, string role) =>
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, "usuario.prueba"),
                        new(ClaimTypes.Role, role)
                    };

                    var identity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults
                            .AuthenticationScheme);

                    await context.SignInAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return Results.NoContent();
                });

            app.MapControllers();

            await app.StartAsync();

            using var anonymous = app.GetTestClient();
            using var reception = app.GetTestClient();
            using var client = app.GetTestClient();

            await SignInAsync(reception, "RECEPTION");
            await SignInAsync(client, "ADMIN");

            var hotelInput = new CreateHotelDto
            {
                Name = "Hotel Prueba T08",
                TaxId = "20999999991",
                Address = "Arequipa",
                Email = "t08@example.com",
                CurrencyCode = "PEN",
                Timezone = "America/Lima"
            };

            // Sin sesión: 401.
            using var unauthorized =
                await anonymous.PostAsJsonAsync(
                    "/api/hotels", hotelInput);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                unauthorized.StatusCode);

            // Recepción: 403.
            using var forbidden =
                await reception.PostAsJsonAsync(
                    "/api/hotels", hotelInput);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbidden.StatusCode);

            // Hotel creado: 201.
            using var hotelResponse =
                await client.PostAsJsonAsync(
                    "/api/hotels", hotelInput);

            Assert.Equal(
                HttpStatusCode.Created,
                hotelResponse.StatusCode);

            var hotel = await hotelResponse.Content
                .ReadFromJsonAsync<HotelDto>();

            Assert.NotNull(hotel);
            Assert.True(hotel.HotelId > 0);
            Assert.True(hotel.Active);

            // Documento tributario duplicado: 409.
            using var duplicateHotel =
                await client.PostAsJsonAsync(
                    "/api/hotels", hotelInput);

            Assert.Equal(
                HttpStatusCode.Conflict,
                duplicateHotel.StatusCode);

            // Hotel inválido: 400.
            using var invalidHotel =
                await client.PostAsJsonAsync(
                    "/api/hotels",
                    new CreateHotelDto
                    {
                        Name = ""
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                invalidHotel.StatusCode);

            var typeInput = new CreateRoomTypeDto
            {
                HotelId = hotel.HotelId,
                Name = "Suite T08",
                Description = "Tipo de prueba",
                MaxAdults = 2,
                MaxChildren = 1,
                BaseRate = 150m
            };

            using var forbiddenType =
                await reception.PostAsJsonAsync(
                    "/api/room-types", typeInput);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbiddenType.StatusCode);

            // Hotel inexistente: 404.
            using var missingHotel =
                await client.PostAsJsonAsync(
                    "/api/room-types",
                    new CreateRoomTypeDto
                    {
                        HotelId = long.MaxValue,
                        Name = "Tipo inválido",
                        MaxAdults = 2,
                        BaseRate = 100m
                    });

            Assert.Equal(
                HttpStatusCode.NotFound,
                missingHotel.StatusCode);

            // Tipo creado: 201.
            using var typeResponse =
                await client.PostAsJsonAsync(
                    "/api/room-types", typeInput);

            Assert.Equal(
                HttpStatusCode.Created,
                typeResponse.StatusCode);

            var roomType = await typeResponse.Content
                .ReadFromJsonAsync<RoomTypeDto>();

            Assert.NotNull(roomType);
            Assert.True(roomType.RoomTypeId > 0);

            // Tipo duplicado: 409.
            using var duplicateType =
                await client.PostAsJsonAsync(
                    "/api/room-types", typeInput);

            Assert.Equal(
                HttpStatusCode.Conflict,
                duplicateType.StatusCode);

            // Crear habitación asociada.
            using var roomResponse =
                await client.PostAsJsonAsync(
                    "/api/rooms",
                    new CreateRoomDto
                    {
                        HotelId = hotel.HotelId,
                        RoomTypeId = roomType.RoomTypeId,
                        RoomNumber = "T08-101",
                        Floor = "1",
                        OperationalStatus = "ACTIVE"
                    });

            Assert.Equal(
                HttpStatusCode.Created,
                roomResponse.StatusCode);

            var room = await roomResponse.Content
                .ReadFromJsonAsync<RoomDto>();

            Assert.NotNull(room);

            // Crear huésped.
            using var guestResponse =
                await client.PostAsJsonAsync(
                    "/api/guests",
                    new
                    {
                        firstName = "Prueba",
                        lastName = "T08"
                    });

            Assert.Equal(
                HttpStatusCode.Created,
                guestResponse.StatusCode);

            var guest = await guestResponse.Content
                .ReadFromJsonAsync<
                    HotelManagement.Application.DTOs.Guests.GuestDto>();

            Assert.NotNull(guest);

            // Crear reserva asociada.
            var checkIn = DateOnly
                .FromDateTime(DateTime.UtcNow)
                .AddDays(2);

            using var reservationResponse =
                await client.PostAsJsonAsync(
                    "/api/reservations",
                    new CreateReservationDto
                    {
                        RoomId = room.RoomId,
                        PrimaryGuestId = guest.Id,
                        CheckInDate = checkIn,
                        CheckOutDate = checkIn.AddDays(2),
                        Adults = 1
                    });

            Assert.Equal(
                HttpStatusCode.Created,
                reservationResponse.StatusCode);

            var reservation = await reservationResponse.Content
                .ReadFromJsonAsync<ReservationDto>();

            Assert.NotNull(reservation);

            var hotelUpdate = new UpdateHotelDto
            {
                Name = "Hotel T08 Actualizado",
                TaxId = hotelInput.TaxId,
                Address = "Arequipa - Perú",
                Email = hotelInput.Email,
                CurrencyCode = "PEN",
                Timezone = "America/Lima"
            };

            var typeUpdate = new UpdateRoomTypeDto
            {
                Name = "Suite T08 Premium",
                Description = "Tipo actualizado",
                MaxAdults = 3,
                MaxChildren = 1,
                BaseRate = 200m
            };

            // Recepción no puede editar.
            using var forbiddenHotelUpdate =
                await reception.PutAsJsonAsync(
                    $"/api/hotels/{hotel.HotelId}",
                    hotelUpdate);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbiddenHotelUpdate.StatusCode);

            using var forbiddenTypeUpdate =
                await reception.PutAsJsonAsync(
                    $"/api/room-types/{roomType.RoomTypeId}",
                    typeUpdate);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbiddenTypeUpdate.StatusCode);

            // Editar hotel: 200.
            using var updatedHotelResponse =
                await client.PutAsJsonAsync(
                    $"/api/hotels/{hotel.HotelId}",
                    hotelUpdate);

            Assert.Equal(
                HttpStatusCode.OK,
                updatedHotelResponse.StatusCode);

            var updatedHotel =
                await updatedHotelResponse.Content
                    .ReadFromJsonAsync<HotelDto>();

            Assert.NotNull(updatedHotel);
            Assert.Equal(hotel.HotelId, updatedHotel.HotelId);
            Assert.Equal(hotelUpdate.Name, updatedHotel.Name);

            // Editar tipo: 200.
            using var updatedTypeResponse =
                await client.PutAsJsonAsync(
                    $"/api/room-types/{roomType.RoomTypeId}",
                    typeUpdate);

            Assert.Equal(
                HttpStatusCode.OK,
                updatedTypeResponse.StatusCode);

            var updatedType =
                await updatedTypeResponse.Content
                    .ReadFromJsonAsync<RoomTypeDto>();

            Assert.NotNull(updatedType);
            Assert.Equal(
                roomType.RoomTypeId,
                updatedType.RoomTypeId);

            Assert.Equal(
                hotel.HotelId,
                updatedType.HotelId);

            Assert.Equal(200m, updatedType.BaseRate);

            // Consultas compatibles con recepción.
            var hotels = await reception
                .GetFromJsonAsync<List<HotelDto>>(
                    "/api/hotels");

            Assert.Contains(hotels!,
                h => h.HotelId == hotel.HotelId &&
                     h.Name == hotelUpdate.Name);

            var types = await reception
                .GetFromJsonAsync<List<RoomTypeDto>>(
                    $"/api/room-types?hotelId={hotel.HotelId}");

            Assert.Contains(types!,
                t => t.RoomTypeId == roomType.RoomTypeId &&
                     t.Name == typeUpdate.Name);

            await app.StopAsync();

            // Verificar persistencia desde un contexto nuevo.
            var options =
                new DbContextOptionsBuilder<
                        HotelManagementDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

            await using var fresh =
                new HotelManagementDbContext(options);

            var persistedHotel = await fresh.Hotels
                .AsNoTracking()
                .SingleAsync(
                    h => h.HotelId == hotel.HotelId);

            var persistedType = await fresh.RoomTypes
                .AsNoTracking()
                .SingleAsync(
                    t => t.RoomTypeId == roomType.RoomTypeId);

            var persistedRoom = await fresh.Rooms
                .AsNoTracking()
                .SingleAsync(
                    r => r.RoomId == room.RoomId);

            var persistedReservation =
                await fresh.Reservations
                    .AsNoTracking()
                    .SingleAsync(
                        r => r.ReservationId ==
                             reservation.ReservationId);

            Assert.Equal(
                hotelUpdate.Name,
                persistedHotel.Name);

            Assert.Equal(
                typeUpdate.Name,
                persistedType.Name);

            Assert.Equal(
                hotel.HotelId,
                persistedRoom.HotelId);

            Assert.Equal(
                roomType.RoomTypeId,
                persistedRoom.RoomTypeId);

            Assert.Equal(
                room.RoomId,
                persistedReservation.RoomId);

            Assert.Equal(
                reservation.NightlyRate,
                persistedReservation.NightlyRate);
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE {database} WITH (FORCE)",
                admin);

            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task SignInAsync(
        HttpClient client,
        string role)
    {
        using var login = await client.PostAsync(
            $"/test/login/{role}", null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            login.StatusCode);

        var cookie = login.Headers
            .GetValues("Set-Cookie")
            .First()
            .Split(';')[0];

        client.DefaultRequestHeaders.Add(
            "Cookie", cookie);
    }
}