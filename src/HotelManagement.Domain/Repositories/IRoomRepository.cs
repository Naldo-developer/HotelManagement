using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(
        long roomId,
        CancellationToken cancellationToken);

    Task<List<Room>> GetAllAsync(
        long? hotelId,
        long? roomTypeId,
        string? operationalStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}