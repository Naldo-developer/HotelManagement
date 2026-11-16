using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class RoomTypeRepository(HotelManagementDbContext context) : IRoomTypeRepository
{
    public async Task<IReadOnlyList<RoomType>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return await context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.HotelId == hotelId)
            .OrderBy(roomType => roomType.Name)
            .ToListAsync(cancellationToken);
    }
}