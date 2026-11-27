using System.Security.Claims;
using System.Text;
using HotelManagement.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using HotelManagement.Api.Authorization;
namespace HotelManagement.Api.Configuration;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey debe tener al menos 32 bytes y configurarse fuera del repositorio.")
            .ValidateOnStart();
        services.AddSingleton<JwtTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, settings) =>
            {
                var options = settings.Value;
                bearer.MapInboundClaims = true;
                bearer.IncludeErrorDetails = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.Operations, policy =>
                policy.RequireRole(AppRoles.Reception, AppRoles.Admin));

            options.AddPolicy(AppPolicies.Configuration, policy =>
                policy.RequireRole(AppRoles.Admin));
            options.AddPolicy(AppPolicies.Financial, policy =>
                policy.RequireRole(AppRoles.Cashier, AppRoles.Admin));
            options.AddPolicy(AppPolicies.Maintenance, policy =>
                policy.RequireRole(AppRoles.Maintenance, AppRoles.Admin)
                    .RequireAssertion(context =>
                        long.TryParse(
                            context.User.FindFirst(
                                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                            out var userId) &&
                        userId > 0));
        });
        return services;
    }
}
