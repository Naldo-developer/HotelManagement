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
    Task<bool> HotelExistsAsync(long hotelId, CancellationToken cancellationToken);

    Task<bool> RoomTypeBelongsToHotelAsync(
        long roomTypeId, long hotelId, CancellationToken cancellationToken);

    Task<bool> RoomNumberExistsAsync(
        long hotelId, string roomNumber, CancellationToken cancellationToken);

    Task<Room> CreateAsync(Room room, CancellationToken cancellationToken);
}