using HotelManagement.Domain.Entities; 

namespace HotelManagement.Domain.Repositories;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guest>> GetAllAsync(
        string? name,
        string? documentType,
        string? documentNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}