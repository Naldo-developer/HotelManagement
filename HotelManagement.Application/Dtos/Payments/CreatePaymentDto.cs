using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Payments;

public sealed class CreatePaymentDto
{
    [Range(1, long.MaxValue)]
    public long ReservationId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999")]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression(
        "^(CASH|CARD|TRANSFER|DIGITAL_WALLET|OTHER)$",
        ErrorMessage = "El método de pago no es válido.")]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}