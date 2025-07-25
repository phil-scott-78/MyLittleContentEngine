using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine;
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/myapp"
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
            outputOptions: outputOptions,
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

    [Fact]
    public async Task InvokeAsync_WithSrcsetAttribute_RewritesAllUrls()
    {
        // Arrange
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/MyLittleContentEngine"
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
                var htmlContent = """
                    <img srcset="/images/beer-cheese-xs.webp 480w, 
                     /images/beer-cheese-sm.webp 768w, 
                     /images/beer-cheese-md.webp 1024w, 
                     /images/beer-cheese-lg.webp 1440w, 
                     /images/beer-cheese-xl.webp 1920w" 
                     sizes="100vw" 
                     src="/images/beer-cheese-md.webp" 
                     alt="picture of Beer Cheese">
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            outputOptions: outputOptions,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        
        // All srcset URLs should be rewritten
        Assert.Contains("/MyLittleContentEngine/images/beer-cheese-xs.webp 480w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/beer-cheese-sm.webp 768w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/beer-cheese-md.webp 1024w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/beer-cheese-lg.webp 1440w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/beer-cheese-xl.webp 1920w", responseContent);
        
        // The src attribute should also be rewritten
        Assert.Contains(@"src=""/MyLittleContentEngine/images/beer-cheese-md.webp""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithSrcsetAttribute_SkipsAlreadyRewrittenUrls()
    {
        // Arrange
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/MyLittleContentEngine"
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
                var htmlContent = """
                    <img srcset="/MyLittleContentEngine/images/already-rewritten.webp 480w, 
                     /images/needs-rewriting.webp 768w" 
                     src="/images/needs-rewriting.webp" 
                     alt="test image">
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            outputOptions: outputOptions,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        
        // Already rewritten URL should remain unchanged
        Assert.Contains("/MyLittleContentEngine/images/already-rewritten.webp 480w", responseContent);
        // New URL should be rewritten
        Assert.Contains("/MyLittleContentEngine/images/needs-rewriting.webp 768w", responseContent);
        // The src attribute should also be rewritten
        Assert.Contains(@"src=""/MyLittleContentEngine/images/needs-rewriting.webp""", responseContent);
    }

    [Fact]
    public async Task InvokeAsync_WithSrcsetAttribute_HandlesNewlinesInSrcset()
    {
        // Arrange
        var outputOptions = new OutputOptions 
        { 
            BaseUrl = "/MyLittleContentEngine"
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
                // This mimics the real HTML with newlines in srcset
                var htmlContent = """
                    <img srcset="/images/chicken-piccata-xs.webp 480w, 
                     /images/chicken-piccata-sm.webp 768w, 
                     /images/chicken-piccata-md.webp 1024w, 
                     /images/chicken-piccata-lg.webp 1440w, 
                     /images/chicken-piccata-xl.webp 1920w" 
                     sizes="100vw" 
                     src="/images/chicken-piccata-md.webp" 
                     alt="picture of Chicken Piccata">
                    """;
                await WriteHtmlResponse(context, htmlContent);
            },
            outputOptions: outputOptions,
            xrefResolver: xrefResolver
        );

        var context = CreateHttpContext(serviceProvider);
        
        // Act
        await middleware.InvokeAsync(context);
        
        // Assert
        var responseContent = await ReadResponseContent(context);
        
        // All srcset URLs should be rewritten (including those on new lines)
        Assert.Contains("/MyLittleContentEngine/images/chicken-piccata-xs.webp 480w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/chicken-piccata-sm.webp 768w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/chicken-piccata-md.webp 1024w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/chicken-piccata-lg.webp 1440w", responseContent);
        Assert.Contains("/MyLittleContentEngine/images/chicken-piccata-xl.webp 1920w", responseContent);
        
        // Should not contain HTML entities for newlines
        Assert.DoesNotContain("&#xA;", responseContent);
        
        // The actual srcset attribute value should not contain newlines
        var srcsetValueStart = responseContent.IndexOf("srcset=\"") + 8;
        var srcsetValueEnd = responseContent.IndexOf("\"", srcsetValueStart);
        var srcsetValue = responseContent.Substring(srcsetValueStart, srcsetValueEnd - srcsetValueStart);
        
        // Should not contain literal newlines in the srcset attribute value
        Assert.DoesNotContain("\n", srcsetValue);
        
        // The src attribute should also be rewritten
        Assert.Contains(@"src=""/MyLittleContentEngine/images/chicken-piccata-md.webp""", responseContent);
    }
}