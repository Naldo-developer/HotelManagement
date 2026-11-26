using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IRoomTypeRepository
{
    Task<IReadOnlyList<RoomType>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken);

    Task<RoomType?> GetByIdAsync(
        long roomTypeId,
        CancellationToken cancellationToken);

    Task<RoomType> CreateAsync(
        RoomType roomType,
        CancellationToken cancellationToken);

    Task<RoomType> UpdateAsync(
        RoomType roomType,
        CancellationToken cancellationToken);
}