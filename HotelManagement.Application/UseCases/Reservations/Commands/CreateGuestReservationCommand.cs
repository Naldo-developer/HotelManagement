using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Transactions;
using HotelManagement.Application.UseCases.Guests.Commands;
using HotelManagement.Domain.Exceptions;
using MediatR;

namespace HotelManagement.Application.UseCases.Reservations.Commands;

public sealed record CreateGuestReservationCommand(CreateGuestReservationDto Input)
    : IRequest<ReservationDto>, ITransactionalRequest;

internal sealed class CreateGuestReservationCommandHandler(ISender sender)
    : IRequestHandler<CreateGuestReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(CreateGuestReservationCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new BusinessValidationException(errors[0].ErrorMessage ?? "Datos de reserva inválidos.");

        // Ambos comandos y sus repositorios participan en la misma unidad de trabajo.
        var guest = await sender.Send(new CreateGuestCommand(input.Guest), cancellationToken);
        return await sender.Send(new CreateReservationCommand(new CreateReservationDto
        {
            PrimaryGuestId = guest.Id,
            RoomId = input.RoomId,
            CheckInDate = input.CheckInDate,
            CheckOutDate = input.CheckOutDate,
            Adults = input.Adults,
            Children = input.Children,
            SpecialRequests = input.SpecialRequests
        }), cancellationToken);
    }
}
