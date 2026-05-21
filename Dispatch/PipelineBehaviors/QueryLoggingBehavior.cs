using Microsoft.Extensions.Logging;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.PipelineBehaviors;

public class QueryLoggingBehavior<TQuery, TResult>(ILogger<QueryLoggingBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> Handle(TQuery query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling query {Query}", typeof(TQuery).Name);
        var result = await next();
        logger.LogInformation("Handled query {Query}", typeof(TQuery).Name);
        return result;
    }
}
