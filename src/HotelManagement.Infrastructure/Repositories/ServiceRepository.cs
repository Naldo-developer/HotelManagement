using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class ServiceRepository(HotelManagementDbContext context)
    : IServiceRepository
{
    public Task<List<Service>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return context.Services
            .AsNoTracking()
            .Where(service =>
                service.HotelId == hotelId &&
                service.Active)
            .OrderBy(service => service.Name)
            .ThenBy(service => service.ServiceId)
            .ToListAsync(cancellationToken);
    }

    public Task<Service?> GetByIdAsync(
        long serviceId,
        CancellationToken cancellationToken)
    {
        return context.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(
                service => service.ServiceId == serviceId,
                cancellationToken);
    }
}