using HotelManagement.Application.Dtos.Consumptions;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Consumptions.Queries;

public record GetConsumptionsByStayQuery(long StayId)
    : IRequest<List<ConsumptionDto>>;

internal class GetConsumptionsByStayQueryHandler(
    IConsumptionRepository consumptionRepository,
    IStayRepository stayRepository)
    : IRequestHandler<GetConsumptionsByStayQuery, List<ConsumptionDto>>
{
    public async Task<List<ConsumptionDto>> Handle(
        GetConsumptionsByStayQuery request,
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

        var consumptions = await consumptionRepository.GetByStayIdAsync(
            request.StayId,
            cancellationToken);

        return consumptions
            .Select(ConsumptionMapping.ToDto)
            .ToList();
    }
}