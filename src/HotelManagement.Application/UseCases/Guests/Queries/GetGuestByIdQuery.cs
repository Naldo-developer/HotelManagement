using HotelManagement.Application.Exceptions;
using HotelManagement.Application.DTOs.Guests;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Guests.Queries;

public class GetGuestByIdQuery : IRequest<GuestDto>
{
    public long Id { get; init; }

    public GetGuestByIdQuery(long id)
    {
        Id = id;
    }
}

internal class GetGuestByIdQueryHandler : IRequestHandler<GetGuestByIdQuery, GuestDto>
{
    private readonly IGuestRepository _guestRepository;

    public GetGuestByIdQueryHandler(IGuestRepository guestRepository)
    {
        _guestRepository = guestRepository;
    }

    public async Task<GuestDto> Handle(
        GetGuestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var guest = await _guestRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (guest == null)
        {
            throw new ResourceNotFoundException($"No existe un huésped con id {request.Id}.");
        }

        return new GuestDto
        {
            Id = guest.GuestId,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            DocumentType = guest.DocumentType,
            DocumentNumber = guest.DocumentNumber,
            Email = guest.Email,
            Phone = guest.Phone
        };
    }
}