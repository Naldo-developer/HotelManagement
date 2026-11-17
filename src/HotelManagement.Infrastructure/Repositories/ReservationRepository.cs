using System.Data;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class ReservationRepository(HotelManagementDbContext context)
    : IReservationRepository
{
    public async Task<Reservation> CreateAsync(
        Reservation reservation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "SET LOCAL search_path TO hotel, public",
            cancellationToken);

        // El bloqueo dura hasta confirmar o revertir la transacción.
        var rooms = await context.Rooms
            .FromSqlInterpolated($"""
                SELECT *
                FROM hotel.rooms
                WHERE room_id = {reservation.RoomId}
                FOR UPDATE
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var room = rooms.SingleOrDefault();

        if (room is null)
        {
            throw new ResourceNotFoundException(
                "La habitación indicada no existe.");
        }

        if (room.OperationalStatus != "ACTIVE")
        {
            throw new BusinessConflictException(
                "La habitación no se encuentra operativa.");
        }

        var guestExists = await context.Guests.AnyAsync(
            guest => guest.GuestId == reservation.PrimaryGuestId,
            cancellationToken);

        if (!guestExists)
        {
            throw new ResourceNotFoundException(
                "El huésped indicado no existe.");
        }

        if (reservation.CheckInDate == default ||
            reservation.CheckOutDate <= reservation.CheckInDate)
        {
            throw new BusinessValidationException(
                "La fecha de salida debe ser posterior a la fecha de entrada.");
        }

        var roomType = await context.RoomTypes
            .AsNoTracking()
            .SingleAsync(
                type => type.RoomTypeId == room.RoomTypeId,
                cancellationToken);

        if (reservation.Adults < 1 ||
            reservation.Adults > roomType.MaxAdults)
        {
            throw new BusinessValidationException(
                "La cantidad de adultos debe estar entre uno y la capacidad de la habitación.");
        }

        if (reservation.Children < 0 ||
            reservation.Children > roomType.MaxChildren)
        {
            throw new BusinessValidationException(
                "La cantidad de niños supera el rango permitido para la habitación.");
        }

        var hasReservation = await context.Reservations.AnyAsync(
            existing =>
                existing.RoomId == reservation.RoomId &&
                (existing.Status == "PENDING" ||
                 existing.Status == "CONFIRMED" ||
                 existing.Status == "CHECKED_IN") &&
                existing.CheckInDate < reservation.CheckOutDate &&
                existing.CheckOutDate > reservation.CheckInDate,
            cancellationToken);

        var hasMaintenance = await context.Maintenances.AnyAsync(
            maintenance =>
                maintenance.RoomId == reservation.RoomId &&
                (maintenance.Status == "SCHEDULED" ||
                 maintenance.Status == "IN_PROGRESS") &&
                maintenance.StartDate < reservation.CheckOutDate &&
                maintenance.EndDate > reservation.CheckInDate,
            cancellationToken);

        if (hasReservation || hasMaintenance)
        {
            throw new BusinessConflictException(
                "La habitación no está disponible para las fechas indicadas.");
        }

        reservation.NightlyRate = roomType.BaseRate;
        reservation.Status = "PENDING";

        context.Reservations.Add(reservation);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: "ex_reservations_room_period"
                    or "ex_room_period_blocks_overlap"
            })
        {
            throw new BusinessConflictException(
                "La habitación ya fue ocupada por una reserva o mantenimiento en esas fechas.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: "P0001"
            } postgres &&
            postgres.MessageText ==
                $"La habitación {reservation.RoomId} posee mantenimiento durante el periodo solicitado.")
        {
            // El trigger puede detectar mantenimiento creado después de la consulta.
            throw new BusinessConflictException(
                "La habitación tiene mantenimiento durante las fechas indicadas.");
        }

        await transaction.CommitAsync(cancellationToken);

        return reservation;
    }

    public Task<List<Reservation>> GetAllAsync(
        long? hotelId,
        long? roomId,
        long? primaryGuestId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var reservations = context.Reservations.AsNoTracking();

        if (hotelId.HasValue)
        {
            reservations = reservations.Where(
                reservation => reservation.Room.HotelId == hotelId.Value);
        }

        if (roomId.HasValue)
        {
            reservations = reservations.Where(
                reservation => reservation.RoomId == roomId.Value);
        }

        if (primaryGuestId.HasValue)
        {
            reservations = reservations.Where(
                reservation => reservation.PrimaryGuestId == primaryGuestId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            reservations = reservations.Where(
                reservation => reservation.Status == status);
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new BusinessValidationException(
                "La página o el tamaño de página no son válidos.");
        }

        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            throw new BusinessValidationException(
                "La página solicitada supera el límite permitido.");
        }

        return reservations
            .OrderByDescending(reservation => reservation.ReservationId)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<Reservation?> GetByIdAsync(
        long reservationId,
        CancellationToken cancellationToken)
    {
        return context.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reservation => reservation.ReservationId == reservationId,
                cancellationToken);
    }

    public async Task<Reservation> CancelAsync(
        long reservationId,
        string cancellationReason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cancellationReason) ||
            cancellationReason.Length > 500)
        {
            throw new BusinessValidationException(
                "El motivo es obligatorio y no debe superar los 500 caracteres.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "SET LOCAL search_path TO hotel, public",
            cancellationToken);

        // C05 debe bloquear esta misma reserva antes de validar el check-in.
        var reservations = await context.Reservations
            .FromSqlInterpolated($"""
                SELECT *
                FROM hotel.reservations
                WHERE reservation_id = {reservationId}
                FOR UPDATE
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reservation = reservations.SingleOrDefault();

        if (reservation is null)
        {
            throw new ResourceNotFoundException(
                "La reserva indicada no existe.");
        }

        // Solo una reserva pendiente o confirmada admite cancelación.
        if (reservation.Status != "PENDING" &&
            reservation.Status != "CONFIRMED")
        {
            throw new BusinessConflictException(
                "Solo se pueden cancelar reservas pendientes o confirmadas.");
        }

        var hasStay = await context.Stays.AnyAsync(
            stay => stay.ReservationId == reservationId,
            cancellationToken);

        if (hasStay)
        {
            throw new BusinessConflictException(
                "No se puede cancelar una reserva que ya tiene una estadía.");
        }

        reservation.Status = "CANCELLED";
        reservation.CancellationReason = cancellationReason.Trim();

        var entry = context.Reservations.Attach(reservation);
        entry.Property(item => item.Status).IsModified = true;
        entry.Property(item => item.CancellationReason).IsModified = true;

        // Los triggers registran el historial y liberan el periodo reservado.
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return reservation;
    }
}