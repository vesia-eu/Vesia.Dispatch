using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Vesia.Dispatch;

internal sealed class CommandLoggingBehavior<TCommand, TResult>(ILogger<CommandLoggingBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);
    
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next();
            sw.Stop();
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

internal class CommandLoggingBehavior<TCommand>(ILogger<CommandLoggingBehavior<TCommand>> logger)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    public async Task Handle(TCommand command, Func<Task> next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);

        var sw = Stopwatch.StartNew();
        try
        {
            await next();
            sw.Stop();
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