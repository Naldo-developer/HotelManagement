using System.Data;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class MaintenanceRepository(
    HotelManagementDbContext context) : IMaintenanceRepository
{
    public Task<List<Maintenance>> GetAllAsync(
        long? roomId,
        string? status,
        CancellationToken cancellationToken)
    {
        var maintenances = context.Maintenances.AsNoTracking();

        if (roomId.HasValue)
        {
            maintenances = maintenances.Where(
                item => item.RoomId == roomId.Value);
        }

        if (status is not null)
        {
            maintenances = maintenances.Where(
                item => item.Status == status);
        }

        return maintenances
            .OrderByDescending(item => item.StartDate)
            .ThenByDescending(item => item.MaintenanceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Maintenance> CreateAsync(
        Maintenance maintenance,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginRepositoryTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var room = await LockRoomAsync(
            maintenance.RoomId,
            cancellationToken);

        if (room.OperationalStatus == "INACTIVE")
        {
            throw new BusinessConflictException(
                "No se puede registrar mantenimiento en una habitación inactiva.");
        }

        var hasOpenStay = await context.Stays.AnyAsync(
            stay =>
                stay.Reservation.RoomId == maintenance.RoomId &&
                (stay.Status == "ACTIVE" ||
                 stay.CheckOutAt == null),
            cancellationToken);

        if (hasOpenStay)
        {
            throw new BusinessConflictException(
                "La habitación tiene una estadía abierta.");
        }

        var hasReservation = await context.Reservations.AnyAsync(
            reservation =>
                reservation.RoomId == maintenance.RoomId &&
                (reservation.Status == "PENDING" ||
                 reservation.Status == "CONFIRMED" ||
                 reservation.Status == "CHECKED_IN") &&
                reservation.CheckInDate < maintenance.EndDate &&
                reservation.CheckOutDate > maintenance.StartDate,
            cancellationToken);

        if (hasReservation)
        {
            throw new BusinessConflictException(
                "La habitación tiene reservas activas durante el periodo indicado.");
        }

        var hasMaintenance = await context.Maintenances.AnyAsync(
            item =>
                item.RoomId == maintenance.RoomId &&
                (item.Status == "SCHEDULED" ||
                 item.Status == "IN_PROGRESS") &&
                item.StartDate < maintenance.EndDate &&
                item.EndDate > maintenance.StartDate,
            cancellationToken);

        if (hasMaintenance)
        {
            throw new BusinessConflictException(
                "Ya existe un mantenimiento durante el periodo indicado.");
        }

        maintenance.Status = "SCHEDULED";
        maintenance.Cost = 0;

        context.Maintenances.Add(maintenance);

        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return maintenance;
    }

    public async Task<Maintenance> CloseAsync(
        long maintenanceId,
        decimal cost,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginRepositoryTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var existing = await context.Maintenances
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.MaintenanceId == maintenanceId,
                cancellationToken);

        if (existing is null)
        {
            throw new ResourceNotFoundException(
                "El mantenimiento no existe.");
        }

        await LockRoomAsync(
            existing.RoomId,
            cancellationToken);

        // Consultar nuevamente después de bloquear la habitación.
        var maintenance = await context.Maintenances
            .SingleOrDefaultAsync(
                item => item.MaintenanceId == maintenanceId,
                cancellationToken);

        if (maintenance is null)
        {
            throw new ResourceNotFoundException(
                "El mantenimiento no existe.");
        }

        if (maintenance.Status != "SCHEDULED" &&
            maintenance.Status != "IN_PROGRESS")
        {
            throw new BusinessConflictException(
                "Solo se pueden cerrar mantenimientos activos.");
        }

        maintenance.Status = "COMPLETED";
        maintenance.Cost = cost;
        maintenance.CompletedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return maintenance;
    }

    private async Task<Room> LockRoomAsync(
        long roomId,
        CancellationToken cancellationToken)
    {
        var rooms = await context.Rooms
            .FromSqlInterpolated($"""
                SELECT *
                FROM hotel.rooms
                WHERE room_id = {roomId}
                FOR UPDATE
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rooms.SingleOrDefault()
            ?? throw new ResourceNotFoundException(
                "La habitación no existe.");
    }

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation
            })
        {
            throw new BusinessConflictException(
                "Existe un conflicto de disponibilidad durante el periodo indicado.");
        }
    }
}