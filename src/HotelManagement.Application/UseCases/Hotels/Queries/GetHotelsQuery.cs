using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Hotels.Queries;

public class GetHotelsQuery : IRequest<IReadOnlyList<HotelDto>>
{
}

internal class GetHotelsQueryHandler(IHotelRepository hotelRepository)
    : IRequestHandler<GetHotelsQuery, IReadOnlyList<HotelDto>>
{
    public async Task<IReadOnlyList<HotelDto>> Handle(
        GetHotelsQuery request,
        CancellationToken cancellationToken)
    {
        var hotels = await hotelRepository.GetAllAsync(cancellationToken);

        return hotels.Select(hotel => new HotelDto
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name
        }).ToList();
    }
}