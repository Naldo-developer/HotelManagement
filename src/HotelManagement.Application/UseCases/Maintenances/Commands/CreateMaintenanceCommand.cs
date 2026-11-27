using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Maintenances;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Maintenances.Commands;

public sealed class CreateMaintenanceCommand(
    CreateMaintenanceDto input,
    long userId) : IRequest<MaintenanceDto>, ITransactionalRequest
{
    public CreateMaintenanceDto Input { get; } = input;
    public long UserId { get; } = userId;
}

internal sealed class CreateMaintenanceCommandHandler(
    IMaintenanceRepository maintenanceRepository)
    : IRequestHandler<CreateMaintenanceCommand, MaintenanceDto>
{
    public async Task<MaintenanceDto> Handle(
        CreateMaintenanceCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                input,
                new ValidationContext(input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ??
                "Los datos del mantenimiento no son válidos.");
        }

        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El usuario responsable no es válido.");
        }

        if (input.StartDate == default ||
            input.EndDate == default)
        {
            throw new BusinessValidationException(
                "Las fechas del mantenimiento son obligatorias.");
        }

        if (input.EndDate <= input.StartDate)
        {
            throw new BusinessValidationException(
                "La fecha de fin debe ser posterior a la fecha de inicio.");
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            throw new BusinessValidationException(
                "La descripción es obligatoria.");
        }

        var maintenance = new Maintenance
        {
            RoomId = input.RoomId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Category = string.IsNullOrWhiteSpace(input.Category)
                ? null
                : input.Category.Trim(),
            Description = input.Description.Trim(),
            ReportedByUserId = request.UserId,
            Status = "SCHEDULED",
            Cost = 0,
            CreatedAt = DateTime.UtcNow
        };

        var created = await maintenanceRepository.CreateAsync(
            maintenance,
            cancellationToken);

        return MaintenanceMapping.ToDto(created);
    }
}