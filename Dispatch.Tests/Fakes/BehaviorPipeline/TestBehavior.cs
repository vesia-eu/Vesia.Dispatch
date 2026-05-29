using Vesia.Dispatch.Attributes;
using Vesia.Dispatch.Interfaces;
using Vesia.Dispatch.Interfaces.Behavior;
using Vesia.Dispatch.Interfaces.Messaging;
using Vesia.Dispatch.Tests.Fakes.Dispatcher;

namespace Vesia.Dispatch.Tests.Fakes.BehaviorPipeline;

[Logged]
public record TestAttributeLoggingQuery : IQuery<string>;

public record TestAttributeNoLoggingQuery : IQuery<string>;

public class TestAttributeLoggingHandler : IQueryHandler<TestAttributeLoggingQuery, string>
{
    public Task<string> Handle(TestAttributeLoggingQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult("logged!");
}

public class TestAttributeNoLoggingHandler : IQueryHandler<TestAttributeNoLoggingQuery, string>
{
    public Task<string> Handle(TestAttributeNoLoggingQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult("not logged!");
}

public class TrackingBehavior : ICommandPipelineBehavior<TestCommand, string>
{
    public static readonly List<string> ExecutionLog = new();

    public async Task<string> Handle(TestCommand command, Func<Task<string>> next, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("before");
        var result = await next();
        ExecutionLog.Add("after");
        return result;
    }
}