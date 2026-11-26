using HotelManagement.Application.Dtos.Maintenances;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Maintenances.Queries;

public sealed class GetMaintenancesQuery(
    long? roomId,
    string? status) : IRequest<List<MaintenanceDto>>
{
    public long? RoomId { get; } = roomId;
    public string? Status { get; } = status;
}

internal sealed class GetMaintenancesQueryHandler(
    IMaintenanceRepository maintenanceRepository)
    : IRequestHandler<GetMaintenancesQuery, List<MaintenanceDto>>
{
    public async Task<List<MaintenanceDto>> Handle(
        GetMaintenancesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.RoomId.HasValue &&
            request.RoomId.Value <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la habitación debe ser positivo.");
        }

        var status = request.Status?.Trim().ToUpperInvariant();

        if (status is not null &&
            status != "SCHEDULED" &&
            status != "IN_PROGRESS" &&
            status != "COMPLETED" &&
            status != "CANCELLED")
        {
            throw new BusinessValidationException(
                "El estado del mantenimiento no es válido.");
        }

        var maintenances = await maintenanceRepository.GetAllAsync(
            request.RoomId,
            status,
            cancellationToken);

        return maintenances
            .Select(MaintenanceMapping.ToDto)
            .ToList();
    }
}