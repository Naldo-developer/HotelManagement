using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IServiceRepository
{
    Task<List<Service>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken);

    Task<Service?> GetByIdAsync(
        long serviceId,
        CancellationToken cancellationToken);
    Task<Service> CreateAsync(
        Service service,
        CancellationToken cancellationToken);

    Task<Service> UpdateAsync(
        Service service,
        CancellationToken cancellationToken);
}