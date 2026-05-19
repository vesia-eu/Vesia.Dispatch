using Venly.Dispatch.Exceptions;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.Extensions;

public class QueryPipelineBehaviorWrapper<TQuery, TResult>(IQueryPipelineBehavior<TQuery, TResult> behavior)
    : IQueryPipelineBehaviorWrapper<TResult> where TQuery : IQuery<TResult>
{
    public Task<TResult> Handle(object query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        if (query is not TQuery typedQuery)
            throw new HandlerNotFoundException(query.GetType().Name);
        
        return behavior.Handle(typedQuery, next, cancellationToken);
    }
}
