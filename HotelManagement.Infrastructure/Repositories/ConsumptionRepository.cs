using System.Data;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class ConsumptionRepository(HotelManagementDbContext context)
    : IConsumptionRepository
{
    public async Task<Consumption> CreateAsync(
        long stayId,
        long serviceId,
        decimal quantity,
        long userId,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (stayId <= 0 || serviceId <= 0 || userId <= 0)
        {
            throw new BusinessValidationException(
                "La estadía, el servicio y el usuario deben ser válidos.");
        }

        if (quantity <= 0)
        {
            throw new BusinessValidationException(
                "La cantidad debe ser mayor que cero.");
        }

        if (notes is { Length: > 1000 })
        {
            throw new BusinessValidationException(
                "Las notas no deben superar los 1000 caracteres.");
        }

        await using var transaction =
            await context.Database.BeginRepositoryTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "SET LOCAL search_path TO hotel, public",
            cancellationToken);

        // Bloquea la estadía para coordinar consumos con el check-out.
        var stays = await context.Stays
            .FromSqlInterpolated($"""
                SELECT *
                FROM hotel.stays
                WHERE stay_id = {stayId}
                FOR UPDATE
                """)
            .AsNoTracking()
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
                "No se pueden registrar consumos en una estadía cerrada.");
        }

        var reservation = await context.Reservations
            .AsNoTracking()
            .Include(item => item.Room)
            .SingleAsync(
                item => item.ReservationId == stay.ReservationId,
                cancellationToken);

        var service = await context.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.ServiceId == serviceId &&
                    item.HotelId == reservation.Room.HotelId &&
                    item.Active,
                cancellationToken);

        if (service is null)
        {
            throw new ResourceNotFoundException(
                "El servicio no existe, está inactivo o no pertenece al hotel de la estadía.");
        }

        var userIsActive = await context.AppUsers
            .Where(user => user.UserId == userId)
            .Where(user => user.Active && user.Employee.Active)
            .AnyAsync(cancellationToken);

        if (!userIsActive)
        {
            throw new BusinessConflictException(
                "El usuario responsable no está activo.");
        }

        var consumption = new Consumption
        {
            StayId = stayId,
            ServiceId = service.ServiceId,
            Quantity = quantity,
            UnitPrice = service.DefaultPrice,
            ConsumedAt = DateTime.UtcNow,
            RecordedByUserId = userId,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim()
        };

        context.Consumptions.Add(consumption);

        await context.SaveChangesAsync(cancellationToken);
        consumption.Service = service;
        await transaction.CommitAsync(cancellationToken);

        return consumption;
    }

    public Task<List<Consumption>> GetByStayIdAsync(
        long stayId,
        CancellationToken cancellationToken)
    {
        return context.Consumptions
            .AsNoTracking()
            .Include(consumption => consumption.Service)
            .Where(consumption => consumption.StayId == stayId)
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ThenByDescending(consumption => consumption.ConsumptionId)
            .ToListAsync(cancellationToken);
    }
}