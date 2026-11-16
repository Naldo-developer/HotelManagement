using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class HotelRepository(HotelManagementDbContext context) : IHotelRepository
{
    public async Task<IReadOnlyList<Hotel>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await context.Hotels
            .AsNoTracking()
            .OrderBy(hotel => hotel.Name)
            .ThenBy(hotel => hotel.HotelId)
            .ToListAsync(cancellationToken);
    }
}