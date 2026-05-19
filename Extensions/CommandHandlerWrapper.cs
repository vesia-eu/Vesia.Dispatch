using Venly.Dispatch.Exceptions;
using Venly.Dispatch.Interfaces;

namespace Venly.Dispatch.Extensions;

public class CommandHandlerWrapper<TCommand, TResult>(ICommandHandler<TCommand,TResult> handler)
    : ICommandHandlerWrapper<TResult>
{
    public Task<TResult> Handle(object command, CancellationToken cancellationToken = default)
    {
        if (command is not TCommand typedCommand)
            throw new HandlerNotFoundException(command.GetType().Name);
        
        return handler.Handle(typedCommand, cancellationToken);
    }
}