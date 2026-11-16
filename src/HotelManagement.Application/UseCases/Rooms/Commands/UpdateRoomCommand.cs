using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Commands;

public class UpdateRoomCommand(long roomId, UpdateRoomDto input) : IRequest<RoomDto>
{
    public long RoomId { get; } = roomId;
    public UpdateRoomDto Input { get; } = input;
}

internal class UpdateRoomCommandHandler(IRoomRepository roomRepository)
    : IRequestHandler<UpdateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(
        UpdateRoomCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(input, new ValidationContext(input),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos de la habitación son inválidos.");
        }

        var room = await roomRepository.GetByIdAsync(
            request.RoomId, cancellationToken);

        if (room is null)
        {
            throw new ResourceNotFoundException("La habitación no existe.");
        }

        if (!await roomRepository.HotelExistsAsync(input.HotelId, cancellationToken))
        {
            throw new BusinessValidationException("El hotel indicado no existe.");
        }

        if (!await roomRepository.RoomTypeBelongsToHotelAsync(
                input.RoomTypeId, input.HotelId, cancellationToken))
        {
            throw new BusinessValidationException(
                "El tipo de habitación debe existir y pertenecer al hotel indicado.");
        }

        var roomNumber = input.RoomNumber.Trim();

        if (await roomRepository.RoomNumberExistsAsync(
                input.HotelId, roomNumber, cancellationToken, excludeRoomId: room.RoomId))
        {
            throw new BusinessConflictException(
                "Ya existe una habitación con ese número en el hotel.");
        }

        room.HotelId = input.HotelId;
        room.RoomTypeId = input.RoomTypeId;
        room.RoomNumber = roomNumber;
        room.Floor = string.IsNullOrWhiteSpace(input.Floor) ? null : input.Floor.Trim();

        await roomRepository.UpdateAsync(room, cancellationToken);

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