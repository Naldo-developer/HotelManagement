using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class HotelRepository(HotelManagementDbContext context)
    : IHotelRepository
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

    public Task<Hotel?> GetByIdAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return context.Hotels
            .SingleOrDefaultAsync(
                hotel => hotel.HotelId == hotelId,
                cancellationToken);
    }

    public async Task<Hotel> CreateAsync(
        Hotel hotel,
        CancellationToken cancellationToken)
    {
        context.Hotels.Add(hotel);

        await SaveAsync(cancellationToken);

        return hotel;
    }

    public async Task<Hotel> UpdateAsync(
        Hotel hotel,
        CancellationToken cancellationToken)
    {
        await SaveAsync(cancellationToken);

        return hotel;
    }

    private async Task SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_hotels_tax_id"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un hotel con ese documento tributario.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException(
                "El hotel fue modificado o eliminado durante la operación.");
        }
    }
}