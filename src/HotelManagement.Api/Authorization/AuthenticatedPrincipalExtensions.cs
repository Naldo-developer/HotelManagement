using System.Globalization;
using System.Security.Claims;
using HotelManagement.Application.Dtos.Auth;

namespace HotelManagement.Api.Authorization;

/// <summary>Adapta claims HTTP ya validados; las reglas siguen en los casos de uso.</summary>
internal static class AuthenticatedPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal) =>
        long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    public static AuthenticatedUserDto ToIdentity(this ClaimsPrincipal principal) => new()
    {
        UserId = principal.GetUserId(),
        Username = principal.Identity!.Name!,
        FullName = principal.FindFirstValue("full_name") ?? string.Empty,
        Roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
    };
}
