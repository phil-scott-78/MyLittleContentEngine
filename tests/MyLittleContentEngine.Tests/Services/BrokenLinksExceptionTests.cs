using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services;

public class BrokenLinksExceptionTests
{
    [Fact]
    public void BrokenLinksException_WithSingleBrokenLink_FormatsMessageCorrectly()
    {
        // Arrange
        var brokenLinks = ImmutableList.Create(
            new BrokenLink(
                new UrlPath("/blog/post1"),
                "/nonexistent",
                LinkType.Href,
                "a")
        );

        // Act
        var exception = new BrokenLinksException(brokenLinks);

        // Assert
        exception.Message.ShouldContain("Found 1 broken link(s)");
        exception.Message.ShouldContain("In page: /blog/post1");
        exception.Message.ShouldContain("<a href=\"/nonexistent\">");
        exception.BrokenLinks.ShouldBe(brokenLinks);
    }

    [Fact]
    public void BrokenLinksException_WithMultipleBrokenLinks_FormatsMessageCorrectly()
    {
        // Arrange
        var brokenLinks = ImmutableList.Create(
            new BrokenLink(new UrlPath("/blog/post1"), "/missing1", LinkType.Href, "a"),
            new BrokenLink(new UrlPath("/blog/post1"), "/missing2", LinkType.Src, "img"),
            new BrokenLink(new UrlPath("/blog/post2"), "/missing3", LinkType.Href, "a")
        );

        // Act
        var exception = new BrokenLinksException(brokenLinks);

        // Assert
        exception.Message.ShouldContain("Found 3 broken link(s)");
        exception.Message.ShouldContain("In page: /blog/post1");
        exception.Message.ShouldContain("In page: /blog/post2");
        exception.Message.ShouldContain("<a href=\"/missing1\">");
        exception.Message.ShouldContain("<img src=\"/missing2\">");
        exception.Message.ShouldContain("<a href=\"/missing3\">");
    }

    [Fact]
    public void BrokenLinksException_GroupsBySourcePage()
    {
        // Arrange
        var brokenLinks = ImmutableList.Create(
            new BrokenLink(new UrlPath("/page1"), "/broken1", LinkType.Href, "a"),
            new BrokenLink(new UrlPath("/page2"), "/broken2", LinkType.Href, "a"),
            new BrokenLink(new UrlPath("/page1"), "/broken3", LinkType.Src, "img")
        );

        // Act
        var exception = new BrokenLinksException(brokenLinks);

        // Assert
        exception.Message.ShouldContain("In page: /page1");
        exception.Message.ShouldContain("In page: /page2");

        // Verify that broken links from page1 are grouped together
        var page1Index = exception.Message.IndexOf("In page: /page1", StringComparison.Ordinal);
        var broken1Index = exception.Message.IndexOf("/broken1", StringComparison.Ordinal);
        var broken3Index = exception.Message.IndexOf("/broken3", StringComparison.Ordinal);

        // Both /broken1 and /broken3 should appear after page1 but before page2
        (broken1Index > page1Index).ShouldBeTrue();
        (broken3Index > page1Index).ShouldBeTrue();
    }

    [Fact]
    public void BrokenLinksException_PreservesAllBrokenLinksData()
    {
        // Arrange
        var brokenLinks = ImmutableList.Create(
            new BrokenLink(new UrlPath("/test"), "/missing", LinkType.Href, "a"),
            new BrokenLink(new UrlPath("/test2"), "/missing2", LinkType.Src, "script")
        );

        // Act
        var exception = new BrokenLinksException(brokenLinks);

        // Assert
        exception.BrokenLinks.Count.ShouldBe(2);
        exception.BrokenLinks[0].SourcePage.Value.ShouldBe("/test");
        exception.BrokenLinks[0].BrokenUrl.ShouldBe("/missing");
        exception.BrokenLinks[0].LinkType.ShouldBe(LinkType.Href);
        exception.BrokenLinks[0].ElementType.ShouldBe("a");

        exception.BrokenLinks[1].SourcePage.Value.ShouldBe("/test2");
        exception.BrokenLinks[1].BrokenUrl.ShouldBe("/missing2");
        exception.BrokenLinks[1].LinkType.ShouldBe(LinkType.Src);
        exception.BrokenLinks[1].ElementType.ShouldBe("script");
    }

    [Fact]
    public void BrokenLinksException_WithVariousLinkTypes_FormatsCorrectly()
    {
        // Arrange
        var brokenLinks = ImmutableList.Create(
            new BrokenLink(new UrlPath("/page"), "/broken", LinkType.Href, "a"),
            new BrokenLink(new UrlPath("/page"), "/broken.jpg", LinkType.Src, "img"),
            new BrokenLink(new UrlPath("/page"), "/broken.js", LinkType.Src, "script"),
            new BrokenLink(new UrlPath("/page"), "/broken.css", LinkType.Href, "link")
        );

        // Act
        var exception = new BrokenLinksException(brokenLinks);

        // Assert
        exception.Message.ShouldContain("<a href=\"/broken\">");
        exception.Message.ShouldContain("<img src=\"/broken.jpg\">");
        exception.Message.ShouldContain("<script src=\"/broken.js\">");
        exception.Message.ShouldContain("<link href=\"/broken.css\">");
    }
}
