using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Maintenances;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Maintenances.Commands;

public sealed class CloseMaintenanceCommand(
    long maintenanceId,
    CloseMaintenanceDto input) : IRequest<MaintenanceDto>
{
    public long MaintenanceId { get; } = maintenanceId;
    public CloseMaintenanceDto Input { get; } = input;
}

internal sealed class CloseMaintenanceCommandHandler(
    IMaintenanceRepository maintenanceRepository)
    : IRequestHandler<CloseMaintenanceCommand, MaintenanceDto>
{
    public async Task<MaintenanceDto> Handle(
        CloseMaintenanceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.MaintenanceId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del mantenimiento debe ser positivo.");
        }

        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request.Input,
                new ValidationContext(request.Input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ??
                "Los datos del cierre no son válidos.");
        }

        if (decimal.Round(request.Input.Cost, 2) !=
            request.Input.Cost)
        {
            throw new BusinessValidationException(
                "El costo debe tener como máximo dos decimales.");
        }

        var maintenance = await maintenanceRepository.CloseAsync(
            request.MaintenanceId,
            request.Input.Cost,
            cancellationToken);

        return MaintenanceMapping.ToDto(maintenance);
    }
}