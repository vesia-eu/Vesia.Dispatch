using Microsoft.Extensions.Logging;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.PipelineBehaviors;

public class CommandLoggingBehavior<TCommand, TResult>(ILogger<CommandLoggingBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);
        var result = await next();
        logger.LogInformation("Handled command {Command}", typeof(TCommand).Name);
        return result;
    }
}