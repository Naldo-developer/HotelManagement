using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Reservations.Queries;

public class GetReservationsQuery(ReservationFilterDto filter)
    : IRequest<List<ReservationDto>>
{
    public ReservationFilterDto Filter { get; } = filter;
}

internal class GetReservationsQueryHandler(
    IReservationRepository reservationRepository)
    : IRequestHandler<GetReservationsQuery, List<ReservationDto>>
{
    public async Task<List<ReservationDto>> Handle(
        GetReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                filter,
                new ValidationContext(filter),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los filtros son inválidos.");
        }

        var reservations = await reservationRepository.GetAllAsync(
            filter.HotelId,
            filter.RoomId,
            filter.PrimaryGuestId,
            filter.Status,
            filter.Page,
            filter.PageSize,
            cancellationToken);

        return reservations
            .Select(reservation => ReservationMapping.ToDto(reservation))
            .ToList();
    }
}