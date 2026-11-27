namespace HotelManagement.Application.Dtos.Payments;

public sealed class PaymentSummaryDto
{
    public long ReservationId { get; set; }
    public decimal AccommodationTotal { get; set; }
    public decimal ConsumptionTotal { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal BalanceDue { get; set; }
}