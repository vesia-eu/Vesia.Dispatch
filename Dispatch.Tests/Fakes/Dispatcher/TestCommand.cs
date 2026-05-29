using Vesia.Dispatch.Interfaces;
using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.Tests.Fakes.Dispatcher;

public record TestCommand : ICommand<string>;

public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<string> Handle(TestCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult("correct!");
}