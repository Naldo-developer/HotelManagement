using Microsoft.OpenApi;

namespace HotelManagement.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddApiSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "HotelManagement.Api",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Inicia sesión en /api/auth/login y pega solo accessToken."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });

            options.OperationFilter<PublicEndpointOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "HotelManagement.Api v1"));

        return app;
    }
}