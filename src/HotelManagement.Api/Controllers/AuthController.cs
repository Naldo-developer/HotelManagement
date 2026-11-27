using System.Globalization;
using System.Security.Claims;
using HotelManagement.Api.Security;
using HotelManagement.Application.Dtos.Auth;
using HotelManagement.Application.UseCases.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender, JwtTokenIssuer tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserDto>> Login([FromBody] LoginDto input)
    {
        var user = await sender.Send(new LoginCommand(input), HttpContext.RequestAborted);

        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas.",
                detail: "Usuario o contraseña incorrectos.");
        }

        var issued = tokens.Issue(user);
        user.AccessToken = issued.AccessToken;
        user.TokenType = "Bearer";
        user.ExpiresAtUtc = issued.ExpiresAtUtc;
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public Task<IActionResult> Logout()
    {
        // JWT de acceso sin estado: el cliente descarta el token. No se revoca
        // una copia previamente emitida; su validez termina con exp.
        return Task.FromResult<IActionResult>(NoContent());
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthenticatedUserDto> Me()
    {
        return Ok(new AuthenticatedUserDto
        {
            UserId = long.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture),
            Username = User.Identity!.Name!,
            FullName = User.FindFirstValue("full_name") ?? string.Empty,
            Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
        });
    }
}
