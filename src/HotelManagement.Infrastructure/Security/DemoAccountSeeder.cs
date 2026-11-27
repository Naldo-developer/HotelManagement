using HotelManagement.Application.Security;
using HotelManagement.Domain.Entities;
using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelManagement.Infrastructure.Security;

public static class DemoAccountSeeder
{
    private const int MinimumPasswordLength = 8;

    private static readonly (string Username, string RoleCode, string FirstName, string LastName, string Position)[] Accounts =
    [
        ("admin.demo", "ADMIN", "Alicia", "Demo", "Administradora"),
        ("recepcion.demo", "RECEPTION", "Rosa", "Demo", "Recepcionista"),
        ("caja.demo", "CASHIER", "Carlos", "Demo", "Cajero"),
        ("mantenimiento.demo", "MAINTENANCE", "Mario", "Demo", "Técnico de mantenimiento")
    ];

    public static async Task SeedDemoAccountsAsync(
        this IServiceProvider services,
        string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoAccountSeeder");

        if (password.Length < MinimumPasswordLength)
        {
            logger.LogWarning(
                "La contraseña demo debe tener al menos {Length} caracteres. No se crearon cuentas.",
                MinimumPasswordLength);
            return;
        }

        var context = scope.ServiceProvider.GetRequiredService<HotelManagementDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<ICredentialHasher>();

        var hotel = await context.Hotels
            .OrderBy(h => h.HotelId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hotel is null)
        {
            logger.LogWarning("No hay hoteles en la base. Cargue database/002_demo_seed.sql.");
            return;
        }

        var roles = await context.Roles.ToDictionaryAsync(r => r.Code, cancellationToken);
        var created = 0;

        foreach (var account in Accounts)
        {
            if (await context.AppUsers.AnyAsync(u => u.Username == account.Username, cancellationToken)
                || !roles.TryGetValue(account.RoleCode, out var role))
            {
                continue;
            }

            var user = new AppUser
            {
                Username = account.Username,
                PasswordHash = hasher.Hash(password),
                Active = true,
                Employee = new Employee
                {
                    HotelId = hotel.HotelId,
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    Position = account.Position,
                    Active = true
                }
            };

            user.Roles.Add(role);
            context.AppUsers.Add(user);
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Se crearon {Count} cuentas demo.", created);
        }
    }
}