using Venly.Dispatch.Interfaces;
using Venly.Dispatch.Interfaces.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Venly.Dispatch.Interfaces.Behavior;

namespace Venly.Dispatch.Services;

public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public async Task<TResult> DispatchAsync<TResult>
        (ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var wrapper = serviceProvider.GetRequiredService<ICommandHandlerWrapper<TResult>>();
        
        var behaviors = serviceProvider
            .GetServices<ICommandPipelineBehaviorWrapper<TResult>>();
        
        var next = () => wrapper.Handle(command, cancellationToken);
        foreach (var behavior in behaviors.Reverse())
        {
            var currentNext = next;
            next = () => behavior.Handle(command, currentNext, cancellationToken);
        }
        
        return await next();
    }

    public async Task<TResult> DispatchAsync<TResult>
        (IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var wrapper = serviceProvider.GetRequiredService<IQueryHandlerWrapper<TResult>>();
        
        var behaviors = serviceProvider
            .GetServices<IQueryPipelineBehaviorWrapper<TResult>>();
        
        var next = () => wrapper.Handle(query, cancellationToken);
        foreach (var behavior in behaviors.Reverse())
        {
            var currentNext = next;
            next = () => behavior.Handle(query, currentNext, cancellationToken);
        }
        
        return await next();
    }
}