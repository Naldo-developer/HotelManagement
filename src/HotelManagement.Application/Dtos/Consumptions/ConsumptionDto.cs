namespace HotelManagement.Application.Dtos.Consumptions;

public class ConsumptionDto
{
    public long ConsumptionId { get; set; }

    public long StayId { get; set; }

    public long ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public DateTime ConsumedAt { get; set; }

    public long? RecordedByUserId { get; set; }

    public string? Notes { get; set; }
}