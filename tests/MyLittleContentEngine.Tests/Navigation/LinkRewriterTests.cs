using MyLittleContentEngine.Services.Content;
using Shouldly;

namespace MyLittleContentEngine.Tests.Navigation;

public class LinkRewriterTests
{
    [Theory]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("mailto:test@example.com", "mailto:test@example.com")]
    [InlineData("tel:555-1234", "tel:555-1234")]
    [InlineData("ftp://files.example.com", "ftp://files.example.com")]
    public void RewriteUrl_WithExternalUrls_ReturnsUnchanged(string url, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, "/docs");

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("#section", "#section")]
    [InlineData("#introduction", "#introduction")]
    [InlineData("#", "#")]
    public void RewriteUrl_WithAnchorLinks_ReturnsUnchanged(string url, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, "/docs");

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/absolute/path", "/absolute/path")]
    [InlineData("/", "/")]
    [InlineData("/docs/guide", "/docs/guide")]
    public void RewriteUrl_WithAbsolutePaths_ReturnsUnchanged(string url, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, "/docs");

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("relative-page", "/docs", "/docs/relative-page")]
    [InlineData("relative-page", "docs", "docs/relative-page")]
    [InlineData("guide.md", "/docs", "/docs/guide.md")]
    [InlineData("guide.md", "docs", "docs/guide.md")]
    [InlineData("sub/page", "/docs", "/docs/sub/page")]
    [InlineData("sub/page", "docs", "docs/sub/page")]
    [InlineData("index", "/", "/index")]
    [InlineData("index", "", "index")]
    public void RewriteUrl_WithRelativePaths_CombinesWithBaseUrl(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("../parent-page", "/docs/sub", "/docs/parent-page")]
    [InlineData("../../root-page", "/docs/sub/subsub", "/docs/root-page")]
    [InlineData("../sibling/page", "/docs/current", "/docs/sibling/page")]
    [InlineData("../", "/docs/current", "/docs/")]
    public void RewriteUrl_WithParentRelativePaths_NavigatesUpCorrectly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("../../../too-many-levels", "/docs", "/too-many-levels")]
    [InlineData("../../../../way-too-many", "/single", "/way-too-many")]
    public void RewriteUrl_WithTooManyParentLevels_HandlesGracefully(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("page?param=value", "/docs", "/docs/page?param=value")]
    [InlineData("guide#section", "/docs", "/docs/guide#section")]
    [InlineData("doc?a=1&b=2#top", "/docs", "/docs/doc?a=1&b=2#top")]
    [InlineData("../parent?query=test", "/docs/sub", "/docs/parent?query=test")]
    [InlineData("../sibling#anchor", "/docs/current", "/docs/sibling#anchor")]
    public void RewriteUrl_WithQueryStringsAndFragments_RewritesPathOnly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("?query=only", "/docs", "/docs?query=only")]
    [InlineData("#fragment-only", "/docs", "#fragment-only")]
    public void RewriteUrl_WithQueryOrFragmentOnly_HandlesCorrectly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("page with spaces", "/docs", "/docs/page with spaces")]
    [InlineData("file%20encoded", "/docs", "/docs/file%20encoded")]
    [InlineData("special-chars!@#$", "/docs", "/docs/special-chars!@#$")]
    public void RewriteUrl_WithSpecialCharacters_PreservesCharacters(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("HTTP://EXAMPLE.COM", "HTTP://EXAMPLE.COM")]
    [InlineData("HTTPS://EXAMPLE.COM", "HTTPS://EXAMPLE.COM")]
    [InlineData("MAILTO:TEST@EXAMPLE.COM", "MAILTO:TEST@EXAMPLE.COM")]
    public void RewriteUrl_WithUppercaseProtocols_RecognizesAsExternal(string url, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, "/docs");

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("relative", "", "relative")]
    [InlineData("../parent", "child", "parent")]
    [InlineData("sibling", "current", "current/sibling")]
    public void RewriteUrl_WithEmptyOrSimpleBaseUrl_HandlesCorrectly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Fact]
    public void RewriteUrl_WithComplexNestedPath_RewritesCorrectly()
    {
        var url = "../../api/reference/methods#get-user";
        var baseUrl = "/docs/guides/authentication";
        var expected = "/docs/api/reference/methods#get-user";

        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    [Fact]
    public void RewriteUrl_WithMultipleQueryParameters_PreservesAll()
    {
        var url = "../search?q=test&type=docs&sort=date#results";
        var baseUrl = "/docs/current";
        var expected = "/docs/search?q=test&type=docs&sort=date#results";

        var result = LinkRewriter.RewriteUrl(url, baseUrl);

        result.ShouldBe(expected);
    }

    // Tests for BaseUrl functionality
    [Theory]
    [InlineData("/docs/guide", "/MyLittleContentEngine", "/MyLittleContentEngine/docs/guide")]
    [InlineData("/", "/MyLittleContentEngine", "/MyLittleContentEngine/")]
    [InlineData("/api/reference", "/app", "/app/api/reference")]
    [InlineData("/index.html", "/subdir", "/subdir/index.html")]
    [InlineData("/styles.css", "/", "/styles.css")]
    [InlineData("/api", "/", "/api")]
    [InlineData("/", "/", "/")]
    public void RewriteUrl_WithBaseUrl_PrependsBaseUrlToAbsolutePaths(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/docs/guide", "/MyLittleContentEngine/", "/MyLittleContentEngine/docs/guide")]
    [InlineData("/api/reference", "/app/", "/app/api/reference")]
    [InlineData("/index.html", "/subdir/", "/subdir/index.html")]
    [InlineData("/styles.css", "/", "/styles.css")]
    [InlineData("/", "/MyLittleContentEngine/", "/MyLittleContentEngine/")]
    public void RewriteUrl_WithTrailingSlashBaseUrl_HandlesCorrectly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("relative-page", "/docs", "/MyLittleContentEngine", "/MyLittleContentEngine/docs/relative-page")]
    [InlineData("../parent", "/docs/sub", "/app", "/app/docs/parent")]
    [InlineData("guide.md", "/content", "/subdir", "/subdir/content/guide.md")]
    public void RewriteUrl_WithBaseUrlAndRelativePaths_CombinesAndPrependsBaseUrl(string url, string relativeToUrl, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, relativeToUrl, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("http://example.com", "/MyLittleContentEngine", "http://example.com")]
    [InlineData("https://example.com", "/app", "https://example.com")]
    [InlineData("mailto:test@example.com", "/subdir", "mailto:test@example.com")]
    [InlineData("#section", "/MyLittleContentEngine", "#section")]
    public void RewriteUrl_WithBaseUrlAndExternalOrAnchorLinks_DoesNotApplyBaseUrl(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/docs/guide?param=value", "/MyLittleContentEngine", "/MyLittleContentEngine/docs/guide?param=value")]
    [InlineData("/api#section", "/app", "/app/api#section")]
    [InlineData("/search?q=test&type=docs#results", "/subdir", "/subdir/search?q=test&type=docs#results")]
    public void RewriteUrl_WithBaseUrlAndQueryStringsFragments_PrependsBaseUrlToPathOnly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/docs/guide", "", "/docs/guide")]
    [InlineData("/docs/guide", "   ", "/docs/guide")]
    [InlineData("relative", "", "relative")]
    public void RewriteUrl_WithEmptyBaseUrl_DoesNotModifyUrl(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/docs/guide", "MyLittleContentEngine", "/MyLittleContentEngine/docs/guide")]
    [InlineData("/docs/guide", "MyLittleContentEngine/", "/MyLittleContentEngine/docs/guide")]
    [InlineData("/docs/guide", "/MyLittleContentEngine/", "/MyLittleContentEngine/docs/guide")]
    public void RewriteUrl_WithBaseUrlNormalization_HandlesSlashesCorrectly(string url, string baseUrl, string expected)
    {
        var result = LinkRewriter.RewriteUrl(url, string.Empty, baseUrl);

        result.ShouldBe(expected);
    }

    [Fact]
    public void RewriteUrl_WithComplexBaseUrlScenario_WorksCorrectly()
    {
        var url = "../../api/reference/methods#get-user";
        var relativeToUrl = "/docs/guides/authentication";
        var baseUrl = "/MyLittleContentEngine";
        var expected = "/MyLittleContentEngine/docs/api/reference/methods#get-user";

        var result = LinkRewriter.RewriteUrl(url, relativeToUrl, baseUrl);

        result.ShouldBe(expected);
    }
}