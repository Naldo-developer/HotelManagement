using HotelManagement.Application.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Authorization;

/// <summary>Traducción del resultado del caso de uso al protocolo HTTP.</summary>
internal static class AuthenticationHttpResponse
{
    public static ActionResult<AuthenticatedUserDto> FromLogin(
        ControllerBase controller, AuthenticatedUserDto? identity)
    {
        controller.Response.Headers.CacheControl = "no-store";
        controller.Response.Headers.Pragma = "no-cache";
        return identity is null
            ? controller.Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas.", detail: "Usuario o contraseña incorrectos.")
            : controller.Ok(identity);
    }
}
