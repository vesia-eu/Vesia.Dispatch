using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.Interfaces;

public interface IDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default);
}
    