using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.RoomTypes.Commands;

public sealed class UpdateRoomTypeCommand(
    long roomTypeId,
    UpdateRoomTypeDto input) : IRequest<RoomTypeDto>, ITransactionalRequest
{
    public long RoomTypeId { get; } = roomTypeId;

    public UpdateRoomTypeDto Input { get; } = input;
}

internal sealed class UpdateRoomTypeCommandHandler(
    IRoomTypeRepository roomTypeRepository)
    : IRequestHandler<UpdateRoomTypeCommand, RoomTypeDto>
{
    public async Task<RoomTypeDto> Handle(
        UpdateRoomTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RoomTypeId <= 0)
        {
            throw new BusinessValidationException(
                "El ID del tipo de habitación debe ser mayor que cero.");
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
                "Los datos del tipo de habitación no son válidos.");
        }

        if (decimal.Round(input.BaseRate, 2) != input.BaseRate)
        {
            throw new BusinessValidationException(
                "La tarifa admite como máximo dos decimales.");
        }

        var roomType = await roomTypeRepository.GetByIdAsync(
            request.RoomTypeId, cancellationToken);

        if (roomType is null)
        {
            throw new ResourceNotFoundException(
                "El tipo de habitación indicado no existe.");
        }

        roomType.Name = input.Name.Trim();
        roomType.Description = input.Description?.Trim();
        roomType.MaxAdults = input.MaxAdults;
        roomType.MaxChildren = input.MaxChildren;
        roomType.BaseRate = input.BaseRate;

        var updated = await roomTypeRepository.UpdateAsync(
            roomType, cancellationToken);

        return RoomTypeMapping.ToDto(updated);
    }
}