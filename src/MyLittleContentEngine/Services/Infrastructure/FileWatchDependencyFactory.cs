
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// A dependency factory that manages service instance lifetime with file-watch invalidation.
/// Instances are cached until file changes are detected, providing optimal performance
/// while ensuring fresh instances when content changes.
/// Integrates with IContentEngineFileWatcher for automatic cache invalidation.
/// </summary>
/// <typeparam name="T">The service type to manage.</typeparam>
internal sealed class FileWatchDependencyFactory<T> : IDisposable where T : class
{
    private readonly Func<IServiceProvider, T> _serviceFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileWatchDependencyFactory<T>> _logger;
    private readonly Lock _lock = new();

    private T? _instance;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FileWatchDependencyFactory.
    /// </summary>
    /// <param name="fileWatcher">The file watcher service for invalidation triggers.</param>
    /// <param name="serviceFactory">Factory function for creating service instances.</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public FileWatchDependencyFactory(
        IContentEngineFileWatcher fileWatcher,
        Func<IServiceProvider, T> serviceFactory,
        IServiceProvider serviceProvider,
        ILogger<FileWatchDependencyFactory<T>> logger )
    {
        _serviceFactory = serviceFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Subscribe to file changes for invalidation
        fileWatcher.SubscribeToChanges(InvalidateInstance);
        
        _logger.LogDebug("FileWatchDependencyFactory<{ServiceType}> initialized with file-watch invalidation",
            typeof(T).Name);
    }

    /// <summary>
    /// Gets the current service instance, creating a new one if needed.
    /// Instances are cached until invalidated by file changes.
    /// </summary>
    /// <returns>The current or newly created service instance.</returns>
    public T GetInstance()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(FileWatchDependencyFactory<T>));
            if (_instance == null)
            {
                CreateNewInstance();
            }

            return _instance!;

            // Create instance if needed
        }
    }

    /// <summary>
    /// Manually invalidates the current instance, forcing creation of a new one on next access.
    /// </summary>
    public void InvalidateInstance()
    {
        lock (_lock)
        {
            if (_disposed || _instance == null) return;

            _logger.LogDebug("Invalidating {ServiceType} instance due to file change", typeof(T).Name);
            DisposeCurrentInstance();
        }
    }

    private void CreateNewInstance()
    {
        _logger.LogDebug("Creating new {ServiceType} instance", typeof(T).Name);
        
        // The factory function handles creating the instance without circular dependencies
        _instance = _serviceFactory(_serviceProvider);
    }

    private void DisposeCurrentInstance()
    {
        if (_instance is IDisposable disposableInstance)
        {
            try
            {
                disposableInstance.Dispose();
                _logger.LogDebug("Disposed {ServiceType} instance", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing {ServiceType} instance", typeof(T).Name);
            }
        }

        _instance = null;
    }

    /// <summary>
    /// Releases all resources used by this factory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_disposed) return;

            DisposeCurrentInstance();
            _disposed = true;
        }

        _logger.LogDebug("FileWatchDependencyFactory<{ServiceType}> disposed", typeof(T).Name);
    }
}