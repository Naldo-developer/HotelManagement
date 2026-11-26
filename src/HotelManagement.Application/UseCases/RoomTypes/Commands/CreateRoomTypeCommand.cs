using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.RoomTypes.Commands;

public sealed class CreateRoomTypeCommand(
    CreateRoomTypeDto input) : IRequest<RoomTypeDto>
{
    public CreateRoomTypeDto Input { get; } = input;
}

internal sealed class CreateRoomTypeCommandHandler(
    IRoomTypeRepository roomTypeRepository,
    IHotelRepository hotelRepository)
    : IRequestHandler<CreateRoomTypeCommand, RoomTypeDto>
{
    public async Task<RoomTypeDto> Handle(
        CreateRoomTypeCommand request,
        CancellationToken cancellationToken)
    {
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

        var hotel = await hotelRepository.GetByIdAsync(
            input.HotelId, cancellationToken);

        if (hotel is null)
        {
            throw new ResourceNotFoundException(
                "El hotel indicado no existe.");
        }

        if (!hotel.Active)
        {
            throw new BusinessConflictException(
                "No se pueden registrar tipos en un hotel inactivo.");
        }

        var roomType = new RoomType
        {
            HotelId = input.HotelId,
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            MaxAdults = input.MaxAdults,
            MaxChildren = input.MaxChildren,
            BaseRate = input.BaseRate,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await roomTypeRepository.CreateAsync(
            roomType, cancellationToken);

        return RoomTypeMapping.ToDto(created);
    }
}