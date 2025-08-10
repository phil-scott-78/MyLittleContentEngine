using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Web;

namespace MyLittleContentEngine.Tests.Services.Web;

public class SitemapRssServiceTests
{
    [Fact]
    public async Task GenerateSitemap_UrlsAreProperlyConstructed()
    {
        // Arrange
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            CanonicalBaseUrl = "https://example.com"
        };

        var mockContentService = new MockContentService();
        var service = new SitemapRssService(options, new[] { mockContentService });

        // Act
        var sitemap = await service.GenerateSitemap();

        // Assert
        Assert.Contains("https://example.com/test-page", sitemap);
        Assert.Contains("https://example.com/blog/post", sitemap);
        Assert.DoesNotContain("https://example.com//", sitemap); // No double slashes
    }

    [Fact]
    public async Task GenerateRssFeed_UrlsAreProperlyConstructed()
    {
        // Arrange
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            CanonicalBaseUrl = "https://example.com/"
        };

        var mockContentService = new MockContentService();
        var service = new SitemapRssService(options, new[] { mockContentService });

        // Act
        var rssFeed = await service.GenerateRssFeed();

        // Assert
        Assert.Contains("https://example.com/test-page", rssFeed);
        Assert.Contains("https://example.com/blog/post", rssFeed);
        Assert.DoesNotContain("https://example.com//", rssFeed); // No double slashes
    }

    private class MockContentService : IContentService
    {
        public int SearchPriority => 1;

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
        {
            var pages = ImmutableList<PageToGenerate>.Empty
                .Add(new PageToGenerate(
                    "/test-page",
                    "TestPage",
                    new Metadata { Title = "Test Page", RssItem = true, LastMod = DateTime.Now }))
                .Add(new PageToGenerate(
                    "blog/post",
                    "BlogPost",
                    new Metadata { Title = "Blog Post", RssItem = true, LastMod = DateTime.Now }));

            return Task.FromResult(pages);
        }

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
        {
            return Task.FromResult(ImmutableList<ContentTocItem>.Empty);
        }

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
        {
            return Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        }

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
        {
            return Task.FromResult(ImmutableList<CrossReference>.Empty);
        }
    }
}