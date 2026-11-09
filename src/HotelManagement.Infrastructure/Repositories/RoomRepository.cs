using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class RoomRepository(HotelManagementDbContext context) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(
        long roomId,
        CancellationToken cancellationToken)
    {
        return context.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(room => room.RoomId == roomId, cancellationToken);
    }

    public Task<List<Room>> GetAllAsync(
        long? hotelId,
        long? roomTypeId,
        string? operationalStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rooms = context.Rooms.AsNoTracking();

        if (hotelId.HasValue)
        {
            rooms = rooms.Where(room => room.HotelId == hotelId.Value);
        }

        if (roomTypeId.HasValue)
        {
            rooms = rooms.Where(room => room.RoomTypeId == roomTypeId.Value);
        }

        if (operationalStatus is not null)
        {
            rooms = rooms.Where(room => room.OperationalStatus == operationalStatus);
        }

        int offset = checked((page - 1) * pageSize);

        return rooms
            .OrderBy(room => room.RoomId)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}