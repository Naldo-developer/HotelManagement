using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Commands;

public class CreateRoomCommand(CreateRoomDto input) : IRequest<RoomDto>
{
    public CreateRoomDto Input { get; } = input;
}

internal class CreateRoomCommandHandler(IRoomRepository roomRepository)
    : IRequestHandler<CreateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(
        CreateRoomCommand request,
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
                input.HotelId, roomNumber, cancellationToken))
        {
            throw new BusinessConflictException(
                "Ya existe una habitación con ese número en el hotel.");
        }

        var room = new Room
        {
            HotelId = input.HotelId,
            RoomTypeId = input.RoomTypeId,
            RoomNumber = roomNumber,
            Floor = string.IsNullOrWhiteSpace(input.Floor) ? null : input.Floor.Trim(),
            OperationalStatus = input.OperationalStatus
        };

        var created = await roomRepository.CreateAsync(room, cancellationToken);

        return new RoomDto
        {
            RoomId = created.RoomId,
            HotelId = created.HotelId,
            RoomTypeId = created.RoomTypeId,
            RoomNumber = created.RoomNumber,
            Floor = created.Floor,
            OperationalStatus = created.OperationalStatus
        };
    }
}