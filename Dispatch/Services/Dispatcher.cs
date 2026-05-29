using Microsoft.Extensions.DependencyInjection;
using Vesia.Dispatch.Exceptions;
using Vesia.Dispatch.Interfaces;
using Vesia.Dispatch.Interfaces.Behavior;
using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.Services;

public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public async Task<TResult> DispatchAsync<TResult>
        (ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        // resolve handler
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResult));
        
        dynamic handler = serviceProvider.GetService(handlerType) 
                          ?? throw new HandlerNotFoundException(command.GetType().Name);

        // resolve behaviors
        var behaviorType = typeof(ICommandPipelineBehavior<,>)
            .MakeGenericType(command.GetType(), typeof(TResult));
        var behaviors = serviceProvider.GetServices(behaviorType);

        // build chain
        var next = (Func<Task<TResult>>)(() => handler.Handle((dynamic)command, cancellationToken));
        foreach (var behavior in behaviors.Reverse())
        {
            if (behavior is null) continue;
            
            var currentNext = next;
            next = () => ((dynamic)behavior).Handle((dynamic)command, currentNext, cancellationToken);
        }

        return await next();
    }

    public async Task<TResult> DispatchAsync<TResult>
        (IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        // resolve handler
        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(query.GetType(), typeof(TResult));
        
        dynamic handler = serviceProvider.GetService(handlerType) 
                          ?? throw new HandlerNotFoundException(query.GetType().Name);

        // resolve behaviors
        var behaviorType = typeof(IQueryPipelineBehavior<,>)
            .MakeGenericType(query.GetType(), typeof(TResult));
        var behaviors = serviceProvider.GetServices(behaviorType);

        // build chain
        var next = (Func<Task<TResult>>)(() => handler.Handle((dynamic)query, cancellationToken));
        foreach (var behavior in behaviors.Reverse())
        {
            if (behavior is null) continue;
            
            var currentNext = next;
            next = () => ((dynamic)behavior).Handle((dynamic)query, currentNext, cancellationToken);
        }

        return await next();
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(INotificationHandler<>)
            .MakeGenericType(notification!.GetType());

        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            await ((dynamic)handler).Handle((dynamic)notification, cancellationToken);
        }
    }
}