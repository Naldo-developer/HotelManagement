using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IRoomTypeRepository
{
    Task<IReadOnlyList<RoomType>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken);
}