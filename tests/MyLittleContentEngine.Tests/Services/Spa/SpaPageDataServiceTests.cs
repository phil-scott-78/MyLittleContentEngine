using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Spa;

namespace MyLittleContentEngine.Tests.Services.Spa;

public class SpaPageDataServiceTests
{
    private static SpaPageDataService CreateService(params IContentService[] contentServices)
    {
        var services = new ServiceCollection();
        foreach (var svc in contentServices)
            services.AddSingleton(svc);

        var sp = services.BuildServiceProvider();
        return new SpaPageDataService([], sp);
    }

    [Fact]
    public async Task Returns_envelope_with_title_and_description()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html", new Metadata { Title = "About Us", Description = "Learn more" }),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("about");

        Assert.NotNull(result);
        Assert.Equal("About Us", result.Title);
        Assert.Equal("Learn more", result.Description);
    }

    [Fact]
    public async Task Returns_null_when_no_page_matches()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html", new Metadata { Title = "About" }),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("contact");

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_when_metadata_has_no_title()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html", new Metadata { Description = "No title" }),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("about");

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_when_page_has_no_metadata()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("about");

        Assert.Null(result);
    }

    [Fact]
    public async Task Matches_url_ignoring_trailing_slash()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about/", "about.html", new Metadata { Title = "About" }),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("about");

        Assert.NotNull(result);
        Assert.Equal("About", result.Title);
    }

    [Fact]
    public async Task Index_slug_resolves_to_root_url()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/", "index.html", new Metadata { Title = "Home" }),
        ]);

        var sut = CreateService(contentService);
        var result = await sut.GetPageDataAsync("index");

        Assert.NotNull(result);
        Assert.Equal("Home", result.Title);
    }

    [Fact]
    public async Task Searches_multiple_content_services()
    {
        var service1 = new StubContentService(
        [
            new PageToGenerate("/about", "about.html", new Metadata { Title = "About" }),
        ]);
        var service2 = new StubContentService(
        [
            new PageToGenerate("/recipes/soup", "recipes/soup.html", new Metadata { Title = "Soup Recipe" }),
        ]);

        var sut = CreateService(service1, service2);
        var result = await sut.GetPageDataAsync("recipes/soup");

        Assert.NotNull(result);
        Assert.Equal("Soup Recipe", result.Title);
    }

    [Fact]
    public async Task Includes_island_html_in_envelope()
    {
        var contentService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html", new Metadata { Title = "About" }),
        ]);

        var island = new StubIslandRenderer("sidebar", "<nav>links</nav>");

        var services = new ServiceCollection();
        services.AddSingleton<IContentService>(contentService);
        var sp = services.BuildServiceProvider();

        var sut = new SpaPageDataService([island], sp);
        var result = await sut.GetPageDataAsync("about");

        Assert.NotNull(result);
        Assert.Equal("<nav>links</nav>", result.Islands["sidebar"]);
    }

    [Fact]
    public async Task Serializes_envelope_to_json()
    {
        var envelope = new SpaPageEnvelope
        {
            Title = "Test",
            Description = "Desc",
            Islands = new Dictionary<string, string> { ["content"] = "<p>hi</p>" },
        };

        var json = SpaPageDataService.Serialize(envelope);

        Assert.Contains("\"title\":\"Test\"", json);
        Assert.Contains("\"description\":\"Desc\"", json);
        Assert.Contains("\"content\":", json);
        Assert.Contains("hi", json);
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

    private class StubIslandRenderer(string name, string html) : ISpaIslandRenderer
    {
        public string IslandName => name;
        public Task<string?> RenderAsync(string url) => Task.FromResult<string?>(html);
    }
}
