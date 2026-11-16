using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IHotelRepository
{
    Task<IReadOnlyList<Hotel>> GetAllAsync(CancellationToken cancellationToken);
}