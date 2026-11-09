using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Queries;

public class GetRoomsQuery(RoomFilterDto filter) : IRequest<IReadOnlyList<RoomDto>>
{
    public RoomFilterDto Filter { get; } = filter;
}

internal class GetRoomsQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetRoomsQuery, IReadOnlyList<RoomDto>>
{
    public async Task<IReadOnlyList<RoomDto>> Handle(
        GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(filter, new ValidationContext(filter),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los filtros de habitaciones son inválidos.");
        }

        long offset = ((long)filter.Page - 1) * filter.PageSize;
        if (offset > int.MaxValue)
        {
            throw new BusinessValidationException(
                "La página solicitada es demasiado alta para ese tamaño de página.");
        }

        var rooms = await roomRepository.GetAllAsync(
            filter.HotelId, filter.RoomTypeId, filter.OperationalStatus,
            filter.Page, filter.PageSize, cancellationToken);

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