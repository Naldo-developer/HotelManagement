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
    
    public Task<List<Room>> GetAvailableAsync(
        long hotelId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        long? roomTypeId,
        CancellationToken cancellationToken)
    {
        var rooms = context.Rooms
            .AsNoTracking()
            .Where(room => room.HotelId == hotelId &&
                           room.OperationalStatus == "ACTIVE");

        if (roomTypeId.HasValue)
        {
            rooms = rooms.Where(room => room.RoomTypeId == roomTypeId.Value);
        }

        return rooms
            .Where(room => !room.Reservations.Any(reservation =>
                (reservation.Status == "PENDING" ||
                 reservation.Status == "CONFIRMED" ||
                 reservation.Status == "CHECKED_IN") &&
                reservation.CheckInDate < checkOutDate &&
                reservation.CheckOutDate > checkInDate))
            .OrderBy(room => room.RoomNumber)
            .ThenBy(room => room.RoomId)
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
        CancellationToken cancellationToken,
        long? excludeRoomId = null)
    {
        var rooms = context.Rooms.Where(room =>
            room.HotelId == hotelId && room.RoomNumber == roomNumber);

        if (excludeRoomId.HasValue)
        {
            rooms = rooms.Where(room => room.RoomId != excludeRoomId.Value);
        }

        return rooms.AnyAsync(cancellationToken);
    }

    public async Task<Room> CreateAsync(Room room, CancellationToken cancellationToken)
    {
        context.Rooms.Add(room);
        await SaveRoomChangesAsync(cancellationToken);
        return room;
    }

    public async Task UpdateAsync(Room room, CancellationToken cancellationToken)
    {
        var entry = context.Rooms.Attach(room);

        entry.Property(r => r.HotelId).IsModified = true;
        entry.Property(r => r.RoomTypeId).IsModified = true;
        entry.Property(r => r.RoomNumber).IsModified = true;
        entry.Property(r => r.Floor).IsModified = true;

        try
        {
            await SaveRoomChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ResourceNotFoundException("La habitación ya no existe.");
        }
    }

    private async Task SaveRoomChangesAsync(CancellationToken cancellationToken)
    {
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
    }
}