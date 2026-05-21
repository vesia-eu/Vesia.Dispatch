using System.Reflection;
using Venly.Dispatch.Interfaces;
using Venly.Dispatch.Services;
using Microsoft.Extensions.DependencyInjection;
using Venly.Dispatch.Enums;
using Venly.Dispatch.Interfaces.Behavior;
using Venly.Dispatch.PipelineBehaviors;

namespace Venly.Dispatch;

public static class ServiceCollectionExtensions
{
    private record HandlerTypes(Type InputArgument, Type ResultArgument, Type Handler);
    
    public static IServiceCollection AddDispatch(
        this IServiceCollection services,
        Action<DispatchOptions>? configure = null,
        params Assembly[]? assemblies)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        if (assemblies is { Length: 0 } or null)
            assemblies = [Assembly.GetCallingAssembly()];
        
        var options = new DispatchOptions();
        configure?.Invoke(options);
        
        //Find CommandHandlers based on public, non-abstract, classes that implements the ICommandHandler interface
        //Select the Input/Output arguments plus the handler itself
        var commandHandlers = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic))
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
            )
            .ToArray();
        //Find QueryHandlers based on public, non-abstract, classes that implements the IQueryHandler interface
        //Select the Input/Output arguments plus the handler itself
        var queryHandlers = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic))
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
            )
            .ToArray();

        //Register Commands + CommandWrappers as scoped
        foreach (var command in commandHandlers)
        {
            var genericType = typeof(ICommandHandler<,>)
                .MakeGenericType(command.InputArgument, command.ResultArgument);

            services.AddScoped(genericType, command.Handler);
            
            // register behavior wrapper if logging enabled
            if (options.CommandLogging == LoggingMode.Disabled) continue;
            var behaviorType = options.CommandLogging == LoggingMode.All
                ? typeof(CommandLoggingBehavior<,>)
                : typeof(CommandOptInLoggingBehavior<,>);
            
            var behaviorInterface = typeof(ICommandPipelineBehavior<,>)
                .MakeGenericType(command.InputArgument, command.ResultArgument);
            var closedBehavior = behaviorType
                .MakeGenericType(command.InputArgument, command.ResultArgument);
            
            services.AddScoped(behaviorInterface, closedBehavior);
        }
        
        //Register Queries + QueryWrappers as scoped
        foreach (var query in queryHandlers)
        {
            // existing handler registration
            var genericType = typeof(IQueryHandler<,>)
                .MakeGenericType(query.InputArgument, query.ResultArgument);
            services.AddScoped(genericType, query.Handler);

            // register behavior wrapper if logging enabled
            if (options.QueryLogging == LoggingMode.Disabled) continue;
            var behaviorType = options.QueryLogging == LoggingMode.All
                ? typeof(QueryLoggingBehavior<,>)
                : typeof(QueryOptInLoggingBehavior<,>);

            var behaviorInterface = typeof(IQueryPipelineBehavior<,>)
                .MakeGenericType(query.InputArgument, query.ResultArgument);
            var closedBehavior = behaviorType
                .MakeGenericType(query.InputArgument, query.ResultArgument);

            services.AddScoped(behaviorInterface, closedBehavior);
        }
        
        
        
        return services;
    }

    public static IServiceCollection AddCommandBehavior<TBehavior>(
        this IServiceCollection services)
        where TBehavior : class
    {
        var behaviorInterface = typeof(TBehavior)
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                 i.GetGenericTypeDefinition() == typeof(ICommandPipelineBehavior<,>))
        ?? throw new InvalidOperationException($"{typeof(TBehavior).Name} does not implement ICommandPipelineBehavior<,>");
        
        // register behavior
        services.AddScoped(behaviorInterface, typeof(TBehavior));

        return services;
    }
    
    public static IServiceCollection AddQueryBehavior<TBehavior>(
        this IServiceCollection services)
        where TBehavior : class
    {
        var behaviorInterface = typeof(TBehavior)
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                 i.GetGenericTypeDefinition() == typeof(IQueryPipelineBehavior<,>)) 
                                ?? throw new InvalidOperationException($"{typeof(TBehavior).Name} does not implement IQueryPipelineBehavior<,>");

        // register behavior
        services.AddScoped(behaviorInterface, typeof(TBehavior));

        return services;
    }
}