using HotelManagement.Application.Dtos.Services;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Mappings;

internal static class ServiceMapping
{
    public static ServiceDto ToDto(Service service)
    {
        return new ServiceDto
        {
            ServiceId = service.ServiceId,
            HotelId = service.HotelId,
            Name = service.Name,
            Description = service.Description,
            DefaultPrice = service.DefaultPrice,
            Unit = service.Unit,
            Active = service.Active
        };
    }
}