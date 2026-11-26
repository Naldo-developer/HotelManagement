using HotelManagement.Application.Dtos.Services;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.UseCases.Services;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Services.Queries;

public record GetServicesQuery(long HotelId) : IRequest<List<ServiceDto>>;

internal class GetServicesQueryHandler(IServiceRepository serviceRepository)
    : IRequestHandler<GetServicesQuery, List<ServiceDto>>
{
    public async Task<List<ServiceDto>> Handle(
        GetServicesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.HotelId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del hotel debe ser mayor que cero.");
        }

        var services = await serviceRepository.GetAllAsync(
            request.HotelId,
            cancellationToken);

        return services
            .Select(ServiceMapping.ToDto)
            .ToList();
    }
}