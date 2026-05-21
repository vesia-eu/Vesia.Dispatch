using System.Reflection;
using Microsoft.Extensions.Logging;
using Venly.Dispatch.Attributes;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.PipelineBehaviors;

public class CommandOptInLoggingBehavior<TCommand, TResult>(ILogger<CommandOptInLoggingBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
    {
        var shouldLog = typeof(TCommand).GetCustomAttribute<LoggedAttribute>() is not null;
        
        if (shouldLog) 
            logger.LogInformation("Handling command {Command}", typeof(TCommand).Name);
        
        var result = await next();
        
        if(shouldLog) 
            logger.LogInformation("Handled command {Command}", typeof(TCommand).Name);
        
        return result;
    }
}