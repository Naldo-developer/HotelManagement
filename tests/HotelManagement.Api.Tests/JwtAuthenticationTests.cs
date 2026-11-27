using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using HotelManagement.Api.Authorization;
using HotelManagement.Api.Configuration;
using HotelManagement.Api.Controllers;
using HotelManagement.Infrastructure.Services;
using HotelManagement.Application;
using HotelManagement.Application.Dtos.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelManagement.Api.Tests;

public sealed class JwtAuthenticationTests
{
    [Fact]
    public async Task Signed_token_exposes_identity_and_roles_without_a_cookie()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        var issued = app.Services.GetRequiredService<JwtTokenIssuer>().Issue(User());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var identity = await response.Content.ReadFromJsonAsync<AuthenticatedUserDto>();
        Assert.Equal(42, identity!.UserId);
        Assert.Equal("jwt.test", identity.Username);
        Assert.Equal("Usuario JWT", identity.FullName);
        Assert.Contains("ADMIN", identity.Roles);
        Assert.Null(identity.AccessToken);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/test/admin")).StatusCode);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("wrong-signature")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("missing-expiration")]
    [InlineData("unsigned")]
    [InlineData("wrong-algorithm")]
    [InlineData("tampered")]
    [InlineData("invalid-subject")]
    [InlineData("zero-subject")]
    [InlineData("negative-subject")]
    [InlineData("empty-subject")]
    public async Task Invalid_tokens_are_rejected(string scenario)
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        var options = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var now = DateTime.UtcNow;
        var key = scenario == "wrong-signature" ? new string('x', 64) : options.SigningKey;
        var algorithm = scenario == "wrong-algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256;
        var token = new JwtSecurityToken(
            issuer: scenario == "wrong-issuer" ? "other-issuer" : options.Issuer,
            audience: scenario == "wrong-audience" ? "other-audience" : options.Audience,
            claims: [new Claim("sub", scenario switch
            {
                "invalid-subject" => "usuario",
                "zero-subject" => "0",
                "negative-subject" => "-1",
                "empty-subject" => "",
                _ => "42"
            }), new Claim("unique_name", "jwt.test"), new Claim("role", "ADMIN")],
            notBefore: scenario == "expired" ? now.AddMinutes(-5) : scenario == "future" ? now.AddMinutes(5) : now.AddMinutes(-1),
            expires: scenario == "missing-expiration" ? null : scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(15),
            signingCredentials: scenario == "unsigned" ? null : new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm));
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (scenario == "tampered")
        {
            var parts = encoded.Split('.');
            parts[1] = Base64UrlEncoder.Encode(Base64UrlEncoder.Decode(parts[1]).Replace("jwt.test", "attacker"));
            encoded = string.Join('.', parts);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", encoded);
        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_and_missing_bearer_are_not_accepted_and_wrong_role_returns_403()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        var issued = app.Services.GetRequiredService<JwtTokenIssuer>().Issue(User("CASHIER"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", "hotel_session=" + issued.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/test/admin")).StatusCode);
    }

    [Fact]
    public async Task Logout_acknowledges_but_does_not_claim_server_side_revocation()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();
        var issued = app.Services.GetRequiredService<JwtTokenIssuer>().Issue(User());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("demasiado-corta")]
    public async Task Missing_or_short_signing_key_prevents_startup(string key)
    {
        var builder = Builder();
        builder.Configuration["Jwt:SigningKey"] = key;
        await using var app = builder.Build();
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }

    private static AuthenticatedUserDto User(string role = "ADMIN") => new()
    {
        UserId = 42, Username = "jwt.test", FullName = "Usuario JWT", Roles = [role]
    };

    private static WebApplicationBuilder Builder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        JwtTestSupport.Configure(builder);
        builder.Services.AddApplication();
        builder.Services.AddApiAuthentication();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        return builder;
    }

    private static async Task<WebApplication> StartApp()
    {
        var app = Builder().Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/test/admin", () => "ok").RequireAuthorization(AppPolicies.Configuration);
        await app.StartAsync();
        return app;
    }
}
