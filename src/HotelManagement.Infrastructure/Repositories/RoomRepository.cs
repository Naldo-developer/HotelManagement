using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

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
            .Where(room => !room.Maintenances.Any(maintenance =>
                (maintenance.Status == "SCHEDULED" || maintenance.Status == "IN_PROGRESS") &&
                maintenance.StartDate < checkOutDate &&
                maintenance.EndDate > checkInDate))
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

    public async Task UpdateAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // T05 y C05 también bloquean esta habitación.
        // Leer los valores actuales después de obtener el bloqueo.
        var rooms = await context.Rooms
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM hotel.rooms
                                  WHERE room_id = {room.RoomId}
                                  FOR UPDATE
                                  """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var currentRoom = rooms.SingleOrDefault();

        if (currentRoom is null)
        {
            throw new ResourceNotFoundException(
                "La habitación ya no existe.");
        }

        var changesHotelOrType =
            currentRoom.HotelId != room.HotelId ||
            currentRoom.RoomTypeId != room.RoomTypeId;

        if (changesHotelOrType)
        {
            var hasReservations = await context.Reservations.AnyAsync(
                reservation => reservation.RoomId == room.RoomId,
                cancellationToken);

            if (hasReservations)
            {
                throw new BusinessConflictException(
                    "No se puede cambiar el hotel ni el tipo de una habitación con reservas registradas.");
            }
        }

        var entry = context.Rooms.Attach(room);

        entry.Property(item => item.HotelId).IsModified = true;
        entry.Property(item => item.RoomTypeId).IsModified = true;
        entry.Property(item => item.RoomNumber).IsModified = true;
        entry.Property(item => item.Floor).IsModified = true;

        try
        {
            await SaveRoomChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ResourceNotFoundException(
                "La habitación ya no existe.");
        }

        await transaction.CommitAsync(cancellationToken);
    }
    
    public async Task<Room> UpdateOperationalStatusAsync(
        long roomId,
        string operationalStatus,
        CancellationToken cancellationToken)
    {
        if (roomId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la habitación debe ser positivo.");
        }

        if (operationalStatus != "ACTIVE" &&
            operationalStatus != "OUT_OF_SERVICE" &&
            operationalStatus != "INACTIVE")
        {
            throw new BusinessValidationException(
                "El estado debe ser ACTIVE, OUT_OF_SERVICE o INACTIVE.");
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // T05 y C05 bloquean esta misma habitación.
        // Las reglas se verifican después de obtener el bloqueo.
        var rooms = await context.Rooms
            .FromSqlInterpolated($"""
                SELECT *
                FROM hotel.rooms
                WHERE room_id = {roomId}
                FOR UPDATE
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var room = rooms.SingleOrDefault();

        if (room is null)
        {
            throw new ResourceNotFoundException(
                "La habitación no existe.");
        }

        // Repetir el estado actual no genera cambios.
        if (room.OperationalStatus == operationalStatus)
        {
            await transaction.CommitAsync(cancellationToken);
            return room;
        }

        if (operationalStatus != "ACTIVE")
        {
            // Una estadía abierta bloquea incluso si terminó
            // el periodo previsto de su reserva.
            var hasOpenStay = await context.Stays.AnyAsync(
                stay =>
                    stay.Reservation.RoomId == roomId &&
                    (stay.Status == "ACTIVE" ||
                    stay.CheckOutAt == null),
                cancellationToken);

            if (hasOpenStay)
            {
                throw new BusinessConflictException(
                    "No se puede desactivar una habitación con una estadía abierta.");
            }

            var hotel = await context.Hotels
                .AsNoTracking()
                .SingleAsync(
                    item => item.HotelId == room.HotelId,
                    cancellationToken);

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                hotel.Timezone);

            var localDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow, timeZone));

             // Incluye reservas futuras y las que siguen vigentes hoy.
            var hasActiveReservation = await context.Reservations.AnyAsync(
                reservation =>
                    reservation.RoomId == roomId &&
                    (reservation.Status == "PENDING" ||
                    reservation.Status == "CONFIRMED" ||
                    reservation.Status == "CHECKED_IN") &&
                    reservation.CheckOutDate > localDate,
                cancellationToken);

            if (hasActiveReservation)
            {
                throw new BusinessConflictException(
                    "No se puede desactivar una habitación con reservas activas vigentes o futuras.");
            }
        }

        var entry = context.Rooms.Attach(room);

        room.OperationalStatus = operationalStatus;
        entry.Property(item => item.OperationalStatus).IsModified = true;

        await SaveRoomChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return room;
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
