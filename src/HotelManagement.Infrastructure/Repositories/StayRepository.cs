using System.Data;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class StayRepository(HotelManagementDbContext context)
    : IStayRepository
{
    public Task<Stay?> GetByIdAsync(
        long stayId,
        CancellationToken cancellationToken)
    {
        return context.Stays
            .AsNoTracking()
            .Include(stay => stay.Reservation)
            .SingleOrDefaultAsync(
                stay => stay.StayId == stayId,
                cancellationToken);
    }

    public Task<Stay?> GetByReservationIdAsync(
        long reservationId,
        CancellationToken cancellationToken)
    {
        return context.Stays
            .AsNoTracking()
            .Include(stay => stay.Reservation)
            .SingleOrDefaultAsync(
                stay => stay.ReservationId == reservationId,
                cancellationToken);
    }

    public async Task<Stay> CheckInAsync(
        long reservationId,
        long userId,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (reservationId <= 0 || userId <= 0)
        {
            throw new BusinessValidationException(
                "La reserva y el usuario deben ser válidos.");
        }

        if (notes is { Length: > 1000 })
        {
            throw new BusinessValidationException(
                "Las notas no deben superar los 1000 caracteres.");
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "SET LOCAL search_path TO hotel, public",
            cancellationToken);

        // Orden de bloqueo de C05: reserva -> habitación.
        // T06 bloquea esta misma reserva antes de cancelar.
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

        // El estado se valida después de obtener el bloqueo.
        if (reservation.Status != "PENDING" &&
            reservation.Status != "CONFIRMED")
        {
            throw new BusinessConflictException(
                "Solo se puede hacer check-in de reservas pendientes o confirmadas.");
        }

        var alreadyHasStay = await context.Stays.AnyAsync(
            stay => stay.ReservationId == reservationId,
            cancellationToken);

        if (alreadyHasStay)
        {
            throw new BusinessConflictException(
                "La reserva ya tiene una estadía registrada.");
        }

        // Serializa los check-in de reservas diferentes
        // que intenten ocupar la misma habitación.
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
                "La habitación de la reserva no existe.");
        }

        if (room.OperationalStatus != "ACTIVE")
        {
            throw new BusinessConflictException(
                "La habitación no se encuentra operativa.");
        }

        var hasActiveStay = await context.Stays.AnyAsync(
            stay =>
                stay.Reservation.RoomId == room.RoomId &&
                (stay.Status == "ACTIVE" ||
                 stay.CheckOutAt == null),
            cancellationToken);

        if (hasActiveStay)
        {
            throw new BusinessConflictException(
                "La habitación ya tiene una estadía abierta.");
        }

        var hotel = await context.Hotels
            .AsNoTracking()
            .SingleAsync(
                item => item.HotelId == room.HotelId,
                cancellationToken);

        var roomType = await context.RoomTypes
            .AsNoTracking()
            .SingleAsync(
                item => item.RoomTypeId == room.RoomTypeId,
                cancellationToken);

        if (!hotel.Active || !roomType.Active)
        {
            throw new BusinessConflictException(
                "El hotel o el tipo de habitación no se encuentra activo.");
        }

        // Revalida capacidad por si se editó el tipo de habitación.
        if (reservation.Adults > roomType.MaxAdults ||
            reservation.Children > roomType.MaxChildren)
        {
            throw new BusinessConflictException(
                "La capacidad actual de la habitación no admite a los huéspedes de la reserva.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            hotel.Timezone);

        var checkInAt = DateTime.UtcNow;

        var localDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(checkInAt, timeZone));

        if (localDate < reservation.CheckInDate ||
            localDate >= reservation.CheckOutDate)
        {
            throw new BusinessConflictException(
                "El check-in debe realizarse desde la fecha de entrada y antes de la fecha de salida.");
        }

        var hasMaintenance = await context.Maintenances.AnyAsync(
            maintenance =>
                maintenance.RoomId == room.RoomId &&
                (maintenance.Status == "SCHEDULED" ||
                 maintenance.Status == "IN_PROGRESS") &&
                maintenance.StartDate < reservation.CheckOutDate &&
                maintenance.EndDate > reservation.CheckInDate,
            cancellationToken);

        if (hasMaintenance)
        {
            throw new BusinessConflictException(
                "La habitación tiene mantenimiento durante el periodo de la reserva.");
        }

        var validUser = await context.AppUsers.AnyAsync(
            user =>
                user.UserId == userId &&
                user.Active &&
                user.Employee.Active,
            cancellationToken);

        if (!validUser)
        {
            throw new BusinessConflictException(
                "El usuario responsable no está activo.");
        }

        // Adjunta la reserva existente y modifica únicamente su estado.
        context.Reservations.Attach(reservation);

        reservation.Status = "CHECKED_IN";

        context.Entry(reservation)
            .Property(item => item.Status)
            .IsModified = true;

        var stay = new Stay
        {
            ReservationId = reservation.ReservationId,
            Reservation = reservation,
            CheckInAt = checkInAt,
            CheckInByUserId = userId,
            Status = "ACTIVE",
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim()
        };

        context.Stays.Add(stay);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_stays_reservation"
            })
        {
            throw new BusinessConflictException(
                "La reserva ya tiene una estadía registrada.");
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
                "La habitación presenta un conflicto de disponibilidad.");
        }

        await transaction.CommitAsync(cancellationToken);

        return stay;
    }
        public async Task<Stay> CheckOutAsync(
            long stayId,
            long userId,
            string? notes,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                "SET LOCAL search_path TO hotel, public",
                cancellationToken);

            var stays = await context.Stays
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM hotel.stays
                    WHERE stay_id = {stayId}
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken);

            var stay = stays.SingleOrDefault();

            if (stay is null)
            {
                throw new ResourceNotFoundException(
                    "La estadía indicada no existe.");
            }

            if (stay.Status != "ACTIVE" || stay.CheckOutAt is not null)
            {
                throw new BusinessConflictException(
                    "Solo se pueden cerrar estadías activas.");
            }

            var reservation = await context.Reservations
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM hotel.reservations
                    WHERE reservation_id = {stay.ReservationId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (reservation is null)
            {
                throw new ResourceNotFoundException(
                    "La reserva asociada no existe.");
            }

            var user = await context.AppUsers
                .Include(appUser => appUser.Employee)
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    appUser => appUser.UserId == userId,
                    cancellationToken);

            if (user is null ||
                !user.Active ||
                user.Employee is null ||
                !user.Employee.Active)
            {
                throw new BusinessValidationException(
                    "El usuario que realiza el check-out no está activo.");
            }

            var nights =
                reservation.CheckOutDate.DayNumber -
                reservation.CheckInDate.DayNumber;

            var accommodationTotal = nights * reservation.NightlyRate;

            var consumptionTotal = await context.Consumptions
                .Where(consumption => consumption.StayId == stayId)
                .Select(consumption =>
                    consumption.Subtotal ??
                    consumption.Quantity * consumption.UnitPrice)
                .SumAsync(cancellationToken);

            var payments = await context.Payments
                .AsNoTracking()
                .Where(payment =>
                    payment.ReservationId == reservation.ReservationId &&
                    payment.Status == "COMPLETED")
                .Select(payment => new
                {
                    payment.TransactionType,
                    payment.Amount
                })
                .ToListAsync(cancellationToken);

            var totalPaid = payments.Sum(payment =>
                payment.TransactionType == "REFUND"
                    ? -payment.Amount
                    : payment.Amount);

            var totalDue = accommodationTotal + consumptionTotal;
            var balanceDue = totalDue - totalPaid;

            if (balanceDue > 0.01m)
            {
                throw new BusinessConflictException(
                    $"No se puede cerrar la estadía. " +
                    $"Saldo pendiente: {balanceDue:0.00}.");
            }

            stay.Status = "COMPLETED";
            stay.CheckOutAt = DateTime.UtcNow;
            stay.CheckOutByUserId = userId;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                stay.Notes = string.IsNullOrWhiteSpace(stay.Notes)
                    ? $"Check-out: {notes.Trim()}"
                    : $"{stay.Notes}\nCheck-out: {notes.Trim()}";
            }

            reservation.Status = "COMPLETED";

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return stay;
        }
}