using HotelManagement.Application.Interfaces;
using HotelManagement.Application.Transactions;

namespace HotelManagement.Api.Tests;

// Solo para pruebas unitarias con FakeGuestRepository. La atomicidad real
// se comprueba por separado en PostgreSQL, nunca con este doble de prueba.
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) => operation(cancellationToken);
}
