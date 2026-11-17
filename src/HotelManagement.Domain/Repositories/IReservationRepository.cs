using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(
        Reservation reservation,
        CancellationToken cancellationToken);
}