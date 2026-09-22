using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Vesia.Dispatch;

internal sealed class QueryOptInLoggingBehavior<TQuery, TResult>(ILogger<QueryOptInLoggingBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> Handle(TQuery query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TQuery).GetCustomAttribute<LoggedAttribute>() is not null;

        var sw = Stopwatch.StartNew();
        try
        {
            if (shouldLog) 
                logger.LogInformation("Handling query {Query}", typeof(TQuery).Name);
        
            sw.Stop();
            var result = await next();
        
            if (shouldLog) 
                logger.LogInformation("Handled query {Query} in {Elapsed}ms", typeof(TQuery).Name, sw.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling query {Query} after {Elapsed}ms", typeof(TQuery).Name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
