using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(
        long reservationId,
        decimal amount,
        string paymentMethod,
        long userId,
        string? reference,
        string? notes,
        CancellationToken cancellationToken);

    Task<List<Payment>> GetByReservationIdAsync(
        long reservationId,
        CancellationToken cancellationToken);
}