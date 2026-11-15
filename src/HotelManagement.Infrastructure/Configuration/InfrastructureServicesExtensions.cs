using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotelManagement.Application.Security;
using HotelManagement.Infrastructure.Security;

namespace HotelManagement.Infrastructure.Configuration;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<HotelManagementDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<ICredentialHasher, CredentialHasher>();
        services.AddScoped<IRoomRepository, RoomRepository>();

        return services;
    }
}