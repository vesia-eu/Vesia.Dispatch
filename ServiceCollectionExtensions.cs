using System.Reflection;
using Venly.Dispatch.Extensions;
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
            
            var interfaceWrapper= typeof(ICommandHandlerWrapper<>)
                .MakeGenericType(command.ResultArgument);
            
            var wrapper = typeof(CommandHandlerWrapper<,>)
                .MakeGenericType(command.InputArgument, command.ResultArgument);
            
            services.AddScoped(interfaceWrapper, wrapper);
        }
        
        //Register Queries + QueryWrappers as scoped
        foreach (var query in queryHandlers)
        {
            var genericType = typeof(IQueryHandler<,>)
                .MakeGenericType(query.InputArgument, query.ResultArgument);

            services.AddScoped(genericType, query.Handler);
            
            var interfaceWrapper= typeof(IQueryHandlerWrapper<>)
                .MakeGenericType(query.ResultArgument);
            
            var wrapper = typeof(QueryHandlerWrapper<,>)
                .MakeGenericType(query.InputArgument, query.ResultArgument);
            
            services.AddScoped(interfaceWrapper, wrapper);
        }
        
        //Register Behaviors
        
        
        //Register Logging Behaviors
        switch (options.CommandLogging)
        {
            case LoggingMode.All:
                services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>));
                break;
            case LoggingMode.OptIn:
                // register a behavior that checks for [Logged] attribute
                break;
            case LoggingMode.Disabled:
                break;
        }
        
        switch (options.QueryLogging)
        {
            case LoggingMode.All:
                services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(QueryLoggingBehavior<,>));
                break;
            case LoggingMode.OptIn:
                // register a behavior that checks for [Logged] attribute
                break;
            case LoggingMode.Disabled:
                break;
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

        var args = behaviorInterface.GetGenericArguments();
        var inputArgument = args[0];
        var resultArgument = args[1];

        // register behavior
        services.AddScoped(behaviorInterface, typeof(TBehavior));

        // register wrapper
        var wrapperInterface = typeof(ICommandPipelineBehaviorWrapper<>).MakeGenericType(resultArgument);
        var wrapper = typeof(CommandPipelineBehaviorWrapper<,>).MakeGenericType(inputArgument, resultArgument);
        services.AddScoped(wrapperInterface, wrapper);

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

        var args = behaviorInterface.GetGenericArguments();
        var inputArgument = args[0];
        var resultArgument = args[1];

        // register behavior
        services.AddScoped(behaviorInterface, typeof(TBehavior));

        // register wrapper
        var wrapperInterface = typeof(IQueryPipelineBehaviorWrapper<>).MakeGenericType(resultArgument);
        var wrapper = typeof(QueryPipelineBehaviorWrapper<,>).MakeGenericType(inputArgument, resultArgument);
        services.AddScoped(wrapperInterface, wrapper);

        return services;
    }
}