using HotelManagement.Application.DTOs.Guests;  
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Guests.Queries;

public class GetGuestsQuery : IRequest<IReadOnlyList<GuestDto>>
{
    public GuestFilterDto Filter { get; set; } = new();
}

internal class GetGuestsQueryHandler : IRequestHandler<GetGuestsQuery, IReadOnlyList<GuestDto>>
{
    private readonly IGuestRepository _guestRepository;

    public GetGuestsQueryHandler(IGuestRepository guestRepository)
    {
        _guestRepository = guestRepository;
    }

    public async Task<IReadOnlyList<GuestDto>> Handle(
        GetGuestsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        var guests = await _guestRepository.GetAllAsync(
            filter.Name,
            filter.DocumentType,
            filter.DocumentNumber,
            filter.Page,
            filter.PageSize,
            cancellationToken);

        return guests.Select(ToDto).ToList();
    }
    

    private static GuestDto ToDto(Guest guest)
    {
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