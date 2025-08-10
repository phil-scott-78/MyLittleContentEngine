using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;

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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("""<a href="/myapp/api/system/string">String Documentation</a>""");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldNotContain("""<a href="xref:UnknownType">""");
        // Check that we have an error span with the xref UID
        responseContent.ShouldContain(""""data-xref-uid="UnknownType"""");
        responseContent.ShouldContain(""""data-xref-error="Reference not found"""");
        responseContent.ShouldContain("Unknown Documentation");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("""<a href="/myapp/api/system/string">String Documentation</a>""");
        responseContent.ShouldContain("""<a href="/myapp/docs/guide">Regular Link</a>""");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldNotContain("""<a href="xref:System.String">""");
        responseContent.ShouldContain(""""data-xref-uid="System.String"""");
        responseContent.ShouldContain(""""data-xref-error="Reference not found"""");
        responseContent.ShouldContain("""<a href="/myapp/docs/guide">Regular Link</a>""");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("""<a href="/myapp/docs/guides/linking">Linking Documents and Media</a>""");
        responseContent.ShouldNotContain("<xref:");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain(""""data-xref-uid="unknown.reference"""");
        responseContent.ShouldContain(""""data-xref-error="Reference not found"""");
        responseContent.ShouldContain("Reference not found: unknown.reference");
        responseContent.ShouldNotContain("<xref:");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("""<a href="/myapp/docs/guides/linking">Linking Documents and Media</a>""");
        responseContent.ShouldNotContain("xref:docs.guides.linking-documents-and-media");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("""<a href="/myapp/docs/guides/linking">Different Content</a>""");
        responseContent.ShouldNotContain("xref:docs.guides.linking-documents-and-media");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("/MyLittleContentEngine/images/beer-cheese-xs.webp 480w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/beer-cheese-sm.webp 768w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/beer-cheese-md.webp 1024w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/beer-cheese-lg.webp 1440w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/beer-cheese-xl.webp 1920w");
        
        // The src attribute should also be rewritten
        responseContent.ShouldContain(@"src=""/MyLittleContentEngine/images/beer-cheese-md.webp""");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("/MyLittleContentEngine/images/already-rewritten.webp 480w");
        // New URL should be rewritten
        responseContent.ShouldContain("/MyLittleContentEngine/images/needs-rewriting.webp 768w");
        // The src attribute should also be rewritten
        responseContent.ShouldContain(@"src=""/MyLittleContentEngine/images/needs-rewriting.webp""");
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
        services.AddSingleton(mockContentService.Object);
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
        responseContent.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-xs.webp 480w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-sm.webp 768w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-md.webp 1024w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-lg.webp 1440w");
        responseContent.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-xl.webp 1920w");
        
        // Should not contain HTML entities for newlines
        responseContent.ShouldNotContain("&#xA;");
        
        // The actual srcset attribute value should not contain newlines
        var srcsetValueStart = responseContent.IndexOf("srcset=\"", StringComparison.Ordinal) + 8;
        var srcsetValueEnd = responseContent.IndexOf("\"", srcsetValueStart, StringComparison.Ordinal);
        var srcsetValue = responseContent.Substring(srcsetValueStart, srcsetValueEnd - srcsetValueStart);
        
        // Should not contain literal newlines in the srcset attribute value
        srcsetValue.ShouldNotContain("\n");
        
        // The src attribute should also be rewritten
        responseContent.ShouldContain(@"src=""/MyLittleContentEngine/images/chicken-piccata-md.webp""");
    }
}