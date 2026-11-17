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
            // El trigger puede detectar un mantenimiento creado después de la consulta.
            throw new BusinessConflictException(
                "La habitación tiene mantenimiento durante las fechas indicadas.");
        }

        await transaction.CommitAsync(cancellationToken);

        return reservation;
    }
}