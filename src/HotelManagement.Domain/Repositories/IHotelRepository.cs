using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IHotelRepository
{
    Task<IReadOnlyList<Hotel>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Hotel?> GetByIdAsync(
        long hotelId,
        CancellationToken cancellationToken);

    Task<Hotel> CreateAsync(
        Hotel hotel,
        CancellationToken cancellationToken);

    Task<Hotel> UpdateAsync(
        Hotel hotel,
        CancellationToken cancellationToken);
}