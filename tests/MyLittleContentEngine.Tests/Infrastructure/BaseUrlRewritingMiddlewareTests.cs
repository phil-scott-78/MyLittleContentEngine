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
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:System.String">String Documentation</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="/myapp/api/system/string">String Documentation</a>""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithUnresolvedXref_ShowsErrorSpan()
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
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:UnknownType">Unknown Documentation</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        // Check that the original xref URL is no longer present
        Assert.DoesNotContain("""<a href="xref:UnknownType">""", responseContent);
        // Check that we have an error span with the xref UID
        Assert.Contains(""""data-xref-uid="UnknownType"""", responseContent);
        Assert.Contains(""""data-xref-error="Reference not found"""", responseContent);
        Assert.Contains("Unknown Documentation", responseContent);
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
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """
                    <a href="xref:System.String">String Documentation</a>
                    <a href="/docs/guide">Regular Link</a>
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
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
    public async Task InvokeAsync_WithEmptyXrefResolver_ShowsUnresolvedReferences()
    {
        // Arrange - XrefResolver with no cross-references
        var options = new ContentEngineOptions 
        { 
            BaseUrl = "/myapp",
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContentEngineFileWatcher, MockContentEngineFileWatcher>();
        
        // Add empty content service but no xref resolver
        var mockContentService = ServiceMockFactory.CreateContentServiceWithCrossReferences();
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """
                    <a href="xref:System.String">String Documentation</a>
                    <a href="/docs/guide">Regular Link</a>
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        // Since XrefResolver is present but has no cross-references, unresolved xrefs should show error spans
        Assert.DoesNotContain("""<a href="xref:System.String">""", responseContent);
        Assert.Contains(""""data-xref-uid="System.String"""", responseContent);
        Assert.Contains(""""data-xref-error="Reference not found"""", responseContent);
        Assert.Contains("""<a href="/myapp/docs/guide">Regular Link</a>""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithXrefTag_ConvertsToLinkWithTitle()
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
            new CrossReference { Uid = "docs.guides.linking-documents-and-media", Title = "Linking Documents and Media", Url = "/docs/guides/linking" }
        );
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<xref:docs.guides.linking-documents-and-media>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="/myapp/docs/guides/linking">Linking Documents and Media</a>""", responseContent);
        Assert.DoesNotContain("<xref:", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithUnresolvedXrefTag_ShowsErrorSpan()
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
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<xref:unknown.reference>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains(""""data-xref-uid="unknown.reference"""", responseContent);
        Assert.Contains(""""data-xref-error="Reference not found"""", responseContent);
        Assert.Contains("Reference not found: unknown.reference", responseContent);
        Assert.DoesNotContain("<xref:", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithMatchingXrefHrefAndContent_ConvertsToLinkWithTitle()
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
            new CrossReference { Uid = "docs.guides.linking-documents-and-media", Title = "Linking Documents and Media", Url = "/docs/guides/linking" }
        );
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:docs.guides.linking-documents-and-media">xref:docs.guides.linking-documents-and-media</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        Assert.Contains("""<a href="/myapp/docs/guides/linking">Linking Documents and Media</a>""", responseContent);
        Assert.DoesNotContain("xref:docs.guides.linking-documents-and-media", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithMismatchedXrefHrefAndContent_DoesNotProcess()
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
            new CrossReference { Uid = "docs.guides.linking-documents-and-media", Title = "Linking Documents and Media", Url = "/docs/guides/linking" }
        );
        services.AddSingleton<IContentService>(mockContentService.Object);
        services.AddSingleton<IXrefResolver, XrefResolver>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var xrefResolver = serviceProvider.GetRequiredService<IXrefResolver>();
        var middleware = new BaseUrlRewritingMiddleware(
            next: async (context) => {
                var htmlContent = """<a href="xref:docs.guides.linking-documents-and-media">Different Content</a>""";
                await WriteHtmlResponse(context, htmlContent);
            },
            options: options,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        // Should not be processed by our new pattern because href and content don't match
        // It should be processed by the existing XrefPattern instead
        Assert.Contains("""<a href="/myapp/docs/guides/linking">Different Content</a>""", responseContent);
        Assert.DoesNotContain("xref:docs.guides.linking-documents-and-media", responseContent);
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