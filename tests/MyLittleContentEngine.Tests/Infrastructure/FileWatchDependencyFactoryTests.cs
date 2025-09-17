using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class FileWatchDependencyFactoryTests
{
    private readonly TestContentEngineFileWatcher _fileWatcher;
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<FileWatchDependencyFactory<TestService>> _logger;

    public FileWatchDependencyFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddTransient<TestService>(); // Use transient to get new instances
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        
        _fileWatcher = new TestContentEngineFileWatcher();
        _logger = _serviceProvider.GetRequiredService<ILogger<FileWatchDependencyFactory<TestService>>>();
    }

    [Fact]
    public void GetInstance_ShouldCreateNewInstance_WhenCalledFirstTime()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        // Act
        var instance = factory.GetInstance();

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<TestService>();
    }

    [Fact]
    public void GetInstance_ShouldReturnSameInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        // Act
        var instance1 = factory.GetInstance();
        var instance2 = factory.GetInstance();

        // Assert
        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void GetInstance_ShouldReturnSameInstance_WhenNoFileChanges()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        var instance1 = factory.GetInstance();

        // Act - wait without any file changes
        Thread.Sleep(50);
        var instance2 = factory.GetInstance();

        // Assert
        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void GetInstance_ShouldCreateNewInstance_WhenFileChangeOccurs()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        var instance1 = factory.GetInstance();

        // Act - trigger file change
        _fileWatcher.TriggerChange();
        var instance2 = factory.GetInstance();

        // Assert
        instance1.ShouldNotBeSameAs(instance2);
    }

    [Fact]
    public void InvalidateInstance_ShouldForceNewInstanceOnNextAccess()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        var instance1 = factory.GetInstance();

        // Act
        factory.InvalidateInstance();
        var instance2 = factory.GetInstance();

        // Assert
        instance1.ShouldNotBeSameAs(instance2);
    }

    [Fact]
    public void Dispose_ShouldDisposeCurrentInstance_WhenInstanceIsDisposable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DisposableTestService>(); // Use transient to get new instances
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();

        Func<IServiceProvider, DisposableTestService> serviceFactory = _ => serviceProvider.GetRequiredService<DisposableTestService>();
        using var factory = new FileWatchDependencyFactory<DisposableTestService>(
            _fileWatcher, serviceFactory, serviceProvider,
            serviceProvider.GetRequiredService<ILogger<FileWatchDependencyFactory<DisposableTestService>>>());

        var instance = factory.GetInstance();

        // Act
        factory.Dispose();

        // Assert
        instance.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void GetInstance_ShouldThrowObjectDisposedException_WhenFactoryDisposed()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        factory.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => factory.GetInstance());
    }

    [Fact]
    public void Constructor_ShouldSubscribeToFileWatcher()
    {
        // Arrange & Act
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        // Assert
        _fileWatcher.SubscribedCallbacks.ShouldContain(callback => callback != null);
    }

    [Fact]
    public async Task GetInstance_ShouldBeThreadSafe_WhenAccessedConcurrently()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var instances = new ConcurrentBag<TestService>();
        var tasks = new List<Task>();

        // Act - create multiple threads accessing the factory concurrently
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var instance = factory.GetInstance();
                    instances.Add(instance);
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert - all instances should be the same (no file changes occurred)
        var uniqueInstances = instances.Distinct().ToArray();
        uniqueInstances.Length.ShouldBe(1);
        instances.Count.ShouldBe(threadCount * iterationsPerThread);
    }

    [Fact]
    public async Task InvalidateInstance_ShouldBeThreadSafe_WhenCalledConcurrentlyWithGetInstance()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        const int operationCount = 100;
        var instances = new ConcurrentBag<TestService>();
        var tasks = new List<Task>();

        // Act - multiple tasks getting instances and invalidating concurrently
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < operationCount; j++)
                {
                    var instance = factory.GetInstance();
                    instances.Add(instance);
                    
                    if (j % 10 == 0) // Invalidate occasionally
                    {
                        factory.InvalidateInstance();
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert - should not crash and should have created multiple instances due to invalidation
        instances.Count.ShouldBeGreaterThan(0);
        var uniqueInstances = instances.Distinct().ToArray();
        uniqueInstances.Length.ShouldBeGreaterThan(1); // Multiple instances due to invalidation
    }

    [Fact]
    public async Task FileWatchInvalidation_ShouldBeThreadSafe_WhenTriggeredConcurrently()
    {
        // Arrange
        Func<IServiceProvider, TestService> serviceFactory = _ => _serviceProvider.GetRequiredService<TestService>();
        using var factory = new FileWatchDependencyFactory<TestService>(
            _fileWatcher, serviceFactory, _serviceProvider, _logger);

        const int threadCount = 5;
        var instances = new ConcurrentBag<TestService>();
        var tasks = new List<Task>();

        // Act - multiple threads getting instances while file changes are triggered
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 20; j++)
                {
                    var instance = factory.GetInstance();
                    instances.Add(instance);

                    // Trigger file change occasionally
                    if (j % 5 == 0)
                    {
                        _fileWatcher.TriggerChange();
                    }

                    Thread.Sleep(10);
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert - should have created multiple instances due to file invalidation
        instances.Count.ShouldBe(threadCount * 20);
        var uniqueInstances = instances.Distinct().ToArray();
        uniqueInstances.Length.ShouldBeGreaterThan(1);
    }
}

// Test helper classes
public class TestService
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class DisposableTestService : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

// Test double for IContentEngineFileWatcher
public class TestContentEngineFileWatcher : IContentEngineFileWatcher
{
    private readonly List<Action> _callbacks = [];

    public IReadOnlyList<Action> SubscribedCallbacks => _callbacks.AsReadOnly();

    public void AddPathWatch(string path, string filePattern, Action<string> onFileChanged, bool includeSubdirectories = true)
    {
        // Not used in these tests
    }

    public void AddPathsWatch(IEnumerable<string> paths, Action onUpdate, bool includeSubdirectories = true)
    {
        // Not used in these tests
    }

    public void SubscribeToChanges(Action onUpdate)
    {
        _callbacks.Add(onUpdate);
    }

    public void TriggerChange()
    {
        foreach (var callback in _callbacks)
        {
            callback();
        }
    }

    public void Dispose()
    {
        _callbacks.Clear();
    }
}