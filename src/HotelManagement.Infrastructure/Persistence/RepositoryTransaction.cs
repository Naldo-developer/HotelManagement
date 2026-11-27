using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagement.Infrastructure.Persistence;

/// <summary>
/// Participa en la transacción del caso de uso. Las invocaciones directas de
/// repositorios conservan su transacción local y sus garantías de bloqueo.
/// </summary>
internal sealed class RepositoryTransaction(IDbContextTransaction? owned) : IAsyncDisposable
{
    public Task CommitAsync(CancellationToken cancellationToken) =>
        owned?.CommitAsync(cancellationToken) ?? Task.CompletedTask;

    public ValueTask DisposeAsync() => owned?.DisposeAsync() ?? ValueTask.CompletedTask;
}

internal static class RepositoryTransactionExtensions
{
    public static async Task<RepositoryTransaction> BeginRepositoryTransactionAsync(
        this DatabaseFacade database, IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new RepositoryTransaction(database.CurrentTransaction is null
            ? await database.BeginTransactionAsync(isolationLevel, cancellationToken)
            : null);
    }
}
