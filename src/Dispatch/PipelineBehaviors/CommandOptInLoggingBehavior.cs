using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Vesia.Dispatch;

internal sealed class CommandOptInLoggingBehavior<TCommand, TResult>(ILogger<CommandOptInLoggingBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TCommand).GetCustomAttribute<LoggedAttribute>() is not null;

        var sw = Stopwatch.StartNew();
        try
        {
            if (shouldLog)
                logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);

            var result = await next();
            sw.Stop();

            if (shouldLog)
                logger.LogInformation("Handled command {Command} in {Elapsed}ms", typeof(TCommand).Name, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling command {Command} after {Elapsed}ms", typeof(TCommand).Name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

internal sealed class CommandOptInLoggingBehavior<TCommand>(ILogger<CommandOptInLoggingBehavior<TCommand>> logger)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    public async Task Handle(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TCommand).GetCustomAttribute<LoggedAttribute>() is not null;
        
        var sw = Stopwatch.StartNew();
        try
        {
            if (shouldLog)
                logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);

            await next();
            sw.Stop();
            
            if (shouldLog)
                logger.LogInformation("Handled command {Command} in {Elapsed}ms", typeof(TCommand).Name, sw.ElapsedMilliseconds);

        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling command {Command} after {Elapsed}ms", typeof(TCommand).Name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}