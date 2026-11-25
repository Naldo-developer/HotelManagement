using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IConsumptionRepository
{
    Task<Consumption> CreateAsync(
        long stayId,
        long serviceId,
        decimal quantity,
        long userId,
        string? notes,
        CancellationToken cancellationToken);

    Task<List<Consumption>> GetByStayIdAsync(
        long stayId,
        CancellationToken cancellationToken);
}