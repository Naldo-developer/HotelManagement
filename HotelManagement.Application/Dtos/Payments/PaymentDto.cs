namespace HotelManagement.Application.Dtos.Payments;

public sealed class PaymentDto
{
    public long PaymentId { get; set; }
    public long ReservationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
    public long? RecordedByUserId { get; set; }
    public string? Notes { get; set; }
}