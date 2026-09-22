using Microsoft.Extensions.DependencyInjection;
using Vesia.Dispatch.Exceptions;
using Vesia.Dispatch.Tests.Fakes.Dispatcher;

namespace Vesia.Dispatch.Tests;

public class DispatcherTest
{
    private readonly IDispatcher _dispatcher;
    private readonly CallTracker _tracker;

    public DispatcherTest()
    {
        _tracker = new CallTracker();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDispatch(null, typeof(DispatcherTest).Assembly);
        services.AddSingleton(_tracker);

        _dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }
    
    [Fact]
    public async Task DispatchCommandTest()
    {
        var command = new TestCommand();
        var result = await _dispatcher.DispatchAsync(command);
        Assert.Same("correct!", result);
    }
    
    [Fact]
    public async Task DispatchVoidCommandTest()
    {
        var command = new VoidCommand();
        await _dispatcher.DispatchAsync(command);

        Assert.True(_tracker.WasCalled);
    }
    
    [Fact]
    public async Task DispatchVoidCommandNoHandlerTest()
    {
        var wrongTestCommand = new VoidCommandNoHandler();
        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => _dispatcher.DispatchAsync(wrongTestCommand));
    }
    
    [Fact]
    public async Task DispatchQueryTest()
    {
        var query = new TestQuery();
        var result = await _dispatcher.DispatchAsync(query);
        Assert.Same("correct!", result);
    }
    
    [Fact]
    public async Task DispatchCommandWithoutHandlerTest()
    {
        var wrongTestCommand = new TestCommandWithoutHandler();
        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => _dispatcher.DispatchAsync(wrongTestCommand));
    }
    
    [Fact]
    public async Task DispatchQueryWithoutHandlerTest()
    {
        var queryWithoutHandler = new TestQueryWithoutHandler();
        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => _dispatcher.DispatchAsync(queryWithoutHandler));
    }
}