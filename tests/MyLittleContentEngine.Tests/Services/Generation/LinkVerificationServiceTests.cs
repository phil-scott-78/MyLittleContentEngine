using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Generation;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services.Generation;

public class LinkVerificationServiceTests
{
    private readonly LinkVerificationService _service = new(NullLogger<LinkVerificationService>.Instance);

    [Fact]
    public async Task ValidateLinksAsync_WithNoLinks_ReturnsEmpty()
    {
        // Arrange
        var html = """<html><body><p>No links here</p></body></html>""";
        var validPages = ImmutableHashSet.Create("/page1", "/page2");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateLinksAsync_WithValidLinks_ReturnsEmpty()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <a href="/page1">Link 1</a>
                <a href="/page2">Link 2</a>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create("/page1", "/page2");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateLinksAsync_WithBrokenLinks_ReturnsBrokenLinks()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <a href="/page1">Valid Link</a>
                <a href="/missing">Broken Link</a>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create("/page1", "/page2");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(1);
        result[0].SourcePage.Value.ShouldBe("/test");
        result[0].BrokenUrl.ShouldBe("/missing");
        result[0].LinkType.ShouldBe(LinkType.Href);
        result[0].ElementType.ShouldBe("a");
    }

    [Fact]
    public async Task ValidateLinksAsync_IgnoresExternalLinks()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <a href="https://example.com">External Link</a>
                <a href="http://example.com">External Link</a>
                <a href="mailto:test@example.com">Email Link</a>
                <a href="tel:+1234567890">Phone Link</a>
                <a href="ftp://example.com">FTP Link</a>
                <a href="data:text/html,<p>Hello</p>">Data URL</a>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateLinksAsync_IgnoresAnchorOnlyLinks()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <a href="#">Anchor Link</a>
                <a href="#section">Section Link</a>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateLinksAsync_StripsQueryStrings()
    {
        // Arrange
        var html = """<a href="/page1?query=value">Link</a>""";
        var validPages = ImmutableHashSet.Create("/page1");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty(); // Should find /page1 after stripping query
    }

    [Fact]
    public async Task ValidateLinksAsync_StripsAnchors()
    {
        // Arrange
        var html = """<a href="/page1#section">Link</a>""";
        var validPages = ImmutableHashSet.Create("/page1");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty(); // Should find /page1 after stripping anchor
    }

    [Fact]
    public async Task ValidateLinksAsync_StripsQueryAndAnchor()
    {
        // Arrange
        var html = """<a href="/page1?query=value#section">Link</a>""";
        var validPages = ImmutableHashSet.Create("/page1");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty(); // Should find /page1 after stripping both
    }

    [Fact]
    public async Task ValidateLinksAsync_HandlesBaseUrl()
    {
        // Arrange
        var html = """<a href="/myapp/page1">Link</a>""";
        var validPages = ImmutableHashSet.Create("/page1");
        var baseUrl = new UrlPath("/myapp");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, baseUrl);

        // Assert
        result.ShouldBeEmpty(); // Should find /page1 after removing /myapp prefix
    }

    [Fact]
    public async Task ValidateLinksAsync_HandlesBaseUrlWithTrailingSlash()
    {
        // Arrange
        var html = """<a href="/myapp/page1">Link</a>""";
        var validPages = ImmutableHashSet.Create("/page1");
        var baseUrl = new UrlPath("/myapp/");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, baseUrl);

        // Assert
        result.ShouldBeEmpty(); // Should find /page1 after removing /myapp prefix
    }

    [Fact]
    public async Task ValidateLinksAsync_ValidatesSrcAttributes()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <img src="/missing-image.jpg" />
                <script src="/missing-script.js"></script>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(bl => bl.BrokenUrl == "/missing-image.jpg" && bl.LinkType == LinkType.Src && bl.ElementType == "img");
        result.ShouldContain(bl => bl.BrokenUrl == "/missing-script.js" && bl.LinkType == LinkType.Src && bl.ElementType == "script");
    }

    [Fact]
    public async Task ValidateLinksAsync_ValidatesAllElementTypes()
    {
        // Arrange
        var html = """
            <html>
            <head>
                <link href="/missing.css" rel="stylesheet" />
            </head>
            <body>
                <a href="/missing-page">Link</a>
                <img src="/missing.jpg" />
                <script src="/missing.js"></script>
                <iframe src="/missing.html"></iframe>
                <embed src="/missing.swf" />
                <source src="/missing.mp4" />
                <track src="/missing.vtt" />
                <form action="/missing"></form>
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(9);
        result.ShouldContain(bl => bl.ElementType == "a" && bl.LinkType == LinkType.Href);
        result.ShouldContain(bl => bl.ElementType == "link" && bl.LinkType == LinkType.Href);
        result.ShouldContain(bl => bl.ElementType == "img" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "script" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "iframe" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "embed" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "source" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "track" && bl.LinkType == LinkType.Src);
        result.ShouldContain(bl => bl.ElementType == "form" && bl.LinkType == LinkType.Href);
    }

    [Fact]
    public async Task ValidateLinksAsync_HandlesSrcsetAttribute()
    {
        // Arrange
        var html = """
            <img srcset="/image-480w.jpg 480w, /image-800w.jpg 800w, /missing.jpg 1200w" />
            """;
        var validPages = ImmutableHashSet.Create("/image-480w.jpg", "/image-800w.jpg");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(1);
        result[0].BrokenUrl.ShouldBe("/missing.jpg");
        result[0].ElementType.ShouldBe("img");
    }

    [Fact]
    public async Task ValidateLinksAsync_HandlesSrcsetWithPixelDensity()
    {
        // Arrange
        var html = """
            <img srcset="/image.jpg 1x, /image@2x.jpg 2x, /missing@3x.jpg 3x" />
            """;
        var validPages = ImmutableHashSet.Create("/image.jpg", "/image@2x.jpg");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(1);
        result[0].BrokenUrl.ShouldBe("/missing@3x.jpg");
    }

    [Fact]
    public async Task ValidateLinksAsync_CaseInsensitiveValidation()
    {
        // Arrange
        var html = """<a href="/Page1">Link</a>""";
        var validPages = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "/page1");

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty(); // Should match case-insensitively
    }

    [Fact]
    public async Task ValidateLinksAsync_IgnoresEmptyLinks()
    {
        // Arrange
        var html = """
            <a href="">Empty Link</a>
            <img src="" />
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateLinksAsync_HandlesMultipleBrokenLinksInSamePage()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <a href="/missing1">Link 1</a>
                <a href="/missing2">Link 2</a>
                <img src="/missing3.jpg" />
            </body>
            </html>
            """;
        var validPages = ImmutableHashSet.Create<string>();

        // Act
        var result = await _service.ValidateLinksAsync(html, new UrlPath("/test"), validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(bl => bl.BrokenUrl == "/missing1");
        result.ShouldContain(bl => bl.BrokenUrl == "/missing2");
        result.ShouldContain(bl => bl.BrokenUrl == "/missing3.jpg");
    }

    [Fact]
    public async Task ValidateLinksAsync_PreservesSourcePageInformation()
    {
        // Arrange
        var html = """<a href="/missing">Link</a>""";
        var validPages = ImmutableHashSet.Create<string>();
        var sourcePage = new UrlPath("/blog/my-post");

        // Act
        var result = await _service.ValidateLinksAsync(html, sourcePage, validPages, UrlPath.Empty);

        // Assert
        result.Count.ShouldBe(1);
        result[0].SourcePage.Value.ShouldBe("/blog/my-post");
    }
}
