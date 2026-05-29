using Vesia.Dispatch.Interfaces;

namespace Vesia.Dispatch.Tests.Fakes.Notification;

public record TestNotification;


public class TestNotificationFirstHandler : INotificationHandler<TestNotification>
{
    public static readonly List<string> ExecutionLog = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("handled1");
        return Task.CompletedTask;
    }
}

public class TestNotificationSecondHandler : INotificationHandler<TestNotification>
{
    public static readonly List<string> ExecutionLog = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("handled2");
        return Task.CompletedTask;
    }
}