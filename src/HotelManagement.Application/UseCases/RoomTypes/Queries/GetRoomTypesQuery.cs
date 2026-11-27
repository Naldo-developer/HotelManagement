using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.RoomTypes.Queries;

public class GetRoomTypesQuery(long hotelId)
    : IRequest<IReadOnlyList<RoomTypeDto>>
{
    public long HotelId { get; } = hotelId;
}

internal class GetRoomTypesQueryHandler(
    IRoomTypeRepository roomTypeRepository)
    : IRequestHandler<GetRoomTypesQuery, IReadOnlyList<RoomTypeDto>>
{
    public async Task<IReadOnlyList<RoomTypeDto>> Handle(
        GetRoomTypesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.HotelId <= 0)
        {
            throw new BusinessValidationException(
                "El ID del hotel debe ser mayor que cero.");
        }

        var roomTypes = await roomTypeRepository.GetAllAsync(
            request.HotelId, cancellationToken);

        return roomTypes.Select(RoomTypeMapping.ToDto).ToList();
    }
}