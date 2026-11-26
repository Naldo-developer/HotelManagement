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
using HotelManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace HotelManagement.Api.Tests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("R02_POSTGRES_ADMIN")))
            Skip = "Definir R02_POSTGRES_ADMIN para ejecutar contra PostgreSQL temporal (ver README).";
    }
}

public sealed class RoomPersistenceTests
{
    [PostgreSqlFact]
    public async Task Rooms_can_be_listed_created_read_and_persisted_and_duplicates_return_409()
    {
        // Solo crea y elimina esta base aleatoria, nunca la base de la conexión recibida.
        var database = "hotel_r02_" + Guid.NewGuid().ToString("N");
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
            Assert.NotNull(rooms);
            Assert.Equal(3, rooms.Count);
            var simple = Assert.Single(rooms, room => room.RoomNumber == "DEMO-101");
            var input = new CreateRoomDto
            {
                HotelId = simple.HotelId,
                RoomTypeId = simple.RoomTypeId,
                RoomNumber = "R02-201",
                Floor = "2",
                OperationalStatus = "ACTIVE"
            };
            using var reception = app.GetTestClient();
            await SignInAsync(reception, "RECEPTION");
            using (var forbidden = await reception.PostAsJsonAsync("/api/rooms", input))
                Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using var created = await client.PostAsJsonAsync("/api/rooms", input);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var room = await created.Content.ReadFromJsonAsync<RoomDto>();
            Assert.NotNull(room);
            Assert.True(room.RoomId > 0);
            Assert.NotNull(created.Headers.Location);
            Assert.EndsWith($"/api/rooms/{room.RoomId}", created.Headers.Location.ToString());

            var fetched = await client.GetFromJsonAsync<RoomDto>(created.Headers.Location);
            Assert.NotNull(fetched);
            Assert.Equal(room.RoomId, fetched.RoomId);
            AssertRoom(fetched, input);

            using var duplicate = await client.PostAsJsonAsync("/api/rooms", input);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
            using var problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
            Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());

            await app.StopAsync();
            // Contexto y conexión independientes de los scopes de cada petición HTTP.
            var options = new DbContextOptionsBuilder<HotelManagementDbContext>().UseNpgsql(connectionString).Options;
            await using var freshContext = new HotelManagementDbContext(options);
            var persisted = await freshContext.Rooms.AsNoTracking().SingleAsync(r => r.RoomId == room.RoomId);
            Assert.Equal(input.HotelId, persisted.HotelId);
            Assert.Equal(input.RoomTypeId, persisted.RoomTypeId);
            Assert.Equal(input.RoomNumber, persisted.RoomNumber);
            Assert.Equal(input.Floor, persisted.Floor);
            Assert.Equal(input.OperationalStatus, persisted.OperationalStatus);
            Assert.Equal(1, await freshContext.Rooms.CountAsync(r => r.HotelId == input.HotelId && r.RoomNumber == input.RoomNumber));
            Assert.Equal(4, await freshContext.Rooms.CountAsync());
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
    private static void AssertRoom(RoomDto room, CreateRoomDto input)
    {
        Assert.Equal(input.HotelId, room.HotelId);
        Assert.Equal(input.RoomTypeId, room.RoomTypeId);
        Assert.Equal(input.RoomNumber, room.RoomNumber);
        Assert.Equal(input.Floor, room.Floor);
        Assert.Equal(input.OperationalStatus, room.OperationalStatus);
    }
}
