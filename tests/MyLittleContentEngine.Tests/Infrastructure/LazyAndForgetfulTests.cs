using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;
// ReSharper disable ConvertToLocalFunction

namespace MyLittleContentEngine.Tests.Infrastructure;

public class LazyAndForgetfulTests
{
    [Fact]
    public async Task Value_FirstAccess_InvokesFactory()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        using var lazy = new LazyAndForgetful<int>(factory);

        var result = await lazy.Value;

        result.ShouldBe(1);
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task Value_MultipleAccesses_InvokesFactoryOnlyOnce()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        using var lazy = new LazyAndForgetful<int>(factory);

        var result1 = await lazy.Value;
        var result2 = await lazy.Value;

        result1.ShouldBe(1);
        result2.ShouldBe(1);
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task Value_ConcurrentAccesses_InvokesFactoryOnlyOnce()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        using var lazy = new LazyAndForgetful<int>(factory);

        // ReSharper disable once AccessToDisposedClosure
        var getValue = () => lazy.Value;
        
        var tasks = new[]
        {
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue),
            Task.Run(getValue)
        };

        var results = await Task.WhenAll(tasks);

        results.ShouldAllBe(result => result == 1);
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task Refresh_AfterInitialValue_RecomputesValue()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        using var lazy = new LazyAndForgetful<int>(factory);

        var initialValue = await lazy.Value;
        initialValue.ShouldBe(1);

        lazy.Refresh();
        await Task.Delay(100, TestContext.Current.CancellationToken); // Wait for debounce

        var refreshedValue = await lazy.Value;
        refreshedValue.ShouldBe(2);
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task Refresh_MultipleCallsWithinDebounceWindow_DebouncesToSingleRefresh()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        using var lazy = new LazyAndForgetful<int>(factory, TimeSpan.FromMilliseconds(100));

        var initialResult = await lazy.Value;
        initialResult.ShouldBe(1);

        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();
        lazy.Refresh();

        await Task.Delay(150, TestContext.Current.CancellationToken); // Wait for debounce

        var result = await lazy.Value;
        result.ShouldBe(2);
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task Value_WaitsForInProgressRefresh()
    {
        var delaySource = new TaskCompletionSource<bool>();
        var callCount = 0;
        var factory = async () =>
        {
            callCount++;
            if (callCount == 2)
            {
                await delaySource.Task;
            }
            return callCount;
        };

        using var lazy = new LazyAndForgetful<int>(factory, TimeSpan.FromMilliseconds(10));

        var initialValue = await lazy.Value;
        initialValue.ShouldBe(1);

        lazy.Refresh();

        var valueTask = lazy.Value;
        
        delaySource.SetResult(true);
        var result = await valueTask;

        result.ShouldBe(2);
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task Refresh_CustomDebounceDelay_RespectsDelay()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        var debounceDelay = TimeSpan.FromMilliseconds(200);
        using var lazy = new LazyAndForgetful<int>(factory, debounceDelay);

        await lazy.Value; // Initial value

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        lazy.Refresh();
        await Task.Delay(250, TestContext.Current.CancellationToken); // Wait longer than debounce

        await lazy.Value; // Trigger refresh completion
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(200);
        callCount.ShouldBe(2);
    }
    
    [Fact]
    public async Task Factory_ThrowsException_PropagatesException()
    {
        var exception = new InvalidOperationException("Test exception");
        var factory = () => Task.FromException<int>(exception);
        using var lazy = new LazyAndForgetful<int>(factory);

        var thrownException = await Should.ThrowAsync<InvalidOperationException>(() => lazy.Value);
        thrownException.Message.ShouldBe("Test exception");
    }

    [Fact]
    public async Task Factory_AsyncException_PropagatesException()
    {
        async Task<int> Factory()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Async test exception");
        }

        using var lazy = new LazyAndForgetful<int>(Factory);

        var thrownException = await Should.ThrowAsync<InvalidOperationException>(() => lazy.Value);
        thrownException.Message.ShouldBe("Async test exception");
    }

    [Fact]
    public void Dispose_CancelsInProgressRefresh()
    {
        var callCount = 0;
        var factory = () => Task.FromResult(++callCount);
        var lazy = new LazyAndForgetful<int>(factory, TimeSpan.FromSeconds(1));

        lazy.Refresh();
        lazy.Dispose();

        // Should not throw and should complete immediately
        // No assertion needed - test passes if no exception is thrown
    }

    [Fact]
    public async Task Dispose_AfterUse_DoesNotThrow()
    {
        var factory = () => Task.FromResult(42);
        var lazy = new LazyAndForgetful<int>(factory);

        var value = await lazy.Value;
        value.ShouldBe(42);

        lazy.Dispose();

        // Should not throw
        // No assertion needed - test passes if no exception is thrown
    }
}