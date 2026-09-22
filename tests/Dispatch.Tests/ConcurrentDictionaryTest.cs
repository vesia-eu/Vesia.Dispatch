using Microsoft.Extensions.DependencyInjection;
using Vesia.Dispatch.Tests.Fakes.Dispatcher;

namespace Vesia.Dispatch.Tests;

public class ConcurrentDictionaryTest
{
    private readonly ServiceProvider _provider;
    private readonly IDispatcher _dispatcher;

    public ConcurrentDictionaryTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CallTracker>();
        services.AddDispatch(null, typeof(DispatcherTest).Assembly);

        _provider = services.BuildServiceProvider();
        _dispatcher = _provider.GetRequiredService<IDispatcher>();
    }

    [Fact]
    public void AddDispatch_PopulatesCommandHandlerCache()
    {
        var found = ServiceCollectionExtensions._commandHandlerTypeCache
            .TryGetValue((typeof(TestCommand), typeof(string)), out var handlerType);

        Assert.True(found);
        Assert.Equal(typeof(ICommandHandler<TestCommand, string>), handlerType);
    }

    [Fact]
    public void AddDispatch_PopulatesVoidHandlerCache()
    {
        var found = ServiceCollectionExtensions._voidHandlerTypeCache
            .TryGetValue(typeof(VoidCommand), out var handlerType);

        Assert.True(found);
        Assert.Equal(typeof(ICommandHandler<VoidCommand>), handlerType);
    }

    [Fact]
    public void AddDispatch_PopulatesQueryHandlerCache()
    {
        var found = ServiceCollectionExtensions._queryHandlerTypeCache
            .TryGetValue((typeof(TestQuery), typeof(string)), out var handlerType);

        Assert.True(found);
        Assert.Equal(typeof(IQueryHandler<TestQuery, string>), handlerType);
    }

    [Fact]
    public async Task DispatchAsync_CommandUsesCachedType_AndReturnsExpectedResult()
    {
        var result = await _dispatcher.DispatchAsync(new TestCommand());

        Assert.Equal("correct!", result);
    }

    [Fact]
    public async Task DispatchAsync_QueryUsesCachedType_AndReturnsExpectedResult()
    {
        var result = await _dispatcher.DispatchAsync(new TestQuery());

        Assert.Equal("correct!", result);
    }

    [Fact]
    public async Task DispatchAsync_VoidCommandUsesCachedType_AndInvokesHandler()
    {
        await _dispatcher.DispatchAsync(new VoidCommand());

        var tracker = _provider.GetRequiredService<CallTracker>();
        Assert.True(tracker.WasCalled);
    }

    [Fact]
    public async Task DispatchAsync_RepeatedCalls_UseCacheWithoutChangingBehavior()
    {
        var first = await _dispatcher.DispatchAsync(new TestCommand());
        var second = await _dispatcher.DispatchAsync(new TestCommand());

        Assert.Equal(first, second);
        Assert.Equal("correct!", second);
    }
}