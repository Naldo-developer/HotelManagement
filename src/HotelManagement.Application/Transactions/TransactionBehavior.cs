using MediatR;

namespace HotelManagement.Application.Transactions;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull, ITransactionalRequest
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(_ => next(), cancellationToken);
}
