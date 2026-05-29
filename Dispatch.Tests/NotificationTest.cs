using Microsoft.Extensions.DependencyInjection;
using Vesia.Dispatch.Interfaces;
using Vesia.Dispatch.Tests.Fakes.Notification;

namespace Vesia.Dispatch.Tests;

public class NotificationTest
{
    private readonly IDispatcher _dispatcher;
    
    public NotificationTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDispatch(null, typeof(NotificationTest).Assembly);
        _dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task DispatchCommandTest()
    {
        var notification = new TestNotification(); 
        await _dispatcher.PublishAsync(notification);
        
        Assert.Contains("handled1", TestNotificationFirstHandler.ExecutionLog);
        Assert.Contains("handled2", TestNotificationSecondHandler.ExecutionLog);
    }
}