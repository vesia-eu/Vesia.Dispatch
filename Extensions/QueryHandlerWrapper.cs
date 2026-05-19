using Venly.Dispatch.Exceptions;
using Venly.Dispatch.Interfaces;

namespace Venly.Dispatch.Extensions;

public class QueryHandlerWrapper<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) : IQueryHandlerWrapper<TResult>
{
    public Task<TResult> Handle(object query, CancellationToken cancellationToken = default)
    {
        if (query is not TQuery typedCommand)
            throw new HandlerNotFoundException(query.GetType().Name);
        
        return handler.Handle(typedCommand, cancellationToken);
    }
}
