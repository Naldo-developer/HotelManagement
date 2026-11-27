using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Queries;

public class GetAvailableRoomsQuery(RoomAvailabilityFilterDto filter)
    : IRequest<IReadOnlyList<RoomDto>>
{
    public RoomAvailabilityFilterDto Filter { get; } = filter;
}

internal class GetAvailableRoomsQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetAvailableRoomsQuery, IReadOnlyList<RoomDto>>
{
    public async Task<IReadOnlyList<RoomDto>> Handle(
        GetAvailableRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(filter, new ValidationContext(filter),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los filtros de disponibilidad son inválidos.");
        }

        // Los atributos Required del DTO ya validaron estos tres valores.
        var hotelId = filter.HotelId!.Value;
        var checkInDate = filter.CheckInDate!.Value;
        var checkOutDate = filter.CheckOutDate!.Value;

        if (checkOutDate <= checkInDate)
        {
            throw new BusinessValidationException(
                "La fecha de salida debe ser posterior a la fecha de entrada.");
        }

        var rooms = await roomRepository.GetAvailableAsync(
            hotelId, checkInDate, checkOutDate, filter.RoomTypeId, cancellationToken);

        return rooms.Select(room => new RoomDto
        {
            RoomId = room.RoomId,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus
        }).ToList();
    }
}