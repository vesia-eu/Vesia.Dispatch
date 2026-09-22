using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Vesia.Dispatch;

internal sealed class QueryLoggingBehavior<TQuery, TResult>(ILogger<QueryLoggingBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> Handle(TQuery query, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Handling query {Query}", typeof(TQuery).Name);
            var result = await next();
            sw.Stop();
            logger.LogInformation("Handled query {Query} in {Elapsed}ms", typeof(TQuery).Name, sw.ElapsedMilliseconds);
            return result;
        }
        catch(Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling query {Query} after {Elapsed}ms", typeof(TQuery).Name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
