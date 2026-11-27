using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Guests;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Guests.Commands;

public class UpdateGuestCommand(long id, UpdateGuestDto input) : IRequest<GuestDto>, ITransactionalRequest
{
    public long Id { get; } = id;
    public UpdateGuestDto Input { get; } = input;
}

internal class UpdateGuestCommandHandler(IGuestRepository guestRepository)
    : IRequestHandler<UpdateGuestCommand, GuestDto>
{
    public async Task<GuestDto> Handle(
        UpdateGuestCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del huésped debe ser positivo.");
        }

        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(input, new ValidationContext(input),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos del huésped son inválidos.");
        }

        var guest = await guestRepository.GetByIdAsync(request.Id, cancellationToken);

        if (guest is null)
        {
            throw new ResourceNotFoundException(
                $"No existe un huésped con id {request.Id}.");
        }

        var documentType = NullIfBlank(input.DocumentType);
        var documentNumber = NullIfBlank(input.DocumentNumber);

        if (documentType is not null && documentNumber is not null &&
            await guestRepository.DocumentExistsForOtherGuestAsync(
                documentType, documentNumber, guest.GuestId, cancellationToken))
        {
            throw new BusinessConflictException("Ya existe un huésped con ese documento.");
        }

        guest.FirstName = input.FirstName.Trim();
        guest.LastName = input.LastName.Trim();
        guest.DocumentType = documentType;
        guest.DocumentNumber = documentNumber;
        guest.Email = NullIfBlank(input.Email);
        guest.Phone = NullIfBlank(input.Phone);

        var updated = await guestRepository.UpdateAsync(guest, cancellationToken);

        return new GuestDto
        {
            Id = updated.GuestId,
            FirstName = updated.FirstName,
            LastName = updated.LastName,
            DocumentType = updated.DocumentType,
            DocumentNumber = updated.DocumentNumber,
            Email = updated.Email,
            Phone = updated.Phone
        };
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}