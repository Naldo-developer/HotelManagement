using System.Net;
using System.Reflection;
using System.Security.Claims;
using HotelManagement.Api.Authorization;
using HotelManagement.Api.Configuration;
using HotelManagement.Api.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace HotelManagement.Api.Tests;

public class RolePolicyTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("MAINTENANCE", HttpStatusCode.Forbidden)]
    [InlineData("CASHIER", HttpStatusCode.Forbidden)]
    [InlineData("RECEPTION", HttpStatusCode.OK)]
    [InlineData("ADMIN", HttpStatusCode.OK)]
    public async Task Operations_policy_returns_expected_status(
        string? role, HttpStatusCode expected)
    {
        await using var app = await StartApp();
        using var client = await ClientAsync(app, role);

        using var response = await client.GetAsync("/test/operations");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("RECEPTION", HttpStatusCode.Forbidden)]
    [InlineData("ADMIN", HttpStatusCode.OK)]
    public async Task Configuration_policy_returns_expected_status(
        string? role, HttpStatusCode expected)
    {
        await using var app = await StartApp();
        using var client = await ClientAsync(app, role);

        using var response = await client.GetAsync("/test/configuration");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("MAINTENANCE", HttpStatusCode.OK)]
    [InlineData("CASHIER", HttpStatusCode.OK)]
    public async Task Any_session_policy_returns_expected_status(
        string? role, HttpStatusCode expected)
    {
        await using var app = await StartApp();
        using var client = await ClientAsync(app, role);

        using var response = await client.GetAsync("/test/authenticated");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public void Guests_and_reservations_require_operations_policy()
    {
        Assert.Equal(AppPolicies.Operations,
            typeof(GuestsController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        Assert.Equal(AppPolicies.Operations,
            typeof(ReservationsController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }

    [Fact]
    public void Room_writes_require_configuration_policy()
    {
        var create = typeof(RoomsController).GetMethod(nameof(RoomsController.Create))!;
        var update = typeof(RoomsController).GetMethod(nameof(RoomsController.Update))!;

        Assert.Equal(AppPolicies.Configuration,
            create.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        Assert.Equal(AppPolicies.Configuration,
            update.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }

    [Fact]
    public void Every_controller_except_auth_and_health_requires_a_session()
    {
        var controllers = typeof(GuestsController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                           && !type.IsAbstract
                           && type != typeof(AuthController)
                           && type != typeof(HealthController));

        foreach (var controller in controllers)
        {
            Assert.True(
                controller.GetCustomAttribute<AuthorizeAttribute>() is not null,
                $"{controller.Name} no exige sesión.");
        }
    }

    private static async Task<WebApplication> StartApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddApiAuthentication();

        var app = builder.Build();
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

        app.MapGet("/test/operations", () => "ok")
            .RequireAuthorization(AppPolicies.Operations);
        app.MapGet("/test/configuration", () => "ok")
            .RequireAuthorization(AppPolicies.Configuration);
        app.MapGet("/test/authenticated", () => "ok")
            .RequireAuthorization();

        await app.StartAsync();
        return app;
    }

    private static async Task<HttpClient> ClientAsync(WebApplication app, string? role)
    {
        var client = app.GetTestClient();

        if (role is not null)
        {
            using var login = await client.PostAsync($"/test/login/{role}", null);
            Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

            var cookie = login.Headers.GetValues("Set-Cookie").First().Split(';')[0];
            client.DefaultRequestHeaders.Add("Cookie", cookie);
        }

        return client;
    }
}