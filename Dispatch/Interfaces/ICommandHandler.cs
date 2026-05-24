namespace Venly.Dispatch.Interfaces;

public interface ICommandHandler<in TCommand, TResult> : IHandler
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
