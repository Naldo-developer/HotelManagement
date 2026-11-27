using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class RoomTypeRepository(HotelManagementDbContext context)
    : IRoomTypeRepository
{
    public async Task<IReadOnlyList<RoomType>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return await context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.HotelId == hotelId)
            .OrderBy(roomType => roomType.Name)
            .ThenBy(roomType => roomType.RoomTypeId)
            .ToListAsync(cancellationToken);
    }

    public Task<RoomType?> GetByIdAsync(
        long roomTypeId,
        CancellationToken cancellationToken)
    {
        return context.RoomTypes
            .SingleOrDefaultAsync(
                roomType => roomType.RoomTypeId == roomTypeId,
                cancellationToken);
    }

    public async Task<RoomType> CreateAsync(
        RoomType roomType,
        CancellationToken cancellationToken)
    {
        context.RoomTypes.Add(roomType);

        await SaveAsync(cancellationToken);

        return roomType;
    }

    public async Task<RoomType> UpdateAsync(
        RoomType roomType,
        CancellationToken cancellationToken)
    {
        await SaveAsync(cancellationToken);

        return roomType;
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
                ConstraintName: "uq_room_types_hotel_name"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un tipo de habitación con ese nombre en el hotel.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation,
                ConstraintName: "fk_room_types_hotel"
            })
        {
            throw new ResourceNotFoundException(
                "El hotel indicado no existe.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException(
                "El tipo de habitación fue modificado o eliminado durante la operación.");
        }
    }
}