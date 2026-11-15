using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;

namespace HotelManagement.Api.Tests;

internal sealed class FakeGuestRepository : IGuestRepository
{
    public List<Guest> Guests { get; } = [];

    public Task<Guest?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Guests.FirstOrDefault(g => g.GuestId == id));

    public Task<IReadOnlyList<Guest>> GetAllAsync(
        string? name, string? documentType, string? documentNumber,
        int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guest>>(Guests);

    public Task<bool> DocumentExistsAsync(
        string documentType, string documentNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Guests.Any(g =>
            g.DocumentType == documentType && g.DocumentNumber == documentNumber));

    public Task<bool> DocumentExistsForOtherGuestAsync(
        string documentType, string documentNumber, long excludedGuestId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Guests.Any(g =>
            g.GuestId != excludedGuestId &&
            g.DocumentType == documentType && g.DocumentNumber == documentNumber));

    public Task<Guest> CreateAsync(Guest guest, CancellationToken cancellationToken)
    {
        guest.GuestId = Guests.Count + 1;
        Guests.Add(guest);
        return Task.FromResult(guest);
    }

    public Task<Guest> UpdateAsync(Guest guest, CancellationToken cancellationToken) =>
        Task.FromResult(guest);
}