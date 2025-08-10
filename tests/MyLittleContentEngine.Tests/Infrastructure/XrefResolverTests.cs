using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Tests.TestHelpers;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class XrefResolverTests
{
    [Fact]
    public async Task ResolveAsync_WithValidUid_ReturnsCorrectUrl()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        // Mock content service with cross-references
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences(
            new CrossReference { Uid = "System.String", Title = "String Class", Url = "/api/system/string" },
            new CrossReference { Uid = "System.Int32", Title = "Int32 Struct", Url = "/api/system/int32" }
        );
        services.AddSingleton(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var provider = services.BuildServiceProvider();
        var xrefResolver = provider.GetRequiredService<IXrefResolver>();

        // Act
        var result = await xrefResolver.ResolveAsync("System.String");

        // Assert
        Assert.Equal("/api/system/string", result);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidUid_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences();
        services.AddSingleton(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var provider = services.BuildServiceProvider();
        var xrefResolver = provider.GetRequiredService<IXrefResolver>();

        // Act
        var result = await xrefResolver.ResolveAsync("NonExistent.Type");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyUid_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences();
        services.AddSingleton(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var provider = services.BuildServiceProvider();
        var xrefResolver = provider.GetRequiredService<IXrefResolver>();

        // Act & Assert
        Assert.Null(await xrefResolver.ResolveAsync(""));
        Assert.Null(await xrefResolver.ResolveAsync(null!));
        Assert.Null(await xrefResolver.ResolveAsync("   "));
    }

    [Fact]
    public async Task ResolveAsync_CaseInsensitive_ReturnsCorrectUrl()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences(
            new CrossReference { Uid = "System.String", Title = "String Class", Url = "/api/system/string" }
        );
        services.AddSingleton(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var provider = services.BuildServiceProvider();
        var xrefResolver = provider.GetRequiredService<IXrefResolver>();

        // Act & Assert
        Assert.Equal("/api/system/string", await xrefResolver.ResolveAsync("system.string"));
        Assert.Equal("/api/system/string", await xrefResolver.ResolveAsync("SYSTEM.STRING"));
        Assert.Equal("/api/system/string", await xrefResolver.ResolveAsync("System.String"));
    }
}

// Mock implementation for testing
public class MockContentEngineFileWatcher : IContentEngineFileWatcher
{
    public void AddPathWatch(string path, string filePattern, Action<string> onFileChanged, bool includeSubdirectories = true) { }
    public void AddPathsWatch(IEnumerable<string> paths, Action onUpdate, bool includeSubdirectories = true) { }
    public void SubscribeToMetadataUpdate(Action onUpdate) { }
    public void Dispose() { }
}