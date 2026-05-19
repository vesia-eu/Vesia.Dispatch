using Venly.Dispatch.Exceptions;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.Extensions;

public class CommandPipelineBehaviorWrapper<TCommand, TResult>(ICommandPipelineBehavior<TCommand, TResult> behavior)
    : ICommandPipelineBehaviorWrapper<TResult> where TCommand : ICommand<TResult>
{
    public Task<TResult> Handle(object command, Func<Task<TResult>> next, CancellationToken cancellationToken)
    {
        if (command is not TCommand typedCommand)
            throw new HandlerNotFoundException(command.GetType().Name);
        
        return behavior.Handle(typedCommand, next, cancellationToken);
    }
}
