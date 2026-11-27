using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Stays.Commands;

public sealed class CheckOutCommand(
    CheckOutDto input,
    long userId) : IRequest<StayDto>
{
    public CheckOutDto Input { get; } = input;
    public long UserId { get; } = userId;
}

internal sealed class CheckOutCommandHandler(
    IStayRepository stayRepository)
    : IRequestHandler<CheckOutCommand, StayDto>
{
    public async Task<StayDto> Handle(
        CheckOutCommand request,
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
                errors[0].ErrorMessage ??
                "Los datos del check-out no son válidos.");
        }

        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El usuario responsable no es válido.");
        }

        var stay = await stayRepository.CheckOutAsync(
            request.Input.StayId,
            request.UserId,
            request.Input.Notes,
            cancellationToken);

        return StayMapping.ToDto(stay);
    }
}