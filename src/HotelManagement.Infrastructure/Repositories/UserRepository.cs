using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class UserRepository(HotelManagementDbContext context) : IUserRepository
{
    public Task<AppUser?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        return context.AppUsers
            .Include(user => user.Employee)
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(
                user => user.Username == username,
                cancellationToken);
    }

    public async Task RegisterLoginAsync(
        long userId,
        DateTime loggedInAtUtc,
        CancellationToken cancellationToken)
    {
        DateTime? value = loggedInAtUtc;

        await context.AppUsers
            .Where(user => user.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    user => user.LastLoginAt,
                    value),
                cancellationToken);
    }

    public Task<List<AppUser>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return context.AppUsers
            .AsNoTracking()
            .Include(user => user.Employee)
            .Include(user => user.Roles)
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);
    }

    public Task<AppUser?> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        return context.AppUsers
            .AsNoTracking()
            .Include(user => user.Employee)
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(
                user => user.UserId == userId,
                cancellationToken);
    }

    public async Task<AppUser> CreateAsync(
        AppUser user,
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var employee = await context.Employees
            .SingleOrDefaultAsync(
                item => item.EmployeeId == user.EmployeeId,
                cancellationToken);

        if (employee is null)
        {
            throw new ResourceNotFoundException(
                "El empleado indicado no existe.");
        }

        var employeeHasUser = await context.AppUsers
            .AnyAsync(
                item => item.EmployeeId == user.EmployeeId,
                cancellationToken);

        if (employeeHasUser)
        {
            throw new BusinessConflictException(
                "El empleado ya tiene una cuenta de usuario.");
        }

        user.Username = user.Username.Trim();
        user.Roles = await ResolveRolesAsync(
            roleCodes,
            cancellationToken);
        user.Employee = employee;

        try
        {
            context.AppUsers.Add(user);
            await context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_app_users_username"
            })
        {
            throw new BusinessConflictException(
                "El nombre de usuario ya está registrado.");
        }
    }

    public async Task<AppUser> UpdateAsync(
        AppUser user,
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var tracked = await context.AppUsers
            .Include(item => item.Roles)
            .Include(item => item.Employee)
            .SingleOrDefaultAsync(
                item => item.UserId == user.UserId,
                cancellationToken);

        if (tracked is null)
        {
            throw new ResourceNotFoundException(
                "El usuario indicado no existe.");
        }

        var usernameInUse = await context.AppUsers
            .AnyAsync(
                item => item.UserId != user.UserId &&
                        item.Username == user.Username.Trim(),
                cancellationToken);

        if (usernameInUse)
        {
            throw new BusinessConflictException(
                "El nombre de usuario ya está registrado.");
        }

        var roles = await ResolveRolesAsync(
            roleCodes,
            cancellationToken);

        tracked.Username = user.Username.Trim();
        tracked.PasswordHash = user.PasswordHash;
        tracked.Active = user.Active;

        tracked.Roles.Clear();

        foreach (var role in roles)
        {
            tracked.Roles.Add(role);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return tracked;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_app_users_username"
            })
        {
            throw new BusinessConflictException(
                "El nombre de usuario ya está registrado.");
        }
    }

    private async Task<List<Role>> ResolveRolesAsync(
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var codes = roleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
        {
            throw new BusinessValidationException(
                "Debe asignarse al menos un rol.");
        }

        var roles = await context.Roles
            .Where(role => codes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != codes.Count)
        {
            throw new BusinessValidationException(
                "Uno o más roles indicados no existen.");
        }

        return roles;
    }
}