using System.Globalization;
using System.Security.Claims;
using HotelManagement.Application.Dtos.Auth;
using HotelManagement.Application.UseCases.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Username),
            new("full_name", user.FullName)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
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