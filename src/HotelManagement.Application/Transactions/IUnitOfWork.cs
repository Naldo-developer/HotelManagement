namespace HotelManagement.Application.Transactions;

/// <summary>Confirma o revierte todas las escrituras del caso de uso.</summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
