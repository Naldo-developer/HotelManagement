using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class ServiceRepository(HotelManagementDbContext context)
    : IServiceRepository
{
    public Task<List<Service>> GetAllAsync(
        long hotelId,
        CancellationToken cancellationToken)
    {
        return context.Services
            .AsNoTracking()
            .Where(service =>
                service.HotelId == hotelId &&
                service.Active)
            .OrderBy(service => service.Name)
            .ThenBy(service => service.ServiceId)
            .ToListAsync(cancellationToken);
    }

    public Task<Service?> GetByIdAsync(
        long serviceId,
        CancellationToken cancellationToken)
    {
        return context.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(
                service => service.ServiceId == serviceId,
                cancellationToken);
    }

    public async Task<Service> CreateAsync(
        Service service,
        CancellationToken cancellationToken)
    {
        try
        {
            context.Services.Add(service);
            await context.SaveChangesAsync(cancellationToken);
            return service;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_services_hotel_name"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un servicio con ese nombre en el hotel.");
        }
    }

    public async Task<Service> UpdateAsync(
        Service service,
        CancellationToken cancellationToken)
    {
        try
        {
            context.Services.Update(service);
            await context.SaveChangesAsync(cancellationToken);
            return service;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_services_hotel_name"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un servicio con ese nombre en el hotel.");
        }
    }
}