using System.Data;
using HotelManagement.Application.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Persistence;

/// <summary>Un DbContext scoped y una transacción compartida por los repositorios.</summary>
public sealed class EfUnitOfWork(HotelManagementDbContext context) : IUnitOfWork
{
    private bool executing;
    private bool rollbackOnly;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        if (executing)
        {
            // Los comandos compuestos participan en la transacción exterior.
            try { return await operation(cancellationToken); }
            catch { rollbackOnly = true; throw; }
        }

        if (context.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("La transacción debe pertenecer a la unidad de trabajo.");

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);
        executing = true;
        rollbackOnly = false;
        try
        {
            var result = await operation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (rollbackOnly)
                throw new InvalidOperationException("Un comando falló; no se puede confirmar la operación parcial.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            finally { context.ChangeTracker.Clear(); }
            throw;
        }
        finally { executing = false; rollbackOnly = false; }
    }
}
