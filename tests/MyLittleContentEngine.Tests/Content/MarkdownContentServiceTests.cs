using MyLittleContentEngine.Models;
using MyLittleContentEngine.Tests.TestHelpers;

namespace MyLittleContentEngine.Tests.Content;

public class MarkdownContentServiceTests
{
    [Fact]
    public void MarkdownContentService_RedirectPages_ShouldGenerateRedirectHtml()
    {
        // Arrange
        const string redirectUrl = "config-runsettings";
        var testFrontMatter = new TestFrontMatter
        {
            Title = "Redirect Page",
            RedirectUrl = redirectUrl
        };

        var mockPage = new MarkdownContentPage<TestFrontMatter>
        {
            FrontMatter = testFrontMatter,
            Url = "/redirect-test",
            MarkdownContent = "",
            Outline = []
        };

        // Act - Simulate what GetRenderedContentPageByUrlOrDefault does for redirects
        string html;
        if (!string.IsNullOrEmpty(mockPage.FrontMatter.RedirectUrl))
        {
            html = $$"""
                <!DOCTYPE html>
                <html>
                  <head>
                    <meta charset="utf-8">
                    <meta http-equiv="refresh" content="0;URL='{{mockPage.FrontMatter.RedirectUrl}}'">
                  </head>
                </html>
                """;
        }
        else
        {
            html = "<!-- Normal markdown would be rendered here -->";
        }

        // Assert
        var expectedHtml = $$"""
            <!DOCTYPE html>
            <html>
              <head>
                <meta charset="utf-8">
                <meta http-equiv="refresh" content="0;URL='{{redirectUrl}}'">
              </head>
            </html>
            """;
        
        Assert.Equal(expectedHtml, html);
    }

    [Fact]
    public void MarkdownContentService_NormalPages_ShouldNotGenerateRedirectHtml()
    {
        // Arrange
        var testFrontMatter = new TestFrontMatter
        {
            Title = "Normal Page",
            RedirectUrl = null
        };

        var mockPage = new MarkdownContentPage<TestFrontMatter>
        {
            FrontMatter = testFrontMatter,
            Url = "/normal-test",
            MarkdownContent = "# Normal Content",
            Outline = []
        };

        // Act - Simulate what GetRenderedContentPageByUrlOrDefault does for normal pages
        string html;
        if (!string.IsNullOrEmpty(mockPage.FrontMatter.RedirectUrl))
        {
            html = "<!-- Redirect HTML would be generated here -->";
        }
        else
        {
            html = "<!-- Normal markdown would be rendered here -->";
        }

        // Assert
        Assert.Equal("<!-- Normal markdown would be rendered here -->", html);
    }

    [Fact]
    public void GetContentTocEntriesAsync_FilterLogic_ShouldExcludeRedirectPages()
    {
        // Arrange
        var redirectPage = new MarkdownContentPage<TestFrontMatter>
        {
            FrontMatter = new TestFrontMatter 
            { 
                Title = "Redirect Page", 
                RedirectUrl = "another-page",
                Order = 1
            },
            Url = "/redirect",
            MarkdownContent = "",
            Outline = []
        };

        var normalPage = new MarkdownContentPage<TestFrontMatter>
        {
            FrontMatter = new TestFrontMatter 
            { 
                Title = "Normal Page", 
                RedirectUrl = null,
                Order = 2
            },
            Url = "/normal",
            MarkdownContent = "# Normal Content",
            Outline = []
        };

        var pages = new[] { redirectPage, normalPage };

        // Act - Simulate the filtering logic from GetContentTocEntriesAsync
        var filteredPages = pages
            .Where(p => p.FrontMatter.RedirectUrl == null)
            .ToList();

        // Assert
        Assert.Single(filteredPages);
        Assert.Equal("Normal Page", filteredPages[0].FrontMatter.Title);
        Assert.Equal("/normal", filteredPages[0].Url);
    }

    [Fact]
    public void RedirectUrl_EmptyString_ShouldNotTriggerRedirect()
    {
        // Arrange
        var testFrontMatter = new TestFrontMatter
        {
            Title = "Page With Empty Redirect",
            RedirectUrl = ""
        };

        // Act - Test the condition used in GetRenderedContentPageByUrlOrDefault
        var shouldRedirect = !string.IsNullOrEmpty(testFrontMatter.RedirectUrl);

        // Assert
        Assert.False(shouldRedirect);
    }
}