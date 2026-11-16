using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.DTOs.Guests;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Guests.Commands;

public class CreateGuestCommand(CreateGuestDto input) : IRequest<GuestDto>
{
    public CreateGuestDto Input { get; } = input;
}

internal class CreateGuestCommandHandler(IGuestRepository guestRepository)
    : IRequestHandler<CreateGuestCommand, GuestDto>
{
    public async Task<GuestDto> Handle(
        CreateGuestCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(input, new ValidationContext(input),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos del huésped son inválidos.");
        }

        var documentType = NullIfBlank(input.DocumentType);
        var documentNumber = NullIfBlank(input.DocumentNumber);

        if (documentType is not null && documentNumber is not null &&
            await guestRepository.DocumentExistsAsync(
                documentType, documentNumber, cancellationToken))
        {
            throw new BusinessConflictException("Ya existe un huésped con ese documento.");
        }

        var guest = new Guest
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            Email = NullIfBlank(input.Email),
            Phone = NullIfBlank(input.Phone)
        };

        var created = await guestRepository.CreateAsync(guest, cancellationToken);

        return new GuestDto
        {
            Id = created.GuestId,
            FirstName = created.FirstName,
            LastName = created.LastName,
            DocumentType = created.DocumentType,
            DocumentNumber = created.DocumentNumber,
            Email = created.Email,
            Phone = created.Phone
        };
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}