using Microsoft.AspNetCore.Authentication.Cookies;
using HotelManagement.Api.Authorization;
namespace HotelManagement.Api.Configuration;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "hotel_session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // Una API devuelve códigos HTTP; no redirige a una página de login.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.Operations, policy =>
                policy.RequireRole(AppRoles.Reception, AppRoles.Admin));

            options.AddPolicy(AppPolicies.Configuration, policy =>
                policy.RequireRole(AppRoles.Admin));
        });
        return services;
    }
}