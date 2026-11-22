using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.UseCases.Stays;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Stays.Queries;

public record GetStayByReservationIdQuery(long ReservationId)
    : IRequest<StayDto>;

internal class GetStayByReservationIdQueryHandler(
    IStayRepository stayRepository)
    : IRequestHandler<GetStayByReservationIdQuery, StayDto>
{
    public async Task<StayDto> Handle(
        GetStayByReservationIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la reserva debe ser mayor que cero.");
        }

        var stay = await stayRepository.GetByReservationIdAsync(
            request.ReservationId,
            cancellationToken);

        if (stay is null)
        {
            throw new ResourceNotFoundException(
                "No existe una estadía asociada a la reserva indicada.");
        }

        return StayMapping.ToDto(stay);
    }
}