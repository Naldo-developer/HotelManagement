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

    Task<bool> DocumentExistsAsync(
        string documentType, string documentNumber, CancellationToken cancellationToken);

    Task<Guest> CreateAsync(Guest guest, CancellationToken cancellationToken);
    Task<bool> DocumentExistsForOtherGuestAsync(
        string documentType,
        string documentNumber,
        long excludedGuestId,
        CancellationToken cancellationToken);

    Task<Guest> UpdateAsync(Guest guest, CancellationToken cancellationToken);
}