using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelManagement.Application.Dtos.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelManagement.Infrastructure.Services;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> settings)
    : HotelManagement.Application.Interfaces.Services.IAccessTokenIssuer
{
    public AccessTokenDto Issue(AuthenticatedUserDto user)
    {
        var options = settings.Value;
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("full_name", user.FullName)
        };
        claims.AddRange(user.Roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(
            issuer: options.Issuer, audience: options.Audience, claims: claims,
            notBefore: now, expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new AccessTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
