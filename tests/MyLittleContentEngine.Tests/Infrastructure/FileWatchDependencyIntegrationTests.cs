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

        // Act - Register service with file-watched lifetime (direct registration)
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify registrations
        var factory = serviceProvider.GetService<FileWatchDependencyFactory<CacheableExpensiveService>>();
        factory.ShouldNotBeNull();

        var service = serviceProvider.GetService<CacheableExpensiveService>();
        service.ShouldNotBeNull();
    }

    [Fact]
    public void AddFileWatched_ShouldProvideConsistentInstancesWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, TestContentEngineFileWatcher>();
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        using var serviceProvider = services.BuildServiceProvider();

        // Act - Create a scope and get service instances
        using var scope = serviceProvider.CreateScope();
        var service1 = scope.ServiceProvider.GetRequiredService<CacheableExpensiveService>();
        var service2 = scope.ServiceProvider.GetRequiredService<CacheableExpensiveService>();

        // Assert - Should be the same instance within scope
        service1.ShouldBeSameAs(service2);
    }

    [Fact]
    public void AddFileWatched_ShouldInvalidateAcrossScopes_WhenFileChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var fileWatcher = new TestContentEngineFileWatcher();
        services.AddSingleton<IContentEngineFileWatcher>(fileWatcher);
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<CacheableExpensiveService>();

        using var serviceProvider = services.BuildServiceProvider();

        // Act - Get instance in first scope
        CacheableExpensiveService service1;
        using (var scope1 = serviceProvider.CreateScope())
        {
            service1 = scope1.ServiceProvider.GetRequiredService<CacheableExpensiveService>();
        }

        // Trigger file change
        fileWatcher.TriggerChange();

        // Get instance in second scope
        CacheableExpensiveService service2;
        using (var scope2 = serviceProvider.CreateScope())
        {
            service2 = scope2.ServiceProvider.GetRequiredService<CacheableExpensiveService>();
        }

        // Assert - Should be different instances due to file change invalidation
        service1.ShouldNotBeSameAs(service2);
    }

    [Fact]
    public void AddFileWatched_ShouldWorkWithContentEngineServices()
    {
        // Arrange - Simulate registering a content service with file-watched lifetime
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, TestContentEngineFileWatcher>();
        
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        
        // Act - Register content service with file-watched lifetime for cache invalidation
        configuredServices.AddFileWatched<SimulatedContentService>();

        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify the service can be resolved and works as expected
        var contentService = serviceProvider.GetRequiredService<SimulatedContentService>();
        contentService.ShouldNotBeNull();
        
        var content = contentService.GetCachedContent();
        content.ShouldNotBeEmpty();
        
        // Should get same content on subsequent calls (cached)
        var content2 = contentService.GetCachedContent();
        content.ShouldBe(content2);
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

// Example content service that could benefit from file-watched caching
public class SimulatedContentService
{
    private readonly Dictionary<string, string> _contentCache = new();

    public string GetCachedContent(string key = "default")
    {
        if (_contentCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Simulate loading content from files
        var content = $"Loaded content for {key} at {DateTime.UtcNow}";
        _contentCache[key] = content;
        return content;
    }
}

// TestContentEngineFileWatcher is defined in FileWatchDependencyFactoryTests.cs