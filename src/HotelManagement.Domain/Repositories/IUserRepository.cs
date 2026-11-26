using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    Task RegisterLoginAsync(
        long userId,
        DateTime loggedInAtUtc,
        CancellationToken cancellationToken);

    Task<List<AppUser>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<AppUser?> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<AppUser> CreateAsync(
        AppUser user,
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken);

    Task<AppUser> UpdateAsync(
        AppUser user,
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken);
}