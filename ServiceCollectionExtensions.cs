using System.Reflection;
using Venly.Dispatch.Extensions;
using Venly.Dispatch.Interfaces;
using Venly.Dispatch.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Venly.Dispatch;

public static class ServiceCollectionExtensions
{
    private record HandlerTypes(Type InputArgument, Type ResultArgument, Type Handler);
    
    public static IServiceCollection AddDispatch(this IServiceCollection services, params Assembly[]? assemblies)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        if (assemblies is { Length: 0 } or null)
            assemblies = [Assembly.GetCallingAssembly()];
        
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
        
        return services;
    }
}