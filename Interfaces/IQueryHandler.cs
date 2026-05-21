
namespace Venly.Dispatch.Interfaces;

public interface IQueryHandler<in TQuery, TResult> : IHandler
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}

public interface IQueryHandlerWrapper<TResult>
{
    Task<TResult> Handle(object query, CancellationToken cancellationToken = default);
}