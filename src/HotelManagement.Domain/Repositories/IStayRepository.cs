using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IStayRepository
{
    Task<Stay> CheckInAsync(
        long reservationId,
        long userId,
        string? notes,
        CancellationToken cancellationToken);

    Task<Stay?> GetByIdAsync(
        long stayId,
        CancellationToken cancellationToken);

    Task<Stay?> GetByReservationIdAsync(
        long reservationId,
        CancellationToken cancellationToken);
}