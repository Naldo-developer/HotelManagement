using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Hotels.Commands;

public sealed class UpdateHotelCommand(
    long hotelId,
    UpdateHotelDto input) : IRequest<HotelDto>
{
    public long HotelId { get; } = hotelId;

    public UpdateHotelDto Input { get; } = input;
}

internal sealed class UpdateHotelCommandHandler(
    IHotelRepository hotelRepository)
    : IRequestHandler<UpdateHotelCommand, HotelDto>
{
    public async Task<HotelDto> Handle(
        UpdateHotelCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HotelId <= 0)
        {
            throw new BusinessValidationException(
                "El ID del hotel debe ser mayor que cero.");
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
                "Los datos del hotel no son válidos.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(
                input.Timezone.Trim(), out _))
        {
            throw new BusinessValidationException(
                "La zona horaria indicada no es válida.");
        }

        var hotel = await hotelRepository.GetByIdAsync(
            request.HotelId, cancellationToken);

        if (hotel is null)
        {
            throw new ResourceNotFoundException(
                "El hotel indicado no existe.");
        }

        hotel.Name = input.Name.Trim();
        hotel.TaxId = string.IsNullOrWhiteSpace(input.TaxId)
            ? null : input.TaxId.Trim();
        hotel.Address = input.Address?.Trim();
        hotel.Phone = input.Phone?.Trim();
        hotel.Email = input.Email?.Trim();
        hotel.CurrencyCode = input.CurrencyCode;
        hotel.Timezone = input.Timezone.Trim();

        var updated = await hotelRepository.UpdateAsync(
            hotel, cancellationToken);

        return HotelMapping.ToDto(updated);
    }
}