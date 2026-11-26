using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IMaintenanceRepository
{
    Task<List<Maintenance>> GetAllAsync(
        long? roomId,
        string? status,
        CancellationToken cancellationToken);

    Task<Maintenance> CreateAsync(
        Maintenance maintenance,
        CancellationToken cancellationToken);

    Task<Maintenance> CloseAsync(
        long maintenanceId,
        decimal cost,
        CancellationToken cancellationToken);
}