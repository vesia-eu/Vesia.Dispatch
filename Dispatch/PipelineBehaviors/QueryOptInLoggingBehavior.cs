using System.Reflection;
using Microsoft.Extensions.Logging;
using Vesia.Dispatch.Attributes;
using Vesia.Dispatch.Interfaces.Behavior;
using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.PipelineBehaviors;

public class QueryOptInLoggingBehavior<TQuery, TResult>(ILogger<QueryOptInLoggingBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> Handle(TQuery query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TQuery).GetCustomAttribute<LoggedAttribute>() is not null;

        try
        {
            if (shouldLog) 
                logger.LogInformation("Handling query {Query}", typeof(TQuery).Name);
        
            var result = await next();
        
            if (shouldLog) 
                logger.LogInformation("Handled query {Query}", typeof(TQuery).Name);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling query {Query}", typeof(TQuery).Name);
            throw;
        }
    }
}
