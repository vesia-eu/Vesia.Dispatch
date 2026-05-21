using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Venly.Dispatch.Enums;
using Venly.Dispatch.Interfaces;
using Venly.Dispatch.PipelineBehaviors;
using Venly.Dispatch.Tests.Fakes.BehaviorPipeline;

namespace Venly.Dispatch.Tests;

public class BehaviorTest
{
    private readonly IDispatcher _dispatcher;
    private readonly FakeLogger<QueryOptInLoggingBehavior<TestAttributeLoggingQuery, string>> _fakeLogger;
    private readonly FakeLogger<QueryOptInLoggingBehavior<TestAttributeNoLoggingQuery, string>> _fakeLoggerNoLogs;

    public BehaviorTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDispatch(options => options.QueryLogging = LoggingMode.OptIn, typeof(BehaviorTest).Assembly);
        
        _fakeLogger = new FakeLogger<QueryOptInLoggingBehavior<TestAttributeLoggingQuery, string>>();
        _fakeLoggerNoLogs = new FakeLogger<QueryOptInLoggingBehavior<TestAttributeNoLoggingQuery, string>>();
        
        services.AddSingleton<ILogger<QueryOptInLoggingBehavior<TestAttributeLoggingQuery, string>>>(_fakeLogger);
        services.AddSingleton<ILogger<QueryOptInLoggingBehavior<TestAttributeNoLoggingQuery, string>>>(_fakeLoggerNoLogs);
        _dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task DispatchOptInTest()
    {
        var query = new TestAttributeLoggingQuery();
        await _dispatcher.DispatchAsync(query);
        Assert.NotEmpty(_fakeLogger.Collector.GetSnapshot());
    }
    
    [Fact]
    public async Task DispatchOptInNoLogTest()
    {
        var query = new TestAttributeNoLoggingQuery();
        await _dispatcher.DispatchAsync(query);
        Assert.Empty(_fakeLoggerNoLogs.Collector.GetSnapshot());
    }
}