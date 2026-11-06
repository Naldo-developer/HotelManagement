using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Application.Configuration;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ApplicationServicesExtensions).Assembly));

        return services;
    }
}
