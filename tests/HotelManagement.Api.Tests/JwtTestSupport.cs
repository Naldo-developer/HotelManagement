using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HotelManagement.Api.Security;
using HotelManagement.Application.Dtos.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HotelManagement.Api.Tests;

internal static class JwtTestSupport
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Configuration["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        builder.Configuration["Jwt:Issuer"] = "HotelManagement.Tests";
        builder.Configuration["Jwt:Audience"] = "HotelManagement.Tests.Client";
    }

    // Este emisor existe exclusivamente en TestServer, nunca en Program.cs.
    public static void MapLogin(WebApplication app) => app.MapPost("/test/login/{role}",
        (string role, JwtTokenIssuer issuer) => Results.Ok(issuer.Issue(new AuthenticatedUserDto
        {
            UserId = 1, Username = "usuario.prueba", FullName = "Usuario Prueba", Roles = [role]
        })));

    public static async Task SignInAsync(HttpClient client, string role)
    {
        using var response = await client.PostAsync($"/test/login/{role}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<IssuedJwt>();
        Assert.NotNull(token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}
