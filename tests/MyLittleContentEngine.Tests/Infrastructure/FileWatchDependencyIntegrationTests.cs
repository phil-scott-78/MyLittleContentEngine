using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

/// <summary>
/// Integration tests demonstrating usage patterns for FileWatchDependencyFactory
/// through the AddFileWatched extension method.
/// </summary>
public class FileWatchDependencyIntegrationTests
{
    [Fact]
    public void AddFileWatched_ShouldRegisterServiceCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register core dependencies first
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, TestContentEngineFileWatcher>();
        
        // Create configured content engine collection
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);

        // Act - Register service with file-watched lifetime
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify factory registration
        var factory = serviceProvider.GetRequiredService<FileWatchDependencyFactory<CacheableExpensiveService>>();
        factory.ShouldNotBeNull();

        // Verify service can be resolved through the factory
        var service1 = serviceProvider.GetRequiredService<CacheableExpensiveService>();
        var service2 = serviceProvider.GetRequiredService<CacheableExpensiveService>();
        
        // Should be same instance (singleton behavior through factory)
        service1.ShouldBeSameAs(service2);
        service1.ShouldNotBeNull();
    }

    [Fact]
    public void AddFileWatched_WithInterface_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, TestContentEngineFileWatcher>();
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);

        // Act - Register service with interface/implementation types
        configuredServices.AddFileWatched<ITestService, TestServiceImplementation>();

        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify factory registration for implementation type
        var factory = serviceProvider.GetRequiredService<FileWatchDependencyFactory<TestServiceImplementation>>();
        factory.ShouldNotBeNull();

        // Verify interface can be resolved through implementation factory
        var service1 = serviceProvider.GetRequiredService<ITestService>();
        var service2 = serviceProvider.GetRequiredService<ITestService>();
        
        // Should be same instance (singleton behavior through factory)
        service1.ShouldBeSameAs(service2);
        service1.ShouldNotBeNull();
        service1.ShouldBeOfType<TestServiceImplementation>();
    }

    [Fact]
    public void GetInstance_ShouldReturnSameInstance_UntilInvalidated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var fileWatcher = new TestContentEngineFileWatcher();
        services.AddSingleton<IContentEngineFileWatcher>(fileWatcher);
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<FileWatchDependencyFactory<CacheableExpensiveService>>();

        // Act - Get multiple instances before invalidation
        var instance1 = factory.GetInstance();
        var instance2 = factory.GetInstance();

        // Assert - Should be same instance
        instance1.ShouldBeSameAs(instance2);

        // Act - Invalidate and get new instance
        factory.InvalidateInstance();
        var instance3 = factory.GetInstance();

        // Assert - Should be different instance after invalidation
        instance3.ShouldNotBeSameAs(instance1);
    }

    [Fact]
    public void FileWatcher_ShouldInvalidateInstance_OnFileChange()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var fileWatcher = new TestContentEngineFileWatcher();
        services.AddSingleton<IContentEngineFileWatcher>(fileWatcher);
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<FileWatchDependencyFactory<CacheableExpensiveService>>();

        // Act - Get initial instance through factory
        var service1 = factory.GetInstance();
        
        // Trigger file change (should invalidate the cached instance in the factory)
        fileWatcher.TriggerChange();
        
        // Get instance after file change through factory
        var service2 = factory.GetInstance();

        // Assert - Should be different instances due to file change invalidation
        service1.ShouldNotBeSameAs(service2);
    }

    [Fact]
    public void Factory_ShouldCreateNewInstance_AfterFileChange()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var fileWatcher = new TestContentEngineFileWatcher();
        services.AddSingleton<IContentEngineFileWatcher>(fileWatcher);
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<FileWatchDependencyFactory<CacheableExpensiveService>>();

        // Act
        var instance1 = factory.GetInstance();
        var instanceId1 = instance1.InstanceId;
        
        // Trigger file watcher change
        fileWatcher.TriggerChange();
        
        var instance2 = factory.GetInstance();
        var instanceId2 = instance2.InstanceId;

        // Assert
        instanceId1.ShouldNotBe(instanceId2);
        instance1.ShouldNotBeSameAs(instance2);
    }

    [Fact]
    public void MultipleServices_ShouldHaveIndependentFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var fileWatcher = new TestContentEngineFileWatcher();
        services.AddSingleton<IContentEngineFileWatcher>(fileWatcher);
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();
        configuredServices.AddFileWatched<ITestService, TestServiceImplementation>();

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory1 = serviceProvider.GetRequiredService<FileWatchDependencyFactory<CacheableExpensiveService>>();
        var factory2 = serviceProvider.GetRequiredService<FileWatchDependencyFactory<TestServiceImplementation>>();
        
        var service1a = serviceProvider.GetRequiredService<CacheableExpensiveService>();
        var service1b = serviceProvider.GetRequiredService<CacheableExpensiveService>();
        
        var service2a = serviceProvider.GetRequiredService<ITestService>();
        var service2b = serviceProvider.GetRequiredService<ITestService>();

        // Assert - Each service type should have independent factories
        factory1.ShouldNotBeSameAs(factory2);
        
        // Same type should return same instance
        service1a.ShouldBeSameAs(service1b);
        service2a.ShouldBeSameAs(service2b);
        
        // Different types should be different instances
        service1a.ShouldNotBeSameAs(service2a);
    }

}

// Example service that benefits from file-watched caching
public class CacheableExpensiveService
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public string PerformExpensiveOperation()
    {
        // Simulate expensive operation
        Thread.Sleep(100);
        return $"Result from instance {InstanceId} created at {CreatedAt}";
    }
}

// Test interfaces and implementations for interface/implementation registration testing
public interface ITestService
{
    string GetValue();
}

public class TestServiceImplementation : ITestService
{
    public string GetValue() => "Test Implementation";
}

// TestContentEngineFileWatcher is defined in FileWatchDependencyFactoryTests.cs