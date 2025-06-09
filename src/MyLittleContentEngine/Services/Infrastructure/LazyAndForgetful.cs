namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// A lazy-loading cache that can "forget" its value and reload it on demand with debounced refresh semantics.
/// This is particularly useful for expensive operations that need to be invalidated and recomputed when their dependencies change
/// but should not be recomputed too frequently when multiple invalidation requests occur in rapid succession.
/// </summary>
/// <typeparam name="T">The type of value to cache and lazily compute.</typeparam>
/// <param name="factory">The factory function that computes the cached value. This should be an expensive operation worth caching.</param>
/// <param name="debounceDelay">The delay to wait after a refresh request before actually executing the factory. Defaults to 50 ms.</param>
internal class LazyAndForgetful<T>(Func<Task<T>> factory, TimeSpan? debounceDelay = null) : IDisposable
{
    private readonly Func<Task<T>> _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Lock _debounceLock = new();
    private readonly TimeSpan _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(50);

    private CancellationTokenSource? _debounceCts;
    private volatile Task<T>? _valueTask;

    /// <summary>
    /// Tracks any in-progress debounced refresh operation, allowing Value getters to wait for completion.
    /// </summary>
    private volatile Task? _refreshTask;

    /// <summary>
    /// Gets the cached value asynchronously.
    /// If the value hasn't been computed yet, invokes the factory function to compute it.
    /// If a refresh operation is in progress, waits for it to complete before returning the value.
    /// </summary>
    /// <returns>A task that represents the cached value.</returns>
    public Task<T> Value => GetValueAsync();

    /// <summary>
    /// Schedules a debounced refresh of the cached value.
    /// Multiple calls within the debounce window will be coalesced into a single refresh operation
    /// that executes after the debounce delay has elapsed since the last call.
    /// This is the "forgetful" part - it forgets the current value and recomputes it.
    /// </summary>
    public void Refresh()
    {
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            // Start the debounced refresh operation
            _refreshTask = DebouncedRefreshAsync(_debounceCts.Token);
        }
    }

    /// <summary>
    /// Implements the debounced refresh logic. Waits for the debounce delay, then performs the actual refresh.
    /// </summary>
    private async Task DebouncedRefreshAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_debounceDelay, token).ConfigureAwait(false);
            await PerformRefreshAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Another Refresh() call cancelled this operation - this is expected behavior
        }
    }

    /// <summary>
    /// Performs the actual refresh by invoking the factory function and updating the cached value.
    /// This method is thread-safe and ensures that only one refresh operation can be executed at a time.
    /// </summary>
    private async Task PerformRefreshAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newTask = _factory();
            // Await the factory task before assigning it to prevent caching a faulted task
            await newTask.ConfigureAwait(false);
            _valueTask = newTask;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the cached value using a double-checked locking pattern for thread safety.
    /// Waits for any in-progress refresh operations before returning the value.
    /// </summary>
    private async Task<T> GetValueAsync()
    {
        // First, wait for any in-flight refresh to complete
        var refreshTask = _refreshTask;
        if (refreshTask != null)
        {
            await refreshTask.ConfigureAwait(false);
        }

        // Check for initialized value using a double-checked locking pattern
        var valueTask = _valueTask;
        if (valueTask != null)
        {
            return await valueTask.ConfigureAwait(false);
        }

        // Value hasn't been initialized yet, acquire lock and initialize
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check: another thread might have initialized it while we waited
            if (_valueTask == null)
            {
                // This is the first access and no refresh has been called yet
                _valueTask = _factory();
            }
            return await _valueTask.ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Releases all resources used by the LazyAndForgetful instance.
    /// Cancels any pending refresh operations and disposes of synchronization primitives.
    /// </summary>
    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}