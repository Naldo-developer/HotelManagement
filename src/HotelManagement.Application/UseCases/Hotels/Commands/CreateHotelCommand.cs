using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Hotels.Commands;

public sealed class CreateHotelCommand(
    CreateHotelDto input) : IRequest<HotelDto>
{
    public CreateHotelDto Input { get; } = input;
}

internal sealed class CreateHotelCommandHandler(
    IHotelRepository hotelRepository)
    : IRequestHandler<CreateHotelCommand, HotelDto>
{
    public async Task<HotelDto> Handle(
        CreateHotelCommand request,
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
                "Los datos del hotel no son válidos.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(
                input.Timezone.Trim(), out _))
        {
            throw new BusinessValidationException(
                "La zona horaria indicada no es válida.");
        }

        var hotel = new Hotel
        {
            Name = input.Name.Trim(),
            TaxId = string.IsNullOrWhiteSpace(input.TaxId)
                ? null : input.TaxId.Trim(),
            Address = input.Address?.Trim(),
            Phone = input.Phone?.Trim(),
            Email = input.Email?.Trim(),
            CurrencyCode = input.CurrencyCode,
            Timezone = input.Timezone.Trim(),
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await hotelRepository.CreateAsync(
            hotel, cancellationToken);

        return HotelMapping.ToDto(created);
    }
}