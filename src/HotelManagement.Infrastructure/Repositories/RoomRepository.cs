using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public Task<bool> HotelExistsAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return context.Hotels.AnyAsync(
            hotel => hotel.HotelId == hotelId, cancellationToken);
    }

    public Task<bool> RoomTypeBelongsToHotelAsync(
        long roomTypeId,
        long hotelId,
        CancellationToken cancellationToken)
    {
        return context.RoomTypes.AnyAsync(
            type => type.RoomTypeId == roomTypeId && type.HotelId == hotelId,
            cancellationToken);
    }

    public Task<bool> RoomNumberExistsAsync(
        long hotelId,
        string roomNumber,
        CancellationToken cancellationToken)
    {
        return context.Rooms.AnyAsync(
            room => room.HotelId == hotelId && room.RoomNumber == roomNumber,
            cancellationToken);
    }

    public async Task<Room> CreateAsync(Room room, CancellationToken cancellationToken)
    {
        context.Rooms.Add(room);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation,
                 ConstraintName: "uq_rooms_hotel_number" })
        {
            throw new BusinessConflictException(
                "Ya existe una habitación con ese número en el hotel.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.ForeignKeyViolation,
                 ConstraintName: "fk_rooms_room_type_hotel" })
        {
            throw new BusinessValidationException(
                "El tipo de habitación debe existir y pertenecer al hotel indicado.");
        }

        return room;
    }
}