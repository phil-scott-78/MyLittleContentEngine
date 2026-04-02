using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class BaseUrlRewritingProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WithXrefUrl_ResolvesAndRewritesCorrectly()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<a href="xref:System.String">String Documentation</a>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        result.ShouldContain("""<a href="/myapp/api/system/string/">String Documentation</a>""");
    }

    [Fact]
    public async Task ProcessAsync_WithUnresolvedXref_ShowsErrorSpan()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<a href="xref:UnknownType">Unknown Documentation</a>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // Check that the original xref URL is no longer present
        result.ShouldNotContain("""<a href="xref:UnknownType">""");
        // Check that we have an error span with the xref UID
        result.ShouldContain(""""data-xref-uid="UnknownType"""");
        result.ShouldContain(""""data-xref-error="Reference not found"""");
        result.ShouldContain("Unknown Documentation");
    }

    [Fact]
    public async Task ProcessAsync_WithXrefAndBaseUrl_AppliesBothTransformations()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """
            <a href="xref:System.String">String Documentation</a>
            <a href="/docs/guide">Regular Link</a>
            """;
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        result.ShouldContain("""<a href="/myapp/api/system/string/">String Documentation</a>""");
        result.ShouldContain("""<a href="/myapp/docs/guide/">Regular Link</a>""");
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyXrefResolver_ShowsUnresolvedReferences()
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

        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """
            <a href="xref:System.String">String Documentation</a>
            <a href="/docs/guide">Regular Link</a>
            """;
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // Since XrefResolver is present but has no cross-references, unresolved xrefs should show error spans
        result.ShouldNotContain("""<a href="xref:System.String">""");
        result.ShouldContain(""""data-xref-uid="System.String"""");
        result.ShouldContain(""""data-xref-error="Reference not found"""");
        result.ShouldContain("""<a href="/myapp/docs/guide/">Regular Link</a>""");
    }

    [Fact]
    public async Task ProcessAsync_WithXrefTag_ConvertsToLinkWithTitle()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<xref:docs.guides.linking-documents-and-media>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        result.ShouldContain("""<a href="/myapp/docs/guides/linking/">Linking Documents and Media</a>""");
        result.ShouldNotContain("<xref:");
    }

    [Fact]
    public async Task ProcessAsync_WithUnresolvedXrefTag_ShowsErrorSpan()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<xref:unknown.reference>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        result.ShouldContain(""""data-xref-uid="unknown.reference"""");
        result.ShouldContain(""""data-xref-error="Reference not found"""");
        result.ShouldContain("Reference not found: unknown.reference");
        result.ShouldNotContain("<xref:");
    }

    [Fact]
    public async Task ProcessAsync_WithMatchingXrefHrefAndContent_ConvertsToLinkWithTitle()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<a href="xref:docs.guides.linking-documents-and-media">xref:docs.guides.linking-documents-and-media</a>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        result.ShouldContain("""<a href="/myapp/docs/guides/linking/">Linking Documents and Media</a>""");
        result.ShouldNotContain("xref:docs.guides.linking-documents-and-media");
    }

    [Fact]
    public async Task ProcessAsync_WithMismatchedXrefHrefAndContent_DoesNotProcess()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """<a href="xref:docs.guides.linking-documents-and-media">Different Content</a>""";
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // Should not be processed by our new pattern because href and content don't match
        // It should be processed by the existing XrefPattern instead
        result.ShouldContain("""<a href="/myapp/docs/guides/linking/">Different Content</a>""");
        result.ShouldNotContain("xref:docs.guides.linking-documents-and-media");
    }

    [Fact]
    public async Task ProcessAsync_WithSrcsetAttribute_RewritesAllUrls()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
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
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // All srcset URLs should be rewritten
        result.ShouldContain("/MyLittleContentEngine/images/beer-cheese-xs.webp 480w");
        result.ShouldContain("/MyLittleContentEngine/images/beer-cheese-sm.webp 768w");
        result.ShouldContain("/MyLittleContentEngine/images/beer-cheese-md.webp 1024w");
        result.ShouldContain("/MyLittleContentEngine/images/beer-cheese-lg.webp 1440w");
        result.ShouldContain("/MyLittleContentEngine/images/beer-cheese-xl.webp 1920w");

        // The src attribute should also be rewritten
        result.ShouldContain(@"src=""/MyLittleContentEngine/images/beer-cheese-md.webp""");
    }

    [Fact]
    public async Task ProcessAsync_WithSrcsetAttribute_SkipsAlreadyRewrittenUrls()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
        var htmlContent = """
            <img srcset="/MyLittleContentEngine/images/already-rewritten.webp 480w,
             /images/needs-rewriting.webp 768w"
             src="/images/needs-rewriting.webp"
             alt="test image">
            """;
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // Already rewritten URL should remain unchanged
        result.ShouldContain("/MyLittleContentEngine/images/already-rewritten.webp 480w");
        // New URL should be rewritten
        result.ShouldContain("/MyLittleContentEngine/images/needs-rewriting.webp 768w");
        // The src attribute should also be rewritten
        result.ShouldContain(@"src=""/MyLittleContentEngine/images/needs-rewriting.webp""");
    }

    [Fact]
    public async Task ProcessAsync_WithSrcsetAttribute_HandlesNewlinesInSrcset()
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
        var processor = new BaseUrlRewritingProcessor(outputOptions, xrefResolver);
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
        var context = CreateProcessorContext(serviceProvider);

        // Act
        var result = await ((IResponseProcessor)processor).ProcessAsync(htmlContent, context);

        // Assert
        // All srcset URLs should be rewritten (including those on new lines)
        result.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-xs.webp 480w");
        result.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-sm.webp 768w");
        result.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-md.webp 1024w");
        result.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-lg.webp 1440w");
        result.ShouldContain("/MyLittleContentEngine/images/chicken-piccata-xl.webp 1920w");

        // Should not contain HTML entities for newlines
        result.ShouldNotContain("&#xA;");

        // The actual srcset attribute value should not contain newlines
        var srcsetValueStart = result.IndexOf("srcset=\"", StringComparison.Ordinal) + 8;
        var srcsetValueEnd = result.IndexOf("\"", srcsetValueStart, StringComparison.Ordinal);
        var srcsetValue = result.Substring(srcsetValueStart, srcsetValueEnd - srcsetValueStart);

        // Should not contain literal newlines in the srcset attribute value
        srcsetValue.ShouldNotContain("\n");

        // The src attribute should also be rewritten
        result.ShouldContain(@"src=""/MyLittleContentEngine/images/chicken-piccata-md.webp""");
    }

    private static DefaultHttpContext CreateProcessorContext(IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Response.ContentType = "text/html";
        return context;
    }
}
