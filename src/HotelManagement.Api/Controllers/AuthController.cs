using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Auth;
using HotelManagement.Application.UseCases.Auth.Commands;
using MediatR;
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
        return AuthenticationHttpResponse.FromLogin(this, user);
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
        return Ok(User.ToIdentity());
    }
}
