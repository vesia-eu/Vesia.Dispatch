using Microsoft.Extensions.Logging;
using Vesia.Dispatch.Interfaces.Behavior;
using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.PipelineBehaviors;

public class CommandLoggingBehavior<TCommand, TResult>(ILogger<CommandLoggingBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);
    
        try
        {
            var result = await next();
            logger.LogInformation("Handled command {Command}", typeof(TCommand).Name);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling command {Command}", typeof(TCommand).Name);
            throw;
        }
    }
}