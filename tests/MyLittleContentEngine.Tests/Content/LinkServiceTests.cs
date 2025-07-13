using MyLittleContentEngine.Services.Content;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class LinkServiceTests
{
    [Theory]
    [InlineData("folder/my-image", "blog/", "http://example.com", "http://example.com/blog/folder/my-image")]
    [InlineData("assets/logo.png", "/MyApp/", "https://mysite.com", "https://mysite.com/MyApp/assets/logo.png")]
    [InlineData("/absolute/path.jpg", "", "http://example.com", "http://example.com/absolute/path.jpg")]
    [InlineData("relative/file.css", "", "https://domain.org", "https://domain.org/relative/file.css")]
    public void GetCanonicalUrl_WithCanonicalBaseUrl_CombinesCorrectly(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("folder/my-image", "blog/", "http://example.com/", "http://example.com/blog/folder/my-image")]
    [InlineData("assets/logo.png", "/MyApp/", "https://mysite.com/", "https://mysite.com/MyApp/assets/logo.png")]
    public void GetCanonicalUrl_WithTrailingSlashInCanonicalBaseUrl_RemovesTrailingSlash(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("folder/my-image", "blog/", "/blog/folder/my-image")]
    [InlineData("assets/logo.png", "/MyApp/", "/MyApp/assets/logo.png")]
    [InlineData("/absolute/path.jpg", "", "/absolute/path.jpg")]
    public void GetCanonicalUrl_WithoutCanonicalBaseUrl_ReturnsRewrittenUrl(string url, string baseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = null
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("folder/my-image", "blog/", "", "/blog/folder/my-image")]
    [InlineData("assets/logo.png", "/MyApp/", "   ", "/MyApp/assets/logo.png")]
    public void GetCanonicalUrl_WithEmptyCanonicalBaseUrl_ReturnsRewrittenUrl(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("http://external.com/image.jpg", "blog/", "http://example.com", "http://external.com/image.jpg")]
    [InlineData("https://cdn.example.com/assets/style.css", "/MyApp/", "https://mysite.com", "https://cdn.example.com/assets/style.css")]
    [InlineData("HTTP://EXTERNAL.COM/FILE.PNG", "", "http://example.com", "HTTP://EXTERNAL.COM/FILE.PNG")]
    [InlineData("HTTPS://CDN.EXAMPLE.COM/STYLE.CSS", "", "https://mysite.com", "HTTPS://CDN.EXAMPLE.COM/STYLE.CSS")]
    public void GetCanonicalUrl_WithExternalUrls_ReturnsUrlUnchanged(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("mailto:test@example.com", "blog/", "http://example.com", "mailto:test@example.com")]
    [InlineData("tel:555-1234", "/MyApp/", "https://mysite.com", "tel:555-1234")]
    [InlineData("ftp://files.example.com", "", "http://example.com", "ftp://files.example.com")]
    public void GetCanonicalUrl_WithSpecialProtocols_ReturnsUrlUnchanged(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("image.jpg?version=2", "blog/", "http://example.com", "http://example.com/blog/image.jpg?version=2")]
    [InlineData("page.html#section", "/app/", "https://mysite.com", "https://mysite.com/app/page.html#section")]
    [InlineData("search?q=test&type=docs#results", "", "http://example.com", "http://example.com/search?q=test&type=docs#results")]
    public void GetCanonicalUrl_WithQueryStringsAndFragments_HandlesCorrectly(string url, string baseUrl, string canonicalBaseUrl, string expected)
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            BaseUrl = baseUrl,
            CanonicalBaseUrl = canonicalBaseUrl
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl(url);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetCanonicalUrl_WithComplexScenario_WorksCorrectly()
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test Blog",
            SiteDescription = "A test blog",
            BaseUrl = "/blog/",
            CanonicalBaseUrl = "https://example.com"
        };
        var linkService = new LinkService(options);

        var result = linkService.GetCanonicalUrl("assets/images/hero.jpg");

        result.ShouldBe("https://example.com/blog/assets/images/hero.jpg");
    }
}