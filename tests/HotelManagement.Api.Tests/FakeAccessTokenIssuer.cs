using HotelManagement.Application.Dtos.Auth;
using HotelManagement.Application.Interfaces.Services;

namespace HotelManagement.Api.Tests;

// Solo aísla el caso de uso. JwtAuthenticationTests y el ciclo HTTP usan el emisor real.
internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public AccessTokenDto Issue(AuthenticatedUserDto user) =>
        new("token-exclusivo-de-prueba", DateTimeOffset.UtcNow.AddMinutes(15));
}
