using System.Reflection;
using Microsoft.Extensions.Logging;
using Venly.Dispatch.Attributes;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.PipelineBehaviors;

public class QueryOptInLoggingBehavior<TQuery, TResult>(ILogger<QueryOptInLoggingBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> Handle(TQuery query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TQuery).GetCustomAttribute<LoggedAttribute>() is not null;

        if (shouldLog) 
            logger.LogInformation("Handling query {Query}", typeof(TQuery).Name);
        
        var result = await next();
        
        if (shouldLog) 
            logger.LogInformation("Handled query {Query}", typeof(TQuery).Name);
        return result;
    }
}
