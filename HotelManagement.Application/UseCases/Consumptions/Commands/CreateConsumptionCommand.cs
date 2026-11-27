using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Consumptions;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Consumptions.Commands;

public class CreateConsumptionCommand(
    CreateConsumptionDto input,
    long userId) : IRequest<ConsumptionDto>, ITransactionalRequest
{
    public CreateConsumptionDto Input { get; } = input;

    public long UserId { get; } = userId;
}

internal class CreateConsumptionCommandHandler(
    IConsumptionRepository consumptionRepository)
    : IRequestHandler<CreateConsumptionCommand, ConsumptionDto>
{
    public async Task<ConsumptionDto> Handle(
        CreateConsumptionCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request.Input,
                new ValidationContext(request.Input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage
                ?? "Los datos del consumo no son válidos.");
        }

        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El usuario responsable no es válido.");
        }

        var consumption = await consumptionRepository.CreateAsync(
            request.Input.StayId,
            request.Input.ServiceId,
            request.Input.Quantity,
            request.UserId,
            request.Input.Notes,
            cancellationToken);

        return ConsumptionMapping.ToDto(consumption);
    }
}