using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(
        Reservation reservation,
        CancellationToken cancellationToken);

    Task<List<Reservation>> GetAllAsync(
        long? hotelId,
        long? roomId,
        long? primaryGuestId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Reservation?> GetByIdAsync(
        long reservationId,
        CancellationToken cancellationToken);

    Task<Reservation> CancelAsync(
        long reservationId,
        string cancellationReason,
        CancellationToken cancellationToken);
}