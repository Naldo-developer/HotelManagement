using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Application.Mappings;
using HotelManagement.Application.UseCases.Stays;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Stays.Commands;

public class CheckInCommand(CheckInDto input, long userId)
    : IRequest<StayDto>, ITransactionalRequest
{
    public CheckInDto Input { get; } = input;

    public long UserId { get; } = userId;
}

internal class CheckInCommandHandler(IStayRepository stayRepository)
    : IRequestHandler<CheckInCommand, StayDto>
{
    public async Task<StayDto> Handle(
        CheckInCommand request,
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
                ?? "Los datos del check-in son inválidos.");
        }

        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El usuario responsable no es válido.");
        }

        var stay = await stayRepository.CheckInAsync(
            request.Input.ReservationId,
            request.UserId,
            request.Input.Notes,
            cancellationToken);

        return StayMapping.ToDto(stay);
    }
}