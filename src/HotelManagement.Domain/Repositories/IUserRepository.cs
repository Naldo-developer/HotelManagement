using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task RegisterLoginAsync(long userId, DateTime loggedInAtUtc, CancellationToken cancellationToken);
}