using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Tests.TestHelpers;
using Xunit;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class BaseUrlRewritingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithXrefUrl_ResolvesAndRewritesCorrectly()
    {
        // Arrange
        var options = new ContentEngineOptions 
        { 
            BaseUrl = "/myapp",
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences(
            new CrossReference { Uid = "System.String", Title = "String Class", Url = "/api/system/string" }
        );
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:System.String">String Documentation</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="/myapp/api/system/string">String Documentation</a>""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithUnresolvedXref_LeavesXrefIntact()
    {
        // Arrange
        var options = new ContentEngineOptions 
        { 
            BaseUrl = "/myapp",
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences();
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:UnknownType">Unknown Documentation</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="xref:UnknownType">Unknown Documentation</a>""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithXrefAndBaseUrl_AppliesBothTransformations()
    {
        // Arrange
        var options = new ContentEngineOptions 
        { 
            BaseUrl = "/myapp",
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences(
            new CrossReference { Uid = "System.String", Title = "String Class", Url = "/api/system/string" }
        );
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """
                    <a href="xref:System.String">String Documentation</a>
                    <a href="/docs/guide">Regular Link</a>
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="/myapp/api/system/string">String Documentation</a>""", responseContent);
        Assert.Contains("""<a href="/myapp/docs/guide">Regular Link</a>""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithoutXrefResolver_SkipsXrefProcessing()
    {
        // Arrange - Don't register XrefResolver
        var options = new ContentEngineOptions 
        { 
            BaseUrl = "/myapp",
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """
                    <a href="xref:System.String">String Documentation</a>
                    <a href="/docs/guide">Regular Link</a>
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="xref:System.String">String Documentation</a>""", responseContent);
        Assert.Contains("""<a href="/myapp/docs/guide">Regular Link</a>""", responseContent);
    }

    private static HttpContext CreateHttpContext(IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = serviceProvider;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task WriteHtmlResponse(HttpContext context, string htmlContent)
    {
        context.Response.ContentType = "text/html";
        context.Response.StatusCode = 200;
        
        var bytes = Encoding.UTF8.GetBytes(htmlContent);
        await context.Response.Body.WriteAsync(bytes);
        context.Response.Body.Position = 0; // Reset position for reading
    }

    private static async Task<string> ReadResponseContent(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}