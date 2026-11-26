using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotelManagement.Application.Security;
using HotelManagement.Infrastructure.Persistence;
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
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IStayRepository, StayRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IConsumptionRepository, ConsumptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();

        return services;
    }
}