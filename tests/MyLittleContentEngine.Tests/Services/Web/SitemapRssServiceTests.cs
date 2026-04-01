using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Web;
using Shouldly;

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
        sitemap.ShouldContain("https://example.com/test-page");
        sitemap.ShouldContain("https://example.com/blog/post");
        sitemap.ShouldNotContain("https://example.com//"); // No double slashes
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
        rssFeed.ShouldContain("https://example.com/test-page");
        rssFeed.ShouldContain("https://example.com/blog/post");
        rssFeed.ShouldNotContain("https://example.com//"); // No double slashes
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

    [Fact]
    public async Task GenerateSitemap_WithLocalization_IncludesHreflangLinks()
    {
        var localization = new LocalizationOptions
        {
            DefaultLocale = "en",
            Locales = ImmutableDictionary<string, LocaleInfo>.Empty
                .Add("en", new LocaleInfo("English"))
                .Add("fr", new LocaleInfo("Français", HtmlLang: "fr-FR"))
        };

        var options = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            CanonicalBaseUrl = "https://example.com",
            Localization = localization
        };

        var enService = new MockLocalizedContentService(
            ("/getting-started", "Getting Started"));
        var frService = new MockLocalizedContentService(
            ("fr/getting-started", "Premiers pas"));

        var service = new SitemapRssService(options, [enService, frService]);
        var sitemap = await service.GenerateSitemap();

        // Should contain hreflang alternate links
        sitemap.ShouldContain("hreflang");
        sitemap.ShouldContain("fr-FR"); // Uses HtmlLang from LocaleInfo
        sitemap.ShouldContain("https://example.com/getting-started");
        sitemap.ShouldContain("https://example.com/fr/getting-started");
    }

    [Fact]
    public async Task GenerateSitemap_WithoutLocalization_NoHreflangLinks()
    {
        var options = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            CanonicalBaseUrl = "https://example.com"
        };

        var mockContentService = new MockContentService();
        var service = new SitemapRssService(options, [mockContentService]);
        var sitemap = await service.GenerateSitemap();

        sitemap.ShouldNotContain("hreflang");
    }

    private class MockLocalizedContentService(params (string url, string title)[] pages) : IContentService
    {
        public int SearchPriority => 1;

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
        {
            return Task.FromResult(pages.Select(p => new PageToGenerate(
                p.url, p.url, new Metadata { Title = p.title })).ToImmutableList());
        }

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
            Task.FromResult(ImmutableList<ContentTocItem>.Empty);
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
            Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
            Task.FromResult(ImmutableList<CrossReference>.Empty);
    }
}