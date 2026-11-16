using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class UserRepository(HotelManagementDbContext context) : IUserRepository
{
    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return context.AppUsers
            .Include(u => u.Employee)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task RegisterLoginAsync(
        long userId, DateTime loggedInAtUtc, CancellationToken cancellationToken)
    {
        DateTime? value = loggedInAtUtc;

        await context.AppUsers
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.LastLoginAt, value),
                cancellationToken);
    }
}