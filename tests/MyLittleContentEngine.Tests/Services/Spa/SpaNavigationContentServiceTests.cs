using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Spa;

namespace MyLittleContentEngine.Tests.Services.Spa;

public class SpaNavigationContentServiceTests
{
    private static IServiceProvider BuildServiceProvider(params IContentService[] contentServices)
    {
        var services = new ServiceCollection();
        foreach (var svc in contentServices)
            services.AddSingleton(svc);

        // Register the SpaNavigationContentService itself so it appears in IEnumerable<IContentService>
        services.AddSingleton(new SpaNavigationOptions());
        services.AddTransient<IContentService, SpaNavigationContentService>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Generates_json_routes_for_html_pages()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
            new PageToGenerate("/docs/intro", "docs/intro.html"),
        ]);

        var sp = BuildServiceProvider(contentService);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Equal(2, pages.Count);
        Assert.Contains(pages, p => p.Url.Value == "/_spa-data/about.json");
        Assert.Contains(pages, p => p.Url.Value == "/_spa-data/docs/intro.json");
    }

    [Fact]
    public async Task Root_page_uses_index_slug()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/", "index.html"),
        ]);

        var sp = BuildServiceProvider(contentService);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Single(pages);
        Assert.Equal("/_spa-data/index.json", pages[0].Url.Value);
    }

    [Fact]
    public async Task Skips_binary_pages()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
            new PageToGenerate(new UrlPath("/images/logo.png"), new FilePath("images/logo.png"), null, IsBinary: true),
        ]);

        var sp = BuildServiceProvider(contentService);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Single(pages);
        Assert.Equal("/_spa-data/about.json", pages[0].Url.Value);
    }

    [Fact]
    public async Task Skips_non_html_files()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
            new PageToGenerate("/styles.css", "styles.css"),
            new PageToGenerate("/app.js", "app.js"),
            new PageToGenerate("/search-index.json", "search-index.json"),
        ]);

        var sp = BuildServiceProvider(contentService);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Single(pages);
        Assert.Equal("/_spa-data/about.json", pages[0].Url.Value);
    }

    [Fact]
    public async Task Collects_pages_from_multiple_content_services()
    {
        var service1 = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);
        var service2 = new StubContentService(
        [
            new PageToGenerate("/recipes/soup", "recipes/soup.html"),
        ]);

        var sp = BuildServiceProvider(service1, service2);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Equal(2, pages.Count);
        Assert.Contains(pages, p => p.Url.Value == "/_spa-data/about.json");
        Assert.Contains(pages, p => p.Url.Value == "/_spa-data/recipes/soup.json");
    }

    [Fact]
    public async Task Respects_custom_data_path()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);

        var sp = BuildServiceProvider(contentService);
        var options = new SpaNavigationOptions { DataPath = "/_api/pages" };
        var sut = new SpaNavigationContentService(sp, options);

        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Single(pages);
        Assert.Equal("/_api/pages/about.json", pages[0].Url.Value);
    }

    [Fact]
    public async Task Does_not_recurse_into_own_instances()
    {
        // The SpaNavigationContentService is registered as IContentService,
        // so it must skip itself to avoid infinite recursion.
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);

        var sp = BuildServiceProvider(contentService);
        var sut = new SpaNavigationContentService(sp, new SpaNavigationOptions());

        // This should not stack overflow or produce duplicate entries.
        var pages = await sut.GetPagesToGenerateAsync();

        Assert.Single(pages);
    }

    private class StubContentService(ImmutableList<PageToGenerate> pages) : IContentService
    {
        public int SearchPriority => 1;

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() =>
            Task.FromResult(pages);

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
            Task.FromResult(ImmutableList<ContentTocItem>.Empty);

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
            Task.FromResult(ImmutableList<ContentToCopy>.Empty);

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
            Task.FromResult(ImmutableList<CrossReference>.Empty);
    }
}
