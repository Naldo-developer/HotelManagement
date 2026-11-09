using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Queries;

public class GetRoomByIdQuery(long roomId) : IRequest<RoomDto>
{
    public long RoomId { get; } = roomId;
}

internal class GetRoomByIdQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetRoomByIdQuery, RoomDto>
{
    public async Task<RoomDto> Handle(
        GetRoomByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.RoomId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la habitación debe ser positivo.");
        }

        var room = await roomRepository.GetByIdAsync(
            request.RoomId, cancellationToken);

        if (room is null)
        {
            throw new ResourceNotFoundException(
                $"No se encontró la habitación con ID {request.RoomId}.");
        }

        return new RoomDto
        {
            RoomId = room.RoomId,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus
        };
    }
}