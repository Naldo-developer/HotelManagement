using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Reservations.Commands;

public class CancelReservationCommand(
    long reservationId,
    CancelReservationDto input) : IRequest<ReservationDto>
{
    public long ReservationId { get; } = reservationId;
    public CancelReservationDto Input { get; } = input;
}

internal class CancelReservationCommandHandler(
    IReservationRepository reservationRepository)
    : IRequestHandler<CancelReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(
        CancelReservationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la reserva debe ser positivo.");
        }

        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                input,
                new ValidationContext(input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos de cancelación son inválidos.");
        }

        // El repositorio valida el estado dentro de la transacción.
        var reservation = await reservationRepository.CancelAsync(
            request.ReservationId,
            input.CancellationReason,
            cancellationToken);

        return ReservationMapping.ToDto(reservation);
    }
}