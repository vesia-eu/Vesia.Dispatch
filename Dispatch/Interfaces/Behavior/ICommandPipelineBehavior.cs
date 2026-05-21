using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.Interfaces.Behavior;

public interface ICommandPipelineBehavior<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, Func<Task<TResult>> next,
        CancellationToken cancellationToken = default);
}
