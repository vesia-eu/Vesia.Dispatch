using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.Interfaces.Behavior;

public interface IQueryPipelineBehavior<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, Func<Task<TResult>> next,
        CancellationToken cancellationToken = default);
}
