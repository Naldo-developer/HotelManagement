using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Rooms.Commands;

public class UpdateRoomOperationalStatusCommand(
    long roomId,
    UpdateRoomOperationalStatusDto input) : IRequest<RoomDto>, ITransactionalRequest
{
    public long RoomId { get; } = roomId;
    public UpdateRoomOperationalStatusDto Input { get; } = input;
}

internal class UpdateRoomOperationalStatusCommandHandler(
    IRoomRepository roomRepository)
    : IRequestHandler<UpdateRoomOperationalStatusCommand, RoomDto>
{
    public async Task<RoomDto> Handle(
        UpdateRoomOperationalStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RoomId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la habitación debe ser positivo.");
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
                errors[0].ErrorMessage ??
                "El estado operativo es inválido.");
        }

        var room = await roomRepository.UpdateOperationalStatusAsync(
            request.RoomId,
            input.OperationalStatus,
            cancellationToken);

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