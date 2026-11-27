using HotelManagement.Application.Dtos.Consumptions;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Mappings;

internal static class ConsumptionMapping
{
    public static ConsumptionDto ToDto(Consumption consumption)
    {
        return new ConsumptionDto
        {
            ConsumptionId = consumption.ConsumptionId,
            StayId = consumption.StayId,
            ServiceId = consumption.ServiceId,
            ServiceName = consumption.Service?.Name ?? string.Empty,
            Quantity = consumption.Quantity,
            UnitPrice = consumption.UnitPrice,
            Subtotal = consumption.Subtotal
                       ?? consumption.Quantity * consumption.UnitPrice,
            ConsumedAt = consumption.ConsumedAt,
            RecordedByUserId = consumption.RecordedByUserId,
            Notes = consumption.Notes
        };
    }
}