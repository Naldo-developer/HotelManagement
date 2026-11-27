using HotelManagement.Application.Dtos.Auth;

namespace HotelManagement.Application.Interfaces.Services;

/// <summary>Puerto de emisión de credenciales; Application no conoce JWT ni ASP.NET.</summary>
public interface IAccessTokenIssuer
{
    AccessTokenDto Issue(AuthenticatedUserDto user);
}
