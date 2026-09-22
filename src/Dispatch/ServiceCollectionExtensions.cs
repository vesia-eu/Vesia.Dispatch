using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Vesia.Dispatch;

/// <summary>
/// Provides extension methods for registering Vesia.Dispatch's dispatcher, handlers, and
/// pipeline behaviors with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static readonly ConcurrentDictionary<Type, Type> _notificationHandlerTypeCache = new();
    
    internal static readonly ConcurrentDictionary<(Type Command, Type Result), Type> _commandHandlerTypeCache = new();
    internal static readonly ConcurrentDictionary<(Type Command, Type Result), Type> _commandBehaviorTypeCache = new();
    
    internal static readonly ConcurrentDictionary<(Type Query, Type Result), Type> _queryHandlerTypeCache = new();
    internal static readonly ConcurrentDictionary<(Type Query, Type Result), Type> _queryBehaviorTypeCache = new();

    internal static readonly ConcurrentDictionary<Type, Type> _voidHandlerTypeCache = new();
    internal static readonly ConcurrentDictionary<Type, Type> _voidBehaviorTypeCache = new();
    
    private record HandlerTypes(Type InputArgument, Type ResultArgument, Type Handler);
    private record VoidHandlerTypes(Type InputArgument, Type Handler);
    private record NotificationTypes(Type InputArgument, Type Handler);

    extension (IServiceCollection services)
    {
        /// <summary>
        /// Scans the given assemblies for command, query, and notification handlers and registers them
        /// with the service collection, along with the <see cref="IDispatcher"/> implementation used to
        /// invoke them. If logging is enabled via <paramref name="configure"/>, matching pipeline logging
        /// behaviors are also registered for commands and/or queries.
        /// </summary>
        /// <param name="configure">
        /// An optional delegate used to configure <see cref="DispatchOptions"/>, such as enabling command
        /// or query logging behaviors. If <see langword="null"/>, default options are used (logging disabled).
        /// </param>
        /// <param name="assemblies">
        /// The assemblies to scan for handler implementations (<see cref="ICommandHandler{TCommand,TResult}"/>,
        /// <see cref="ICommandHandler{TCommand}"/>, <see cref="IQueryHandler{TQuery,TResult}"/>, and
        /// <see cref="INotificationHandler{TNotification}"/>). If <see langword="null"/> or empty, the calling
        /// assembly is scanned instead.
        /// </param>
        /// <returns>The same <see cref="IServiceCollection"/> instance, for chaining.</returns>
        public IServiceCollection AddDispatch(
            Action<DispatchOptions>? configure = null,
            params Assembly[]? assemblies)
        {
            services.AddScoped<IDispatcher, Dispatcher>();
    
            if (assemblies is { Length: 0 } or null)
                assemblies = [Assembly.GetCallingAssembly()];
            
            var options = new DispatchOptions();
            configure?.Invoke(options);
            
            // Find CommandHandlers based on public, non-abstract, classes that implements the ICommandHandler interface
            // Select the Input/Output arguments plus the handler itself
            var commandHandlers = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic)) //NestedPublic included to correctly register UnitTests
                .Where(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Any(i => i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
                .SelectMany(handler => handler.GetInterfaces()
                    .Where(handlerInterface => 
                        handlerInterface.IsGenericType
                        && handlerInterface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                    .Select(handlerInterface => new HandlerTypes(
                            InputArgument: handlerInterface.GetGenericArguments()[0],
                            ResultArgument: handlerInterface.GetGenericArguments()[1],
                            Handler: handler
                        )
                    )
                ).ToArray();
            
            // Find CommandHandlers that are void - No return type.
            // Based on public, non-abstract, classes that implements the ICommandHandler interface
            // Select the Input argument and the handler itself
            var voidCommandHandlers = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic))
                .Where(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Any(i => i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
                .SelectMany(handler => handler.GetInterfaces()
                    .Where(handlerInterface => 
                        handlerInterface.IsGenericType
                        && handlerInterface.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                    .Select(handlerInterface => new VoidHandlerTypes(
                            InputArgument: handlerInterface.GetGenericArguments()[0],
                            Handler: handler
                        )
                    )
                ).ToArray();
            
            // Find QueryHandlers based on public, non-abstract, classes that implements the IQueryHandler interface
            // Select the Input/Output arguments plus the handler itself
            var queryHandlers = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic)) //NestedPublic included to correctly register UnitTests
                .Where(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Any(i => i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .SelectMany(handler => handler.GetInterfaces()
                    .Where(handlerInterface => 
                        handlerInterface.IsGenericType
                        && handlerInterface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                    .Select(handlerInterface => new HandlerTypes(
                            InputArgument: handlerInterface.GetGenericArguments()[0],
                            ResultArgument: handlerInterface.GetGenericArguments()[1],
                            Handler: handler
                        )
                    )
                ).ToArray();
            
            // Find NotificationHandlers based on public, non-abstract, classes that implements the INotification interface
            // Select the Input arguments plus the handler itself
            var notificationHandlers = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic)) //NestedPublic included to correctly register UnitTests
                .Where(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Any(i => i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .SelectMany(handler => handler.GetInterfaces()
                    .Where(handlerInterface => 
                        handlerInterface.IsGenericType
                        && handlerInterface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(handlerInterface => new NotificationTypes(
                            InputArgument: handlerInterface.GetGenericArguments()[0],
                            Handler: handler
                        )
                    )
                ).ToArray();
            
            // Register Commands as scoped based on the scanning
            foreach (var command in commandHandlers)
            {
                // Generate interface of CommandHandler based on the handlers input argument and its return type.
                var handlerInterface = typeof(ICommandHandler<,>)
                    .MakeGenericType(command.InputArgument, command.ResultArgument);

                services.AddScoped(handlerInterface, command.Handler);

                // Cache the closed handler interface type, keyed by (command, result) — this is what Dispatcher looks up.
                _commandHandlerTypeCache.TryAdd((command.InputArgument, command.ResultArgument), handlerInterface);

                // Register behavior wrapper if logging enabled
                if (options.CommandLogging == LoggingMode.Disabled) continue;
                var behaviorType = options.CommandLogging == LoggingMode.All
                    ? typeof(CommandLoggingBehavior<,>)
                    : typeof(CommandOptInLoggingBehavior<,>);

                // Register Command-Pipeline interfaces 
                var behaviorInterface = typeof(ICommandPipelineBehavior<,>)
                    .MakeGenericType(command.InputArgument, command.ResultArgument);
                var behaviorService = behaviorType
                    .MakeGenericType(command.InputArgument, command.ResultArgument);

                services.AddScoped(behaviorInterface, behaviorService);

                // Cache the pipeline behavior interface type too — same lookup pattern in Dispatcher.
                _commandBehaviorTypeCache.TryAdd((command.InputArgument, command.ResultArgument), behaviorInterface);
            }
            
            // Register void Commands as scoped
            foreach (var command in voidCommandHandlers)
            {
                // Generate Interfaces of void CommandHandlers based on handler's Input argument only
                var handlerInterface = typeof(ICommandHandler<>)
                    .MakeGenericType(command.InputArgument);
    
                services.AddScoped(handlerInterface, command.Handler);
    
                // Cache the closed handler interface type, keyed by command type (void commands have no result type).
                _voidHandlerTypeCache.TryAdd(command.InputArgument, handlerInterface);
    
                // Register behavior wrapper if logging enabled - if 'disabled' skip the steps below.
                if (options.CommandLogging == LoggingMode.Disabled) continue;
                var behaviorType = options.CommandLogging == LoggingMode.All
                    ? typeof(CommandLoggingBehavior<>)
                    : typeof(CommandOptInLoggingBehavior<>);
    
                var behaviorInterface = typeof(ICommandPipelineBehavior<>)
                    .MakeGenericType(command.InputArgument);
                var behaviorService = behaviorType
                    .MakeGenericType(command.InputArgument);
    
                services.AddScoped(behaviorInterface, behaviorService);
    
                // Cache the pipeline behavior interface type too — same lookup pattern in Dispatcher.
                _voidBehaviorTypeCache.TryAdd(command.InputArgument, behaviorInterface);
            }
            
            // Register Queries as scoped
            foreach (var query in queryHandlers)
            {
                // Generate interface of QueryHandler based on the handlers input argument and its return type.
                var handlerInterface = typeof(IQueryHandler<,>)
                    .MakeGenericType(query.InputArgument, query.ResultArgument);
                
                services.AddScoped(handlerInterface, query.Handler);
                
                _queryHandlerTypeCache.TryAdd((query.InputArgument, query.ResultArgument), handlerInterface);
    
                // Register behavior wrapper if logging enabled - if 'disabled' skip the steps below.
                if (options.QueryLogging == LoggingMode.Disabled) continue;
                var behaviorType = options.QueryLogging == LoggingMode.All
                    ? typeof(QueryLoggingBehavior<,>)
                    : typeof(QueryOptInLoggingBehavior<,>);
    
                var behaviorInterface = typeof(IQueryPipelineBehavior<,>)
                    .MakeGenericType(query.InputArgument, query.ResultArgument);
                var behaviorService = behaviorType
                    .MakeGenericType(query.InputArgument, query.ResultArgument);
    
                services.AddScoped(behaviorInterface, behaviorService);
                
                // Cache the pipeline behavior interface type too — same lookup pattern in Dispatcher.
                _queryBehaviorTypeCache.TryAdd((query.InputArgument, query.ResultArgument), behaviorInterface);
            }
            
            // Register Notifications as scoped
            foreach (var notification in notificationHandlers)
            {
                // Existing handler registration
                var handlerInterface = typeof(INotificationHandler<>)
                    .MakeGenericType(notification.InputArgument);
    
                services.AddScoped(handlerInterface, notification.Handler);
    
                _notificationHandlerTypeCache.TryAdd(notification.InputArgument, handlerInterface);
            }
            
            return services;
        }
    
        /// <summary>
        /// Registers a command pipeline behavior in the service collection, resolving the closed
        /// <see cref="ICommandPipelineBehavior{TCommand,TResult}"/> interface implemented by
        /// <typeparamref name="TBehavior"/> and registering it with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TBehavior">
        /// The behavior type to register. Must implement <see cref="ICommandPipelineBehavior{TCommand,TResult}"/>.
        /// </typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> instance, for chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="TBehavior"/> does not implement <see cref="ICommandPipelineBehavior{TCommand,TResult}"/>.
        /// </exception>
        public IServiceCollection AddCommandBehavior<TBehavior>()
            where TBehavior : class
        {
            var behaviorInterface = typeof(TBehavior)
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandPipelineBehavior<,>))
            ?? throw new InvalidOperationException($"{typeof(TBehavior).Name} does not implement ICommandPipelineBehavior<,>");

            // Register behavior
            services.AddScoped(behaviorInterface, typeof(TBehavior));

            // Cache the closed behavior interface type, keyed by (command, result) args from the resolved interface.
            var genericArgs = behaviorInterface.GetGenericArguments();
            _commandBehaviorTypeCache.TryAdd((genericArgs[0], genericArgs[1]), behaviorInterface);

            return services;
        }

        /// <summary>
        /// Registers a query pipeline behavior in the service collection, resolving the closed
        /// <see cref="IQueryPipelineBehavior{TQuery,TResult}"/> interface implemented by
        /// <typeparamref name="TBehavior"/> and registering it with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TBehavior">
        /// The behavior type to register. Must implement <see cref="IQueryPipelineBehavior{TQuery,TResult}"/>.
        /// </typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> instance, for chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="TBehavior"/> does not implement <see cref="IQueryPipelineBehavior{TQuery,TResult}"/>.
        /// </exception>
        public IServiceCollection AddQueryBehavior<TBehavior>()
            where TBehavior : class
        {
            var behaviorInterface = typeof(TBehavior)
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryPipelineBehavior<,>)) 
            ?? throw new InvalidOperationException($"{typeof(TBehavior).Name} does not implement IQueryPipelineBehavior<,>");
            // Register behavior
            services.AddScoped(behaviorInterface, typeof(TBehavior));

            // Cache the closed behavior interface type, keyed by (query, result) args from the resolved interface.
            var genericArgs = behaviorInterface.GetGenericArguments();
            _queryBehaviorTypeCache.TryAdd((genericArgs[0], genericArgs[1]), behaviorInterface);

            return services;
        }
    }
}
