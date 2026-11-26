using HotelManagement.Application.Dtos.Payments;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.Payments;

internal static class PaymentMapping
{
    public static PaymentDto ToDto(Payment payment) => new()
    {
        PaymentId = payment.PaymentId,
        ReservationId = payment.ReservationId,
        TransactionType = payment.TransactionType,
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod,
        Status = payment.Status,
        Reference = payment.Reference,
        PaidAt = payment.PaidAt,
        RecordedByUserId = payment.RecordedByUserId,
        Notes = payment.Notes
    };
}