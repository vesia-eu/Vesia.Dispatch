using Venly.Dispatch.Interfaces;
using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.Tests.Fakes.Dispatcher;

public record TestQuery : IQuery<string>;

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult("correct!");
}