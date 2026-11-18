using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.UseCases.Stays;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Stays.Queries;

public record GetStayByIdQuery(long StayId) : IRequest<StayDto>;

internal class GetStayByIdQueryHandler(IStayRepository stayRepository)
    : IRequestHandler<GetStayByIdQuery, StayDto>
{
    public async Task<StayDto> Handle(
        GetStayByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.StayId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la estadía debe ser mayor que cero.");
        }

        var stay = await stayRepository.GetByIdAsync(
            request.StayId,
            cancellationToken);

        if (stay is null)
        {
            throw new ResourceNotFoundException(
                "La estadía indicada no existe.");
        }

        return StayMapping.ToDto(stay);
    }
}