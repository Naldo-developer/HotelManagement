using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Reservations.Queries;

public class GetReservationByIdQuery(long reservationId)
    : IRequest<ReservationDto>
{
    public long ReservationId { get; } = reservationId;
}

internal class GetReservationByIdQueryHandler(
    IReservationRepository reservationRepository)
    : IRequestHandler<GetReservationByIdQuery, ReservationDto>
{
    public async Task<ReservationDto> Handle(
        GetReservationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la reserva debe ser positivo.");
        }

        var reservation = await reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new ResourceNotFoundException(
                "La reserva indicada no existe.");
        }

        return ReservationMapping.ToDto(reservation);
    }
}