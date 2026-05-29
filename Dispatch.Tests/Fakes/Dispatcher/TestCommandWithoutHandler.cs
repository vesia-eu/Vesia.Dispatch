using Vesia.Dispatch.Interfaces.Messaging;

namespace Vesia.Dispatch.Tests.Fakes.Dispatcher;

public record TestCommandWithoutHandler : ICommand<string>;