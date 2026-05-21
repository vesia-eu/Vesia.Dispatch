using Venly.Dispatch.Interfaces.Messaging;

namespace Venly.Dispatch.Tests.Fakes.Dispatcher;

public record TestQueryWithoutHandler : IQuery<string>;